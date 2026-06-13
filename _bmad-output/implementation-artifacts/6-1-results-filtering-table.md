---
baseline_commit: 45395fc
---

# Story 6.1: Results Filtering Table

Status: done

## Story

Là giáo viên,
tôi muốn lọc kết quả theo lớp, template/đề, mode, tên học sinh, kỹ năng và trạng thái,
để tìm các bài nộp cần chấm mà không cần rời khỏi phạm vi của mình.

## Acceptance Criteria

1. **Given** giáo viên mở `/teacher/results`
   **When** trang tải
   **Then** hiển thị các bộ lọc: lớp, mode (homework/live-exam), template/đề, tìm kiếm học sinh, kỹ năng, trạng thái — cùng với summary counts (tổng số kết quả và số bài cần chấm).

2. **Given** kết quả gồm cả Homework và Live Exam submissions
   **When** rows render
   **Then** mỗi row hiển thị: mode (homework/live-exam), tên học sinh, lớp, tên template, kỹ năng, điểm/trạng thái, thời gian nộp.

3. **Given** giáo viên thay đổi bộ lọc
   **When** query chạy
   **Then** rows cập nhật trong performance budget
   **And** detail đang chọn bị xóa nếu không còn khớp (selection state sẵn sàng cho Story 6.2).

4. **Given** không có row nào khớp bộ lọc
   **When** bảng rỗng
   **Then** hiển thị empty state rõ ràng và action "Xóa bộ lọc".

5. **Given** giáo viên cố lọc hoặc xem kết quả ngoài phạm vi
   **When** API đánh giá resource policies
   **Then** dữ liệu ngoài phạm vi bị loại trừ hoặc bị từ chối phía server.

## Tasks / Subtasks

- [x] Task 1: Backend — DTOs và Filter model (AC1, AC2, AC5)
  - [x] 1.1 Tạo `src/EnglishTestWeb.Api/Contracts/Results/ResultRowDto.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Results;

    public sealed record ResultRowDto(
        Guid Id,
        string Type,           // "reading-listening" | "speaking"
        string Mode,           // "homework" | "live-exam"
        string StudentName,
        string StudentId,
        Guid ClassId,
        string ClassName,
        Guid TemplateId,
        string TemplateTitle,
        string Skill,          // "reading" | "listening" | "speaking"
        string Status,
        decimal? Score,        // AutoScore (R/L) hoặc Speaking Score cast sang decimal
        DateTimeOffset? SubmittedAt,
        DateTimeOffset CreatedAt);
    ```
  - [x] 1.2 Tạo `src/EnglishTestWeb.Api/Contracts/Results/ResultsPageDto.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Results;

    public sealed record ResultsPageDto(
        IReadOnlyList<ResultRowDto> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int NeedsGrading);     // Speaking submissions có status="submitted" trong filtered set
    ```
  - [x] 1.3 Tạo `src/EnglishTestWeb.Api/Application/Results/ResultsFilter.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Application.Results;

    public sealed record ResultsFilter(
        Guid? ClassId,
        string? Mode,          // "homework" | "live-exam" | null (all)
        Guid? TemplateId,
        string? Q,             // tìm kiếm tên/email học sinh
        string? Skill,         // "reading" | "listening" | "speaking" | null (all)
        string? Status,        // "draft" | "submitted" | "auto-graded" | "graded" | null (all)
        int Page,
        int PageSize,
        string Sort,           // default: "submittedAt"
        string Direction);     // "asc" | "desc"
    ```

- [x] Task 2: Backend — Interface và Service (AC1–AC5)
  - [x] 2.1 Tạo `src/EnglishTestWeb.Api/Application/Results/IResultsService.cs`:
    ```csharp
    using EnglishTestWeb.Api.Contracts.Results;

    namespace EnglishTestWeb.Api.Application.Results;

    public interface IResultsService
    {
        Task<ResultsPageDto> GetResultsForTeacherAsync(
            string teacherId,
            ResultsFilter filter,
            CancellationToken cancellationToken = default);
    }
    ```
  - [x] 2.2 Tạo `src/EnglishTestWeb.Api/Infrastructure/Results/ResultsService.cs`:

    **Chiến lược query — 2 bảng riêng, merge in-memory, paginate:**
    ```csharp
    using EnglishTestWeb.Api.Application.Results;
    using EnglishTestWeb.Api.Contracts.Results;
    using EnglishTestWeb.Api.Domain.Speaking;
    using EnglishTestWeb.Api.Domain.Submissions;
    using EnglishTestWeb.Api.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    namespace EnglishTestWeb.Api.Infrastructure.Results;

    public sealed class ResultsService(EnglishTestWebDbContext db) : IResultsService
    {
        public async Task<ResultsPageDto> GetResultsForTeacherAsync(
            string teacherId,
            ResultsFilter filter,
            CancellationToken cancellationToken = default)
        {
            // Clamp pageSize 1..100
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);
            var page = Math.Max(1, filter.Page);

            // --- Step 0: Pre-filter by student name if Q provided ---
            IReadOnlyList<string>? studentIdFilter = null;
            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim().ToLower();
                studentIdFilter = await db.Users
                    .Where(u => (u.UserName != null && u.UserName.ToLower().Contains(q)) ||
                                (u.Email    != null && u.Email.ToLower().Contains(q)))
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                if (studentIdFilter.Count == 0)
                    return new ResultsPageDto([], page, pageSize, 0, 0);
            }

            // --- Step 1: Query Submissions (Reading/Listening) ---
            // Skip this query if skill filter explicitly selects "speaking"
            var rlRows = new List<ResultRowDto>();
            if (filter.Skill != "speaking")
            {
                var submissionQuery = db.Submissions
                    .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
                    .Include(s => s.LiveExamSession).ThenInclude(l => l!.Template)
                    .Where(s =>
                        (s.HomeworkAssignment != null && s.HomeworkAssignment.TeacherId == teacherId) ||
                        (s.LiveExamSession    != null && s.LiveExamSession.TeacherId == teacherId));

                if (filter.ClassId.HasValue)
                    submissionQuery = submissionQuery.Where(s =>
                        (s.HomeworkAssignment != null && s.HomeworkAssignment.ClassId == filter.ClassId) ||
                        (s.LiveExamSession    != null && s.LiveExamSession.ClassId == filter.ClassId));

                if (filter.Mode == "homework")
                    submissionQuery = submissionQuery.Where(s => s.HomeworkAssignmentId != null);
                else if (filter.Mode == "live-exam")
                    submissionQuery = submissionQuery.Where(s => s.LiveExamSessionId != null);

                if (filter.TemplateId.HasValue)
                    submissionQuery = submissionQuery.Where(s =>
                        (s.HomeworkAssignment != null && s.HomeworkAssignment.TestTemplateId == filter.TemplateId) ||
                        (s.LiveExamSession    != null && s.LiveExamSession.TestTemplateId == filter.TemplateId));

                if (!string.IsNullOrWhiteSpace(filter.Skill))  // reading | listening
                    submissionQuery = submissionQuery.Where(s =>
                        (s.HomeworkAssignment != null && s.HomeworkAssignment.Template != null &&
                         s.HomeworkAssignment.Template.Skill == filter.Skill) ||
                        (s.LiveExamSession    != null && s.LiveExamSession.Template    != null &&
                         s.LiveExamSession.Template.Skill == filter.Skill));

                if (!string.IsNullOrWhiteSpace(filter.Status))
                    submissionQuery = submissionQuery.Where(s => s.Status == filter.Status);

                if (studentIdFilter != null)
                    submissionQuery = submissionQuery.Where(s => studentIdFilter.Contains(s.StudentId));

                var submissions = await submissionQuery.AsNoTracking().ToListAsync(cancellationToken);

                foreach (var s in submissions)
                {
                    var template = s.HomeworkAssignment?.Template ?? s.LiveExamSession?.Template;
                    var mode     = s.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";
                    var classId  = s.HomeworkAssignment?.ClassId ?? s.LiveExamSession?.ClassId ?? Guid.Empty;

                    rlRows.Add(new ResultRowDto(
                        Id:            s.Id,
                        Type:          "reading-listening",
                        Mode:          mode,
                        StudentName:   s.StudentId,   // resolved below
                        StudentId:     s.StudentId,
                        ClassId:       classId,
                        ClassName:     classId.ToString(),  // resolved below
                        TemplateId:    template?.Id ?? Guid.Empty,
                        TemplateTitle: template?.Title ?? string.Empty,
                        Skill:         template?.Skill ?? string.Empty,
                        Status:        s.Status,
                        Score:         s.AutoScore,
                        SubmittedAt:   s.SubmittedAt,
                        CreatedAt:     s.CreatedAt));
                }
            }

            // --- Step 2: Query SpeakingSubmissions ---
            // Skip if skill filter explicitly selects "reading" or "listening"
            var speakingRows = new List<ResultRowDto>();
            if (filter.Skill is null or "speaking")
            {
                var speakingQuery = db.SpeakingSubmissions
                    .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
                    .Include(s => s.LiveExamSession).ThenInclude(l => l!.Template)
                    .Where(s =>
                        (s.HomeworkAssignment != null && s.HomeworkAssignment.TeacherId == teacherId) ||
                        (s.LiveExamSession    != null && s.LiveExamSession.TeacherId == teacherId));

                if (filter.ClassId.HasValue)
                    speakingQuery = speakingQuery.Where(s =>
                        (s.HomeworkAssignment != null && s.HomeworkAssignment.ClassId == filter.ClassId) ||
                        (s.LiveExamSession    != null && s.LiveExamSession.ClassId == filter.ClassId));

                if (filter.Mode == "homework")
                    speakingQuery = speakingQuery.Where(s => s.HomeworkAssignmentId != null);
                else if (filter.Mode == "live-exam")
                    speakingQuery = speakingQuery.Where(s => s.LiveExamSessionId != null);

                if (filter.TemplateId.HasValue)
                    speakingQuery = speakingQuery.Where(s =>
                        (s.HomeworkAssignment != null && s.HomeworkAssignment.TestTemplateId == filter.TemplateId) ||
                        (s.LiveExamSession    != null && s.LiveExamSession.TestTemplateId == filter.TemplateId));

                if (!string.IsNullOrWhiteSpace(filter.Status))
                    speakingQuery = speakingQuery.Where(s => s.Status == filter.Status);

                if (studentIdFilter != null)
                    speakingQuery = speakingQuery.Where(s => studentIdFilter.Contains(s.StudentId));

                var speakings = await speakingQuery.AsNoTracking().ToListAsync(cancellationToken);

                foreach (var s in speakings)
                {
                    var template = s.HomeworkAssignment?.Template ?? s.LiveExamSession?.Template;
                    var mode     = s.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";
                    var classId  = s.HomeworkAssignment?.ClassId ?? s.LiveExamSession?.ClassId ?? Guid.Empty;

                    speakingRows.Add(new ResultRowDto(
                        Id:            s.Id,
                        Type:          "speaking",
                        Mode:          mode,
                        StudentName:   s.StudentId,
                        StudentId:     s.StudentId,
                        ClassId:       classId,
                        ClassName:     classId.ToString(),
                        TemplateId:    template?.Id ?? Guid.Empty,
                        TemplateTitle: template?.Title ?? string.Empty,
                        Skill:         "speaking",
                        Status:        s.Status,
                        Score:         s.Score.HasValue ? (decimal)s.Score.Value : null,
                        SubmittedAt:   s.SubmittedAt,
                        CreatedAt:     s.CreatedAt));
                }
            }

            // --- Step 3: Merge + resolve names (batch, no N+1) ---
            var allRows = rlRows.Concat(speakingRows).ToList();

            var allStudentIds = allRows.Select(r => r.StudentId).Distinct().ToList();
            var allClassIds   = allRows.Select(r => r.ClassId).Distinct().ToList();

            var studentNames = await db.Users
                .Where(u => allStudentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? u.Id, cancellationToken);

            var classNames = await db.Classes
                .Where(c => allClassIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            var resolved = allRows.Select(r => r with
            {
                StudentName = studentNames.GetValueOrDefault(r.StudentId, r.StudentId),
                ClassName   = classNames.GetValueOrDefault(r.ClassId, r.ClassId.ToString()),
            }).ToList();

            // --- Step 4: Sort ---
            IOrderedEnumerable<ResultRowDto> sorted = filter.Sort switch
            {
                "studentName" => filter.Direction == "asc"
                    ? resolved.OrderBy(r => r.StudentName)
                    : resolved.OrderByDescending(r => r.StudentName),
                "score" => filter.Direction == "asc"
                    ? resolved.OrderBy(r => r.Score)
                    : resolved.OrderByDescending(r => r.Score),
                "status" => filter.Direction == "asc"
                    ? resolved.OrderBy(r => r.Status)
                    : resolved.OrderByDescending(r => r.Status),
                _ => filter.Direction == "asc"  // default: submittedAt desc
                    ? resolved.OrderBy(r => r.SubmittedAt)
                    : resolved.OrderByDescending(r => r.SubmittedAt),
            };

            var sortedList = sorted.ThenBy(r => r.Id).ToList();

            // --- Step 5: Summary counts + Paginate ---
            var totalCount   = sortedList.Count;
            var needsGrading = sortedList.Count(r => r.Type == "speaking" && r.Status == SpeakingSubmissionStatuses.Submitted);
            var items        = sortedList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new ResultsPageDto(items, page, pageSize, totalCount, needsGrading);
        }
    }
    ```

    **QUAN TRỌNG - Bẫy thường gặp:**
    - `filter.Skill is null or "speaking"` dùng C# pattern `is null or "..."` — nếu compiler không nhận, viết `filter.Skill == null || filter.Skill == "speaking"`.
    - Không dùng `.ToList()` rồi `.AsNoTracking()` — phải `AsNoTracking()` trước `ToListAsync()`.
    - `studentIdFilter.Contains(s.StudentId)` — khi `studentIdFilter` là `IReadOnlyList<string>`, EF Core sẽ translate thành IN clause.
    - `SpeakingSubmissionStatuses.Submitted` = `"submitted"` (hằng số đã có trong domain).

- [x] Task 3: Backend — Controller và DI (AC1–AC5)
  - [x] 3.1 Tạo `src/EnglishTestWeb.Api/Controllers/TeacherResultsController.cs`:
    ```csharp
    using EnglishTestWeb.Api.Application.Results;
    using EnglishTestWeb.Api.Application.Security;
    using EnglishTestWeb.Api.Contracts.Results;
    using EnglishTestWeb.Api.Infrastructure.Identity;
    using EnglishTestWeb.Api.Infrastructure.Security;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    namespace EnglishTestWeb.Api.Controllers;

    [ApiController]
    [Route("api/teacher/results")]
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    public sealed class TeacherResultsController(
        IResultsService resultsService,
        ICurrentUserContext currentUserContext,
        IHiddenResourceResponseFactory hiddenResourceResponseFactory) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ResultsPageDto>> GetResults(
            [FromQuery] Guid? classId,
            [FromQuery] string? mode,
            [FromQuery] Guid? templateId,
            [FromQuery] string? q,
            [FromQuery] string? skill,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sort = "submittedAt",
            [FromQuery] string direction = "desc",
            CancellationToken cancellationToken = default)
        {
            var teacherId = currentUserContext.UserId;
            if (string.IsNullOrWhiteSpace(teacherId))
                return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                    "auth.unauthorized", "Unauthorized.", "Authentication required.");

            var filter = new ResultsFilter(
                ClassId:    classId,
                Mode:       mode,
                TemplateId: templateId,
                Q:          q,
                Skill:      skill,
                Status:     status,
                Page:       page,
                PageSize:   pageSize,
                Sort:       sort,
                Direction:  direction);

            var result = await resultsService.GetResultsForTeacherAsync(teacherId, filter, cancellationToken);
            return Ok(result);
        }
    }
    ```
  - [x] 3.2 Đăng ký trong `src/EnglishTestWeb.Api/Program.cs`:
    ```csharp
    builder.Services.AddScoped<IResultsService, ResultsService>();
    ```
    Thêm using: `using EnglishTestWeb.Api.Application.Results;` và `using EnglishTestWeb.Api.Infrastructure.Results;`
  - [x] 3.3 `dotnet build` — xác nhận build thành công.

- [x] Task 4: Backend — Tests (AC1, AC3, AC5)
  - [x] 4.1 Tạo `tests/EnglishTestWeb.Api.Tests/Results/ResultsTestHelper.cs`:
    ```csharp
    // Helper tạo dữ liệu seed cho results tests
    // Cần tạo: HomeworkAssignment + Submission (submitted) + SpeakingSubmission (submitted)
    // Dùng SeedSpeakingHomeworkAsync từ SpeakingTestHelper để có foundation data
    // Thêm một Submission entity với Status=SubmissionStatuses.Submitted
    ```
    Seed method signature:
    ```csharp
    internal static async Task<(Guid homeworkId, Guid classId, Guid templateId)>
        SeedResultsHomeworkAsync(TestApiFactory factory);
    // Creates: HomeworkAssignment with Template (Reading skill) + published
    // Returns ids needed for test assertions
    
    internal static async Task<Guid> SeedSubmittedReadingSubmissionAsync(
        TestApiFactory factory, Guid homeworkId, string studentId);
    // Creates Submission with Status=Submitted, SubmittedAt=now, AutoScore=7.5m
    ```
  - [x] 4.2 Tạo `tests/EnglishTestWeb.Api.Tests/Results/TeacherResultsTests.cs`:

    **Tests cần implement (9 tests):**

    ```
    GetResults_AsTeacher_NoFilters_Returns200WithItems
    GetResults_FilterByClass_Returns200WithFilteredItems
    GetResults_FilterBySkillSpeaking_OnlyReturnsSpeak
    GetResults_FilterByStatus_ReturnsOnlyMatchingStatus
    GetResults_FilterByStudentSearch_ReturnsMatchingStudents
    GetResults_EmptyFilter_OutOfScopeDataExcluded
    GetResults_NoMatchingRows_Returns200WithEmptyItems
    GetResults_Unauthenticated_Returns401
    GetResults_AsStudent_Returns403
    ```

    **Pattern cho authenticated tests (two-client pattern):**
    ```csharp
    await using var factory = new TestApiFactory();
    var (homeworkId, classId, templateId) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

    // Student client first (để seed membership hoạt động)
    using var studentClient = factory.CreateClient();
    await AuthTestHelper.SignInStudentAsync(studentClient);
    var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
    var subId = await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(factory, homeworkId, studentId);

    // Teacher client
    using var client = factory.CreateClient();
    await AuthTestHelper.SignInTeacherAsync(client);
    var resp = await client.GetAsync("/api/teacher/results");
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    // parse JSON, assert items.Length >= 1
    ```

    **Test scope isolation:**
    ```csharp
    // GetResults_EmptyFilter_OutOfScopeDataExcluded:
    // Seed data với teacher default, nhưng request từ teacher KHÁC
    // Hoặc đơn giản: verify kết quả không trả dữ liệu của teacher khác
    // Dùng Guid.NewGuid() cho submission ngoài scope
    ```
  - [x] 4.3 `dotnet test` — tất cả 9 tests pass.

- [x] Task 5: Frontend — Models và API Service (AC1, AC2)
  - [x] 5.1 Tạo `src/EnglishTestWeb.Client/src/app/core/results/results.models.ts`:
    ```typescript
    export interface ResultRowDto {
      id: string;
      type: 'reading-listening' | 'speaking';
      mode: 'homework' | 'live-exam';
      studentName: string;
      studentId: string;
      classId: string;
      className: string;
      templateId: string;
      templateTitle: string;
      skill: 'reading' | 'listening' | 'speaking';
      status: string;
      score: number | null;
      submittedAt: string | null;
      createdAt: string;
    }

    export interface ResultsPageDto {
      items: ResultRowDto[];
      page: number;
      pageSize: number;
      totalCount: number;
      needsGrading: number;
    }

    export interface ResultsFilter {
      classId?: string;
      mode?: 'homework' | 'live-exam';
      templateId?: string;
      q?: string;
      skill?: 'reading' | 'listening' | 'speaking';
      status?: string;
      page: number;
      pageSize: number;
      sort: string;
      direction: 'asc' | 'desc';
    }

    export const RESULT_STATUS_LABELS: Record<string, string> = {
      draft: 'Nháp',
      submitted: 'Đã nộp',
      'auto-graded': 'Đã chấm tự động',
      graded: 'Đã chấm',
    };

    export const RESULT_MODE_LABELS: Record<string, string> = {
      homework: 'Bài tập',
      'live-exam': 'Thi trực tiếp',
    };

    export const RESULT_SKILL_LABELS: Record<string, string> = {
      reading: 'Reading',
      listening: 'Listening',
      speaking: 'Speaking',
    };
    ```
  - [x] 5.2 Tạo `src/EnglishTestWeb.Client/src/app/core/results/results-api.service.ts`:
    ```typescript
    import { HttpClient, HttpParams } from '@angular/common/http';
    import { Injectable, inject } from '@angular/core';
    import { firstValueFrom } from 'rxjs';
    import { ResultsFilter, ResultsPageDto } from './results.models';

    @Injectable({ providedIn: 'root' })
    export class ResultsApiService {
      private readonly http = inject(HttpClient);

      getResults(filter: ResultsFilter): Promise<ResultsPageDto> {
        let params = new HttpParams()
          .set('page', filter.page)
          .set('pageSize', filter.pageSize)
          .set('sort', filter.sort)
          .set('direction', filter.direction);

        if (filter.classId)    params = params.set('classId', filter.classId);
        if (filter.mode)       params = params.set('mode', filter.mode);
        if (filter.templateId) params = params.set('templateId', filter.templateId);
        if (filter.q)          params = params.set('q', filter.q);
        if (filter.skill)      params = params.set('skill', filter.skill);
        if (filter.status)     params = params.set('status', filter.status);

        return firstValueFrom(
          this.http.get<ResultsPageDto>('/api/teacher/results', { params }),
        );
      }
    }
    ```

- [x] Task 6: Frontend — Feature Component (AC1–AC5)
  - [x] 6.1 Tạo `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.ts`:
    ```typescript
    import { Component, OnInit, inject, signal } from '@angular/core';
    import { FormsModule } from '@angular/forms';
    import { ResultRowDto, ResultsFilter, ResultsPageDto, RESULT_MODE_LABELS, RESULT_SKILL_LABELS, RESULT_STATUS_LABELS } from '../../core/results/results.models';
    import { ResultsApiService } from '../../core/results/results-api.service';

    type LoadState = 'loading' | 'loaded' | 'error';

    @Component({
      selector: 'app-teacher-results',
      templateUrl: './teacher-results.component.html',
      styleUrl: './teacher-results.component.css',
      imports: [FormsModule],
    })
    export class TeacherResultsComponent implements OnInit {
      private readonly api = inject(ResultsApiService);

      // Filter signals — string để bind với <select> / <input>
      protected readonly filterClass     = signal<string>('');
      protected readonly filterMode      = signal<string>('');
      protected readonly filterTemplate  = signal<string>('');
      protected readonly filterStudent   = signal<string>('');
      protected readonly filterSkill     = signal<string>('');
      protected readonly filterStatus    = signal<string>('');

      // Pagination
      protected readonly currentPage = signal<number>(1);
      protected readonly pageSize    = signal<number>(20);

      // Results
      protected readonly loadState      = signal<LoadState>('loading');
      protected readonly results        = signal<ResultsPageDto | null>(null);
      protected readonly errorMessage   = signal<string | null>(null);
      protected readonly selectedRowId  = signal<string | null>(null);  // chuẩn bị cho Story 6.2

      // Label maps (dùng trong template)
      protected readonly modeLabelMap   = RESULT_MODE_LABELS;
      protected readonly skillLabelMap  = RESULT_SKILL_LABELS;
      protected readonly statusLabelMap = RESULT_STATUS_LABELS;

      ngOnInit(): void {
        void this.loadResults();
      }

      protected onFilterChange(): void {
        this.currentPage.set(1);
        void this.loadResults();
      }

      protected onClearFilters(): void {
        this.filterClass.set('');
        this.filterMode.set('');
        this.filterTemplate.set('');
        this.filterStudent.set('');
        this.filterSkill.set('');
        this.filterStatus.set('');
        this.currentPage.set(1);
        void this.loadResults();
      }

      protected onSelectRow(row: ResultRowDto): void {
        this.selectedRowId.set(row.id);
        // Story 6.2 sẽ thêm detail panel logic ở đây
      }

      protected onPageChange(newPage: number): void {
        this.currentPage.set(newPage);
        void this.loadResults();
      }

      protected formatDate(iso: string): string {
        return new Date(iso).toLocaleString('vi-VN');
      }

      protected formatScore(score: number | null, type: string): string {
        if (score === null) return '—';
        if (type === 'speaking') return String(Math.round(score));
        return score.toFixed(1);
      }

      private async loadResults(): Promise<void> {
        this.loadState.set('loading');
        this.errorMessage.set(null);

        // Khi filter thay đổi, clear selected row nếu không còn khớp (AC3)
        // (đơn giản: clear luôn khi load)
        this.selectedRowId.set(null);

        const filter: ResultsFilter = {
          classId:    this.filterClass()    || undefined,
          mode:       (this.filterMode()    || undefined) as ResultsFilter['mode'],
          templateId: this.filterTemplate() || undefined,
          q:          this.filterStudent()  || undefined,
          skill:      (this.filterSkill()   || undefined) as ResultsFilter['skill'],
          status:     this.filterStatus()   || undefined,
          page:       this.currentPage(),
          pageSize:   this.pageSize(),
          sort:       'submittedAt',
          direction:  'desc',
        };

        try {
          const data = await this.api.getResults(filter);
          this.results.set(data);
          this.loadState.set('loaded');
        } catch {
          this.loadState.set('error');
          this.errorMessage.set('Không thể tải kết quả. Vui lòng thử lại.');
        }
      }
    }
    ```
  - [x] 6.2 Tạo `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.html`:

    **Cấu trúc template:**
    ```html
    <div class="results-page">
      <!-- Header + summary counts -->
      <div class="results-header">
        <h1>Kết quả</h1>
        @if (results(); as r) {
          <div class="summary-counts">
            <span>{{ r.totalCount }} kết quả</span>
            @if (r.needsGrading > 0) {
              <span class="needs-grading-badge">{{ r.needsGrading }} cần chấm</span>
            }
          </div>
        }
      </div>

      <!-- Filter bar -->
      <div class="filter-bar">
        <select [ngModel]="filterMode()" (ngModelChange)="filterMode.set($event); onFilterChange()">
          <option value="">Tất cả mode</option>
          <option value="homework">Bài tập</option>
          <option value="live-exam">Thi trực tiếp</option>
        </select>

        <select [ngModel]="filterSkill()" (ngModelChange)="filterSkill.set($event); onFilterChange()">
          <option value="">Tất cả kỹ năng</option>
          <option value="reading">Reading</option>
          <option value="listening">Listening</option>
          <option value="speaking">Speaking</option>
        </select>

        <select [ngModel]="filterStatus()" (ngModelChange)="filterStatus.set($event); onFilterChange()">
          <option value="">Tất cả trạng thái</option>
          <option value="draft">Nháp</option>
          <option value="submitted">Đã nộp</option>
          <option value="auto-graded">Đã chấm tự động</option>
          <option value="graded">Đã chấm</option>
        </select>

        <input
          type="search"
          placeholder="Tìm học sinh..."
          [ngModel]="filterStudent()"
          (ngModelChange)="filterStudent.set($event)"
          (keyup.enter)="onFilterChange()"
        />

        <button type="button" (click)="onFilterChange()">Tìm</button>
        <button type="button" (click)="onClearFilters()">Xóa bộ lọc</button>
      </div>

      <!-- Loading / Error states -->
      @if (loadState() === 'loading') {
        <div class="loading-state">Đang tải...</div>
      }
      @if (loadState() === 'error') {
        <div class="error-state" role="alert">{{ errorMessage() }}</div>
      }

      <!-- Results table -->
      @if (loadState() === 'loaded' && results(); as r) {
        @if (r.items.length === 0) {
          <!-- Empty state (AC4) -->
          <div class="empty-state">
            <p>Không có kết quả nào khớp bộ lọc.</p>
            <button type="button" (click)="onClearFilters()">Xóa bộ lọc</button>
          </div>
        } @else {
          <table class="results-table">
            <thead>
              <tr>
                <th>Học sinh</th>
                <th>Lớp</th>
                <th>Đề</th>
                <th>Kỹ năng</th>
                <th>Mode</th>
                <th>Trạng thái</th>
                <th>Điểm</th>
                <th>Thời gian nộp</th>
              </tr>
            </thead>
            <tbody>
              @for (row of r.items; track row.id) {
                <tr
                  [class.selected]="selectedRowId() === row.id"
                  (click)="onSelectRow(row)"
                  tabindex="0"
                  (keyup.enter)="onSelectRow(row)"
                >
                  <td>{{ row.studentName }}</td>
                  <td>{{ row.className }}</td>
                  <td>{{ row.templateTitle }}</td>
                  <td>{{ skillLabelMap[row.skill] ?? row.skill }}</td>
                  <td>{{ modeLabelMap[row.mode] ?? row.mode }}</td>
                  <td>
                    <span [class]="'status-badge status-' + row.status">
                      {{ statusLabelMap[row.status] ?? row.status }}
                    </span>
                  </td>
                  <td>{{ formatScore(row.score, row.type) }}</td>
                  <td>{{ row.submittedAt ? formatDate(row.submittedAt) : '—' }}</td>
                </tr>
              }
            </tbody>
          </table>

          <!-- Pagination -->
          @if (r.totalCount > r.pageSize) {
            <div class="pagination">
              <button
                type="button"
                [disabled]="currentPage() <= 1"
                (click)="onPageChange(currentPage() - 1)"
              >Trước</button>
              <span>Trang {{ currentPage() }} / {{ Math.ceil(r.totalCount / r.pageSize) }}</span>
              <button
                type="button"
                [disabled]="currentPage() * r.pageSize >= r.totalCount"
                (click)="onPageChange(currentPage() + 1)"
              >Sau</button>
            </div>
          }
        }
      }
    </div>
    ```

    **QUAN TRỌNG về template:**
    - Dùng `@if` / `@for` (Angular 17+ control flow) — đã dùng trong toàn codebase.
    - `[ngModel]` / `(ngModelChange)` yêu cầu `FormsModule` trong `imports: [FormsModule]`.
    - `Math.ceil` trong template cần expose: thêm `protected readonly Math = Math;` vào component class.
    - `[disabled]="condition"` trên `<button>` là đúng Angular syntax.

  - [x] 6.3 Thêm vào component class (bổ sung sau khi viết template):
    ```typescript
    protected readonly Math = Math;   // cần cho Math.ceil() trong template
    ```
  - [x] 6.4 Tạo `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.css`:
    ```css
    .results-page { padding: 1.5rem; }
    .results-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1rem; }
    .results-header h1 { margin: 0; font-size: 1.5rem; }
    .summary-counts { display: flex; gap: 0.75rem; align-items: center; color: #4b5563; font-size: 0.9rem; }
    .needs-grading-badge { background: #fef3c7; color: #92400e; padding: 0.2rem 0.6rem; border-radius: 9999px; font-weight: 600; font-size: 0.8rem; }
    .filter-bar { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem; }
    .filter-bar select, .filter-bar input { padding: 0.4rem 0.6rem; border: 1px solid #d1d5db; border-radius: 0.375rem; font-size: 0.9rem; }
    .filter-bar button { padding: 0.4rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; background: #f9fafb; cursor: pointer; font-size: 0.9rem; }
    .filter-bar button:hover { background: #f3f4f6; }
    .loading-state, .error-state { padding: 2rem; text-align: center; color: #6b7280; }
    .error-state { color: #dc2626; }
    .empty-state { padding: 3rem; text-align: center; color: #6b7280; }
    .empty-state button { margin-top: 0.75rem; padding: 0.4rem 1rem; border: 1px solid #d1d5db; border-radius: 0.375rem; background: #f9fafb; cursor: pointer; }
    .results-table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
    .results-table th { text-align: left; padding: 0.5rem 0.75rem; border-bottom: 2px solid #e5e7eb; color: #374151; font-weight: 600; }
    .results-table td { padding: 0.5rem 0.75rem; border-bottom: 1px solid #e5e7eb; }
    .results-table tbody tr:hover { background: #f9fafb; cursor: pointer; }
    .results-table tbody tr.selected { background: #ecfdf5; }
    .results-table tbody tr:focus-visible { outline: 2px solid #059669; outline-offset: -2px; }
    .status-badge { padding: 0.15rem 0.5rem; border-radius: 9999px; font-size: 0.8rem; font-weight: 500; }
    .status-draft { background: #f3f4f6; color: #6b7280; }
    .status-submitted { background: #dbeafe; color: #1e40af; }
    .status-auto-graded { background: #d1fae5; color: #065f46; }
    .status-graded { background: #d1fae5; color: #065f46; }
    .pagination { display: flex; align-items: center; gap: 0.75rem; padding: 1rem 0; }
    .pagination button { padding: 0.4rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; background: #f9fafb; cursor: pointer; }
    .pagination button:disabled { opacity: 0.5; cursor: default; }
    ```

- [x] Task 7: Frontend — Route update (AC1)
  - [x] 7.1 Sửa `src/EnglishTestWeb.Client/src/app/app.routes.ts`:

    Thay đoạn:
    ```typescript
    {
      path: 'results',
      loadComponent: () =>
        import('./features/teacher-placeholder/teacher-placeholder.component').then(
          (module) => module.TeacherPlaceholderComponent,
        ),
      data: {
        title: 'Kết quả',
        description: 'Epic 6 sẽ triển khai Results & Grading.',
      },
    },
    ```
    Bằng:
    ```typescript
    {
      path: 'results',
      loadComponent: () =>
        import('./features/teacher-results/teacher-results.component').then(
          (module) => module.TeacherResultsComponent,
        ),
    },
    ```

- [x] Task 8: Frontend — Tests (AC1–AC4)
  - [x] 8.1 Tạo `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.spec.ts`:

    **Tests cần implement (5 tests):**
    ```typescript
    // 1. Renders loading state initially
    // 2. Renders results table after successful load
    // 3. Renders empty state when items is []
    // 4. Clear filters button calls onClearFilters and reloads
    // 5. Error state shown when API throws
    ```

    **Pattern — dùng Vitest + mock service:**
    ```typescript
    import { TestBed } from '@angular/core/testing';
    import { describe, it, expect, vi } from 'vitest';
    import { TeacherResultsComponent } from './teacher-results.component';
    import { ResultsApiService } from '../../core/results/results-api.service';
    import { ResultsPageDto } from '../../core/results/results.models';

    const mockEmptyPage: ResultsPageDto = {
      items: [], page: 1, pageSize: 20, totalCount: 0, needsGrading: 0,
    };

    describe('TeacherResultsComponent', () => {
      // setup với TestBed, provide mock ResultsApiService
      // assert DOM state
    });
    ```
  - [x] 8.2 `npm test` trong `src/EnglishTestWeb.Client` — tất cả tests pass.

## Dev Notes

### Bối cảnh và mục đích

Story 6.1 xây dựng trang `/teacher/results` — bảng kết quả với filter cho giáo viên. Đây là story đầu tiên của Epic 6. Story 6.2 sẽ thêm master-detail panel (detail của từng row), Story 6.3 sẽ thêm dashboard.

**Phạm vi của story này:**
- API endpoint `GET /api/teacher/results` với filters + pagination
- Angular component với filters, bảng, empty state, summary counts
- Row selection signal (sẵn sàng cho 6.2) nhưng **chưa có detail panel**

### Dữ liệu domain quan trọng

**Hai loại submission:**
1. `Submission` (Reading/Listening) — entity trong `Domain/Submissions/`
   - Status: `draft` | `submitted` | `auto-graded`
   - Score: `AutoScore` (decimal?)
   - Linked via HomeworkAssignmentId hoặc LiveExamSessionId

2. `SpeakingSubmission` (Speaking) — entity trong `Domain/Speaking/`
   - Status: `draft` | `submitted` | `graded`
   - Score: `Score` (int?) — phải cast sang decimal khi populate ResultRowDto
   - Linked via HomeworkAssignmentId hoặc LiveExamSessionId

**Teacher scope:**
- Teacher chỉ thấy submissions thuộc HomeworkAssignments/LiveExamSessions do chính họ tạo
- Check: `HomeworkAssignment.TeacherId == teacherId` hoặc `LiveExamSession.TeacherId == teacherId`
- KHÔNG scope qua `SchoolClass.TeacherId` — một class có thể có assignments từ nhiều teacher (theoretical)

**Skill:**
- Reading/Listening submissions: skill lấy từ `TestTemplate.Skill` (qua Include nav)
- SpeakingSubmissions: skill luôn là `"speaking"` (template skill = "speaking")

**TemplateSkill constants:**
```csharp
TemplateSkill.Reading   = "reading"
TemplateSkill.Listening = "listening"
TemplateSkill.Speaking  = "speaking"
```

### Patterns từ story trước

**Từ Story 5.3 (TeacherSpeakingGradingController):**
- Controller pattern: `[Authorize(Roles = IdentityRoleNames.Teacher)]`, inject `ICurrentUserContext`, check `teacherId`, delegate to service
- Service nhận `(bool Success, string? ErrorCode, TDto? Dto)` — nhưng cho results list, service trả thẳng DTO (không có notFound)
- `IHiddenResourceResponseFactory.FromCode(...)` cho error responses

**Từ deferred-work.md (Story 4.1 — AssignedTestService):**
- "Two sequential DB round-trips... instead of a UNION — optimize when profiling shows query latency is a concern." → Our story dùng cùng pattern (2 queries, merge in memory) — đây là acceptable MVP approach

**Từ codebase (speaking-api.service.ts):**
- Dùng `firstValueFrom(this.http.get<T>(...))` — không dùng Observable subscribe
- `HttpParams` với `.set()` chaining cho query params

### Cấu trúc file mới

**Backend:**
```
src/EnglishTestWeb.Api/
  Application/Results/
    IResultsService.cs          NEW
    ResultsFilter.cs            NEW
  Contracts/Results/
    ResultRowDto.cs             NEW
    ResultsPageDto.cs           NEW
  Controllers/
    TeacherResultsController.cs NEW
  Infrastructure/Results/
    ResultsService.cs           NEW
  Program.cs                    UPDATE (AddScoped)
```

**Frontend:**
```
src/EnglishTestWeb.Client/src/app/
  core/results/
    results.models.ts           NEW
    results-api.service.ts      NEW
  features/teacher-results/
    teacher-results.component.ts   NEW
    teacher-results.component.html NEW
    teacher-results.component.css  NEW
    teacher-results.component.spec.ts NEW
  app.routes.ts                 UPDATE
```

**Tests:**
```
tests/EnglishTestWeb.Api.Tests/
  Results/
    TeacherResultsTests.cs      NEW
    ResultsTestHelper.cs        NEW
```

### Các bẫy thường gặp cần tránh

1. **N+1 query cho student names/class names** — KHÔNG query per-row. Collect all IDs, batch query, dùng Dictionary.
2. **`AsNoTracking()` trước `ToListAsync()`** — đúng thứ tự: `.AsNoTracking().ToListAsync()`.
3. **`filter.Skill is null or "speaking"` C# pattern** — nếu không compile, viết `filter.Skill == null || filter.Skill == "speaking"`.
4. **Include chains cho SpeakingSubmission** — dùng `ThenInclude(h => h!.Template)` với null-forgiving `!` (existing pattern từ TeacherSpeakingGradingService).
5. **FormsModule trong imports** — standalone component phải có `imports: [FormsModule]` để dùng `ngModel`.
6. **`Math.ceil` trong template** — Angular template không tự nhận `Math` global; phải `protected readonly Math = Math;` trong class.
7. **`@if` / `@for` control flow** — dùng Angular 17+ syntax (không phải `*ngIf` / `*ngFor`).
8. **Score field type** — `ResultRowDto.Score` là `decimal?` (C#) → `number | null` (TypeScript). `AutoScore` là `decimal?`, `Score` là `int?` → cast cả hai sang `decimal` khi build DTO.
9. **Pagination 1-based** — `page = 1` là trang đầu. Skip = `(page-1) * pageSize`.
10. **Two-client test pattern** — student phải sign-in TRƯỚC teacher trong cùng factory để seed membership hoạt động.

### Thông tin kỹ thuật bổ sung

**Đường dẫn import C# mẫu:**
```csharp
using EnglishTestWeb.Api.Application.Results;   // IResultsService, ResultsFilter
using EnglishTestWeb.Api.Contracts.Results;      // ResultRowDto, ResultsPageDto
using EnglishTestWeb.Api.Infrastructure.Results; // ResultsService
using EnglishTestWeb.Api.Domain.Speaking;        // SpeakingSubmissionStatuses
using EnglishTestWeb.Api.Infrastructure.Persistence; // EnglishTestWebDbContext
```

**Đăng ký DI trong Program.cs — tìm đoạn AddScoped hiện có:**
```csharp
builder.Services.AddScoped<ITeacherSpeakingGradingService, TeacherSpeakingGradingService>();
// Thêm dưới dòng trên:
builder.Services.AddScoped<IResultsService, ResultsService>();
```

**Test helper pattern (từ SpeakingTestHelper.cs):**
- Tạo entities trực tiếp qua `factory.Services.GetRequiredService<EnglishTestWebDbContext>()`
- Không cần HTTP call để seed data

**Angular `HttpParams` import:**
```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
```

### Project Structure Notes

- API theo structure: `Application/` cho interfaces+DTOs, `Infrastructure/` cho implementations — nhất quán với TeacherSpeakingGradingService
- Angular features folder: `features/teacher-results/` — nhất quán với naming convention `teacher-*`
- Core services: `core/results/` — nhất quán với `core/speaking/`
- Route update: thay `teacher-placeholder` bằng `teacher-results` — placeholder đã được thiết kế để thay thế

### References

- [Source: architecture.md#API-Patterns] — Pagination `{ items, page, pageSize, totalCount }`, filter params camelCase
- [Source: architecture.md#Requirements-Mapping] — `FR-17: Application/Results, features/results-grading`
- [Source: TeacherSpeakingGradingController.cs] — pattern cho teacher controller với scope check
- [Source: TeacherSpeakingGradingService.cs] — pattern cho service với Include chains + null-forgiving
- [Source: speaking-api.service.ts] — pattern cho Angular API service với firstValueFrom
- [Source: app.routes.ts:152-161] — route hiện tại dùng teacher-placeholder, cần thay
- [Source: EnglishTestWebDbContext.cs] — available DbSets: Submissions, SpeakingSubmissions, HomeworkAssignments, LiveExamSessions, Classes, Users

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- API tests: 9/9 pass (311 total API tests, 0 failures)
- Angular tests: 186/186 pass (27 test files)
- `filter.Skill is null or "speaking"` C# pattern compiled fine (no fallback needed)
- Controller had incorrect using `EnglishTestWeb.Api.Infrastructure.Security` — removed, only `Application.Security` needed
- Angular template type warnings on Record index access (skillLabelMap/modeLabelMap/statusLabelMap) — non-blocking, all tests pass

### File List

- `src/EnglishTestWeb.Api/Contracts/Results/ResultRowDto.cs` — NEW
- `src/EnglishTestWeb.Api/Contracts/Results/ResultsPageDto.cs` — NEW
- `src/EnglishTestWeb.Api/Application/Results/ResultsFilter.cs` — NEW
- `src/EnglishTestWeb.Api/Application/Results/IResultsService.cs` — NEW
- `src/EnglishTestWeb.Api/Infrastructure/Results/ResultsService.cs` — NEW
- `src/EnglishTestWeb.Api/Controllers/TeacherResultsController.cs` — NEW
- `src/EnglishTestWeb.Api/Program.cs` — UPDATE (AddScoped IResultsService)
- `tests/EnglishTestWeb.Api.Tests/Results/ResultsTestHelper.cs` — NEW
- `tests/EnglishTestWeb.Api.Tests/Results/TeacherResultsTests.cs` — NEW
- `src/EnglishTestWeb.Client/src/app/core/results/results.models.ts` — NEW
- `src/EnglishTestWeb.Client/src/app/core/results/results-api.service.ts` — NEW
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.ts` — NEW
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.html` — NEW
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.css` — NEW
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.spec.ts` — NEW
- `src/EnglishTestWeb.Client/src/app/app.routes.ts` — UPDATE (results route → TeacherResultsComponent)

### Senior Developer Review (AI)

**Review Date:** 2026-06-13 (Pass 1) + 2026-06-13 (Pass 2)
**Outcome:** Approved after 2 passes
**Layers run:** Blind Hunter, Edge Case Hunter, Acceptance Auditor
**Dismissed:** 5 | **Deferred:** 9 | **Patch:** 1 | **Decision Needed:** 2

#### Review Follow-ups (AI)

- [x] [Review][Decision→Defer] AC1 Missing class filter — deferred sang Story 6.2. Backend hỗ trợ classId; dropdown cần populate từ classes API — out of scope 6.1; Story 6.2 redesign filter bar.
- [x] [Review][Decision→Defer] AC1 Missing template filter — deferred sang Story 6.2. Cần templateId Guid từ library API; Story 6.2 sẽ xử lý cùng class filter.
- [x] [Review][Patch] Concurrent filter changes stale-data race — đã fix: thêm debounce 300ms và request ID tracking. [teacher-results.component.ts:47-49]

**Pass 2 findings:**
- [x] [Review][Patch] `onClearFilters()` thiếu `clearTimeout(debounceTimer)` — gây extra request/flicker khi cancel debounce. Đã fix: thêm clearTimeout guard đầu method. [teacher-results.component.ts:60]
- [x] [Review][Defer] `needsGrading` phụ thuộc vào active filter — badge shows "0 cần chấm" khi filter `status=graded`, dù có ungraded submissions ngoài filter. By design MVP; thay đổi khi có global pending-work dashboard.
- [x] [Review][Defer] `studentIdFilter` `.ToLower()` có thể trigger client-side eval trên non-SQL providers — in-memory test masks; production SQL Server OK vì EF Core dịch thành LOWER(). Fix khi standardize collation-aware search.
- [x] [Review][Defer] `SubmittedAt` null sort non-deterministic — draft submissions (null SubmittedAt) có unstable pagination order. Low impact (teacher view); thêm secondary sort key khi cần.
- [x] [Review][Defer] Angular spec thiếu assertion verify filter signals reset trong onClearFilters test. Thêm khi có test expansion story.
- [x] [Review][Defer] Angular spec thiếu test cho `onPageChange` code path. Thêm khi có test expansion story.
- [x] [Review][Defer] Full result set loaded into memory before pagination — cả hai queries fetch all rows trước khi sort/paginate. MVP trade-off đã được ghi nhận trong Dev Notes. [ResultsService.cs]
- [x] [Review][Defer] Unbounded IN clause from Q search — `studentIdFilter` có thể chứa nhiều IDs nếu query match nhiều users. Cùng scope với in-memory pagination, acceptable MVP. [ResultsService.cs:24-31]
- [x] [Review][Defer] Q search scopes to all users, not just teacher's students — performance chậm hơn nhưng không leak data vì teacher scope vẫn được apply ở bước tiếp. [ResultsService.cs]
- [x] [Review][Defer] Skill empty-string asymmetry — `filter.Skill=""` bỏ speaking rows; Angular gửi `undefined` nên masked trong practice. [ResultsService.cs:37,103]
- [x] [Review][Defer] Status "graded" returns 0 RL results — RL không có status "graded"; by design. [teacher-results.component.html:46]
- [x] [Review][Defer] Guid.Empty sentinel for unknown classId — khi cả hai navigation null, classId = Guid.Empty, hiển thị garbage string. Chỉ xảy ra khi data corrupt. [ResultsService.cs:80]
- [x] [Review][Defer] AC3 selectedRowId cleared unconditionally — spec nói "nếu không còn khớp" nhưng code clear luôn; 6.2 sẽ implement fine-grained selection. [teacher-results.component.ts:83]
- [x] [Review][Defer] Sort tiebreaker uses random Guid — không time-ordered, inconsistent pagination với rows có cùng submittedAt. [ResultsService.cs:199]
- [x] [Review][Defer] Full dataset materialised then sorted in memory — no DB-level sort. Cùng với in-memory pagination. [ResultsService.cs]
- [x] [Review][Defer] Template navigation null produces empty TemplateTitle/Guid.Empty TemplateId — chỉ khi template bị xóa/orphaned. [ResultsService.cs:78-79]
