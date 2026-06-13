---
baseline_commit: 17b2c6d
---

# Story 6.3: Teacher Dashboard Summary And Recent Work Routing

Status: done

## Story

Là giáo viên,
tôi muốn một dashboard yên tĩnh với các số liệu scan-level và danh sách công việc gần đây,
để tôi có thể nắm bắt khối lượng công việc hiện tại và điều hướng nhanh đến module phù hợp.

## Acceptance Criteria

1. **Given** giáo viên mở `/teacher/dashboard`
   **When** dashboard data tải
   **Then** hiển thị scan metrics cho: tổng đề gốc, số Homework đang hoạt động (chưa hết deadline), số Live Exam mở hôm nay/đang open, số submission gần đây (7 ngày), số Speaking cần chấm
   **And** tất cả số liệu là scoped theo teacher đang đăng nhập.

2. **Given** class filter được chọn
   **When** metrics refresh
   **Then** counts và recent work chỉ phản ánh class được chọn (vẫn scoped theo teacher).

3. **Given** giáo viên click Thư viện đề, Lớp học hoặc Kết quả trong navigation
   **When** navigation xảy ra
   **Then** chuyển tới module tương ứng (shell navigation đã có sẵn — không thay đổi shell).

4. **Given** dashboard chưa có dữ liệu (teacher mới)
   **When** metrics render với count = 0
   **Then** dashboard vẫn calm và operational
   **And** không xuất hiện workflow "tạo đề" riêng trên dashboard (tạo đề phải đi qua Thư viện đề).

5. **Given** recent work rows hiển thị
   **When** giáo viên click một row
   **Then** route điều hướng đúng context: submission rows → `/teacher/results`, template rows → `/teacher/library/{id}/review`, homework rows → `/teacher/results?mode=homework`, live-exam rows → `/teacher/results?mode=live-exam`.

## Tasks / Subtasks

- [x] Task 1: Backend — DTOs (AC1, AC2, AC5)
  - [x] 1.1 Tạo `src/EnglishTestWeb.Api/Contracts/Dashboard/TeacherDashboardSummaryDto.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Dashboard;

    public sealed record TeacherDashboardSummaryDto(
        int TemplateCount,
        int ActiveHomeworkCount,
        int OpenLiveExamCount,
        int RecentSubmissionCount,
        int PendingSpeakingCount);
    ```
  - [x] 1.2 Tạo `src/EnglishTestWeb.Api/Contracts/Dashboard/TeacherRecentWorkItemDto.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Dashboard;

    public sealed record TeacherRecentWorkItemDto(
        string Type,          // "submission" | "template" | "homework" | "live-exam"
        string Id,
        string Title,
        string ClassName,
        string Mode,          // "homework" | "live-exam" | ""
        string Status,
        DateTimeOffset Timestamp);
    ```
  - [x] 1.3 Tạo `src/EnglishTestWeb.Api/Contracts/Dashboard/TeacherDashboardDto.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Dashboard;

    public sealed record TeacherDashboardDto(
        TeacherDashboardSummaryDto Summary,
        IReadOnlyList<TeacherRecentWorkItemDto> RecentWork);
    ```

- [x] Task 2: Backend — ITeacherDashboardService (AC1, AC2)
  - [x] 2.1 Tạo `src/EnglishTestWeb.Api/Application/Dashboard/ITeacherDashboardService.cs`:
    ```csharp
    using EnglishTestWeb.Api.Contracts.Dashboard;

    namespace EnglishTestWeb.Api.Application.Dashboard;

    public interface ITeacherDashboardService
    {
        Task<TeacherDashboardDto> GetDashboardAsync(
            string teacherId,
            Guid? classId = null,
            CancellationToken cancellationToken = default);
    }
    ```
  - [x] 2.2 Tạo `src/EnglishTestWeb.Api/Infrastructure/Dashboard/TeacherDashboardService.cs`:

    **Logic:**
    - Tất cả queries dùng `AsNoTracking()`.
    - `TemplateCount`: `db.TestTemplates.Where(t => t.TeacherId == teacherId && (classId == null))` — templates không có ClassId, đếm tất cả templates của teacher (không filter theo class).
    - `ActiveHomeworkCount`: `db.HomeworkAssignments.Where(h => h.TeacherId == teacherId && h.DeadlineAt > now && (classId == null || h.ClassId == classId))` — status = published, chưa hết deadline.
    - `OpenLiveExamCount`: `db.LiveExamSessions.Where(l => l.TeacherId == teacherId && l.Status == "open" && (classId == null || l.ClassId == classId))`.
    - `RecentSubmissionCount`: join Submissions → HomeworkAssignments/LiveExamSessions scoped by teacherId, SubmittedAt trong 7 ngày qua.
    - `PendingSpeakingCount`: SpeakingSubmissions với status = "submitted" (chưa graded) scoped qua assignment/session → teacherId.
    - RecentWork: lấy 10 submissions gần nhất (SubmittedAt DESC), join để lấy templateTitle và className.

    ```csharp
    using EnglishTestWeb.Api.Application.Dashboard;
    using EnglishTestWeb.Api.Contracts.Dashboard;
    using EnglishTestWeb.Api.Domain.LiveExams;
    using EnglishTestWeb.Api.Domain.Speaking;
    using EnglishTestWeb.Api.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    namespace EnglishTestWeb.Api.Infrastructure.Dashboard;

    public sealed class TeacherDashboardService(EnglishTestWebDbContext db)
        : ITeacherDashboardService
    {
        private static readonly TimeSpan RecentWindow = TimeSpan.FromDays(7);

        public async Task<TeacherDashboardDto> GetDashboardAsync(
            string teacherId,
            Guid? classId = null,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var recentCutoff = now - RecentWindow;

            // Run all summary counts in parallel using separate queries
            var templateCountTask = db.TestTemplates
                .Where(t => t.TeacherId == teacherId)
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var activeHomeworkCountTask = db.HomeworkAssignments
                .Where(h => h.TeacherId == teacherId
                         && h.DeadlineAt > now
                         && (classId == null || h.ClassId == classId))
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var openLiveExamCountTask = db.LiveExamSessions
                .Where(l => l.TeacherId == teacherId
                         && l.Status == LiveExamSessionStatuses.Open
                         && (classId == null || l.ClassId == classId))
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Recent submission count: submissions belonging to teacher's homework/live-exams
            var recentSubmissionCountTask = db.Submissions
                .Where(s => s.SubmittedAt != null
                         && s.SubmittedAt >= recentCutoff
                         && (s.HomeworkAssignment != null
                             ? s.HomeworkAssignment.TeacherId == teacherId
                               && (classId == null || s.HomeworkAssignment.ClassId == classId)
                             : s.LiveExamSession != null
                               && s.LiveExamSession.TeacherId == teacherId
                               && (classId == null || s.LiveExamSession.ClassId == classId)))
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Pending speaking: submitted (not graded) speaking submissions scoped to teacher
            var pendingSpeakingCountTask = db.SpeakingSubmissions
                .Where(ss => ss.Status == SpeakingSubmissionStatuses.Submitted
                          && (ss.HomeworkAssignment != null
                              ? ss.HomeworkAssignment.TeacherId == teacherId
                                && (classId == null || ss.HomeworkAssignment.ClassId == classId)
                              : ss.LiveExamSession != null
                                && ss.LiveExamSession.TeacherId == teacherId
                                && (classId == null || ss.LiveExamSession.ClassId == classId)))
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Await summary counts sequentially (EF DbContext not thread-safe)
            var templateCount         = await templateCountTask;
            var activeHomeworkCount   = await activeHomeworkCountTask;
            var openLiveExamCount     = await openLiveExamCountTask;
            var recentSubmissionCount = await recentSubmissionCountTask;
            var pendingSpeakingCount  = await pendingSpeakingCountTask;

            // Recent work: last 10 submissions for this teacher
            var recentSubmissions = await db.Submissions
                .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
                .Include(s => s.LiveExamSession).ThenInclude(l => l!.Template)
                .Where(s => s.SubmittedAt != null
                         && (s.HomeworkAssignment != null
                             ? s.HomeworkAssignment.TeacherId == teacherId
                               && (classId == null || s.HomeworkAssignment.ClassId == classId)
                             : s.LiveExamSession != null
                               && s.LiveExamSession.TeacherId == teacherId
                               && (classId == null || s.LiveExamSession.ClassId == classId)))
                .OrderByDescending(s => s.SubmittedAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Batch resolve class names
            var classIds = recentSubmissions
                .Select(s => s.HomeworkAssignment?.ClassId ?? s.LiveExamSession?.ClassId ?? Guid.Empty)
                .Distinct()
                .Where(id => id != Guid.Empty)
                .ToList();

            var classNames = classIds.Any()
                ? await db.Classes
                    .Where(c => classIds.Contains(c.Id))
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken)
                : [];

            var recentWork = recentSubmissions.Select(s =>
            {
                var isHomework = s.HomeworkAssignmentId.HasValue;
                var assignedClassId = s.HomeworkAssignment?.ClassId ?? s.LiveExamSession?.ClassId ?? Guid.Empty;
                var className = classNames.GetValueOrDefault(assignedClassId, string.Empty);
                var template = s.HomeworkAssignment?.Template ?? s.LiveExamSession?.Template;
                return new TeacherRecentWorkItemDto(
                    Type:      "submission",
                    Id:        s.Id.ToString(),
                    Title:     template?.Title ?? string.Empty,
                    ClassName: className,
                    Mode:      isHomework ? "homework" : "live-exam",
                    Status:    s.Status,
                    Timestamp: s.SubmittedAt!.Value);
            }).ToList();

            return new TeacherDashboardDto(
                Summary: new TeacherDashboardSummaryDto(
                    TemplateCount:         templateCount,
                    ActiveHomeworkCount:   activeHomeworkCount,
                    OpenLiveExamCount:     openLiveExamCount,
                    RecentSubmissionCount: recentSubmissionCount,
                    PendingSpeakingCount:  pendingSpeakingCount),
                RecentWork: recentWork);
        }
    }
    ```

    **QUAN TRỌNG:**
    - `db.SpeakingSubmissions` có `HomeworkAssignment` và `LiveExamSession` nav props — verify bằng grep trước khi code.
    - Các count tasks được tạo TRƯỚC rồi await tuần tự (KHÔNG dùng `Task.WhenAll` vì EF DbContext không thread-safe).
    - `SpeakingSubmissionStatuses.Submitted` = `"submitted"`, `LiveExamSessionStatuses.Open` = `"open"`.
    - `classNames.GetValueOrDefault` — không throw khi key không tồn tại.

- [x] Task 3: Backend — TeacherDashboardController (AC1, AC2)
  - [x] 3.1 Tạo `src/EnglishTestWeb.Api/Controllers/TeacherDashboardController.cs`:
    ```csharp
    using EnglishTestWeb.Api.Application.Dashboard;
    using EnglishTestWeb.Api.Application.Security;
    using EnglishTestWeb.Api.Contracts.Dashboard;
    using EnglishTestWeb.Api.Infrastructure.Identity;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    namespace EnglishTestWeb.Api.Controllers;

    [ApiController]
    [Route("api/teacher/dashboard")]
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    public sealed class TeacherDashboardController(
        ITeacherDashboardService dashboardService,
        ICurrentUserContext currentUserContext,
        IHiddenResourceResponseFactory hiddenResourceResponseFactory) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<TeacherDashboardDto>> GetDashboard(
            [FromQuery] Guid? classId = null,
            CancellationToken cancellationToken = default)
        {
            var teacherId = currentUserContext.UserId;
            if (string.IsNullOrWhiteSpace(teacherId))
                return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                    "auth.unauthorized", "Unauthorized.", "Authentication required.");

            var dto = await dashboardService.GetDashboardAsync(teacherId, classId, cancellationToken);
            return Ok(dto);
        }
    }
    ```

- [x] Task 4: Backend — DI registration và build (AC1)
  - [x] 4.1 Thêm vào `src/EnglishTestWeb.Api/Program.cs` (sau `AddScoped<ITeacherSubmissionDetailService,...>`):
    ```csharp
    builder.Services.AddScoped<ITeacherDashboardService, TeacherDashboardService>();
    ```
  - [x] 4.2 Verify `db.SpeakingSubmissions` có Include `HomeworkAssignment` và `LiveExamSession` — chạy:
    ```bash
    grep -n "HomeworkAssignment\|LiveExamSession" src/EnglishTestWeb.Api/Domain/Speaking/SpeakingSubmission.cs
    ```
    Nếu nav props chưa có: thêm vào `SpeakingSubmission.cs`:
    ```csharp
    public HomeworkAssignment? HomeworkAssignment { get; set; }
    public LiveExamSession? LiveExamSession { get; set; }
    ```
    Và verify EF config trong `SpeakingSubmissionConfiguration.cs` đã có FK.
  - [x] 4.3 `dotnet build` — xác nhận build thành công.

- [x] Task 5: Backend — Tests (AC1, AC2, AC4)
  - [x] 5.1 Tạo `tests/EnglishTestWeb.Api.Tests/Dashboard/TeacherDashboardTests.cs`:

    **Tests cần implement (5 tests):**
    ```
    GetDashboard_AsTeacher_ReturnsSummaryAndRecentWork
    GetDashboard_WithClassFilter_FiltersMetrics
    GetDashboard_NoData_ReturnsZeroCounts
    GetDashboard_Unauthenticated_Returns401
    GetDashboard_AsStudent_Returns403
    ```

    **Pattern:**
    ```csharp
    await using var factory = new TestApiFactory();
    // Seed some data via ResultsTestHelper or new DashboardTestHelper
    using var client = factory.CreateClient();
    await AuthTestHelper.SignInTeacherAsync(client);
    var resp = await client.GetAsync("/api/teacher/dashboard");
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    var dto = await resp.Content.ReadFromJsonAsync<TeacherDashboardDto>();
    Assert.NotNull(dto);
    Assert.NotNull(dto.Summary);
    // assert counts >= 0
    ```

  - [x] 5.2 `dotnet test` — tất cả tests pass.

- [x] Task 6: Frontend — Models và API service (AC1, AC2, AC5)
  - [x] 6.1 Tạo `src/EnglishTestWeb.Client/src/app/core/dashboard/dashboard.models.ts`:
    ```typescript
    export interface TeacherDashboardSummaryDto {
      templateCount: number;
      activeHomeworkCount: number;
      openLiveExamCount: number;
      recentSubmissionCount: number;
      pendingSpeakingCount: number;
    }

    export interface TeacherRecentWorkItemDto {
      type: string;      // "submission" | "template" | "homework" | "live-exam"
      id: string;
      title: string;
      className: string;
      mode: string;      // "homework" | "live-exam" | ""
      status: string;
      timestamp: string; // ISO DateTimeOffset
    }

    export interface TeacherDashboardDto {
      summary: TeacherDashboardSummaryDto;
      recentWork: TeacherRecentWorkItemDto[];
    }

    export const RECENT_WORK_MODE_LABELS: Record<string, string> = {
      homework: 'Homework',
      'live-exam': 'Thi trực tiếp',
    };

    export const RECENT_WORK_STATUS_LABELS: Record<string, string> = {
      submitted: 'Đã nộp',
      'auto-graded': 'Đã chấm tự động',
      graded: 'Đã chấm',
    };
    ```
  - [x] 6.2 Tạo `src/EnglishTestWeb.Client/src/app/core/dashboard/dashboard-api.service.ts`:
    ```typescript
    import { HttpClient } from '@angular/common/http';
    import { Injectable, inject } from '@angular/core';
    import { firstValueFrom } from 'rxjs';
    import { TeacherDashboardDto } from './dashboard.models';

    @Injectable({ providedIn: 'root' })
    export class DashboardApiService {
      private readonly http = inject(HttpClient);

      getDashboard(classId?: string): Promise<TeacherDashboardDto> {
        const params: Record<string, string> = {};
        if (classId) params['classId'] = classId;
        return firstValueFrom(
          this.http.get<TeacherDashboardDto>('/api/teacher/dashboard', { params }),
        );
      }
    }
    ```

- [x] Task 7: Frontend — Component update (AC1, AC2, AC4, AC5)
  - [x] 7.1 Cập nhật `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.ts`:

    **Thay toàn bộ file bằng:**
    ```typescript
    import { Component, OnInit, inject, signal } from '@angular/core';
    import { FormsModule } from '@angular/forms';
    import { RouterLink } from '@angular/router';
    import { DashboardApiService } from '../../core/dashboard/dashboard-api.service';
    import { ClassesApiService } from '../../core/classes/classes-api.service';
    import {
      TeacherDashboardDto,
      TeacherRecentWorkItemDto,
      RECENT_WORK_MODE_LABELS,
      RECENT_WORK_STATUS_LABELS,
    } from '../../core/dashboard/dashboard.models';
    import { ClassSummary } from '../../core/classes/classes.models';

    @Component({
      selector: 'app-teacher-dashboard',
      templateUrl: './teacher-dashboard.component.html',
      styleUrl: './teacher-dashboard.component.css',
      imports: [FormsModule, RouterLink],
    })
    export class TeacherDashboardComponent implements OnInit {
      private readonly dashboardApi = inject(DashboardApiService);
      private readonly classesApi   = inject(ClassesApiService);

      protected readonly dashboard         = signal<TeacherDashboardDto | null>(null);
      protected readonly availableClasses  = signal<ClassSummary[]>([]);
      protected readonly filterClass       = signal<string>('');
      protected readonly loadState         = signal<'loading' | 'loaded' | 'error'>('loading');
      protected readonly loadError         = signal<string | null>(null);

      protected readonly modeLabelMap   = RECENT_WORK_MODE_LABELS;
      protected readonly statusLabelMap = RECENT_WORK_STATUS_LABELS;

      ngOnInit(): void {
        void this.loadClasses();
        void this.loadDashboard();
      }

      private async loadClasses(): Promise<void> {
        try {
          this.availableClasses.set(await this.classesApi.getTeacherClasses());
        } catch {
          // non-critical — filter just won't populate
        }
      }

      protected async onClassFilterChange(): Promise<void> {
        await this.loadDashboard();
      }

      private async loadDashboard(): Promise<void> {
        this.loadState.set('loading');
        this.loadError.set(null);
        try {
          const classId = this.filterClass() || undefined;
          const data = await this.dashboardApi.getDashboard(classId);
          this.dashboard.set(data);
          this.loadState.set('loaded');
        } catch {
          this.loadState.set('error');
          this.loadError.set('Không thể tải dữ liệu. Vui lòng thử lại.');
        }
      }

      protected getRouterLink(item: TeacherRecentWorkItemDto): string[] {
        if (item.type === 'template') return ['/teacher/library', item.id, 'review'];
        return ['/teacher/results'];
      }

      protected getQueryParams(item: TeacherRecentWorkItemDto): Record<string, string> {
        if (item.type === 'submission' && item.mode) return { mode: item.mode };
        if (item.type === 'homework') return { mode: 'homework' };
        if (item.type === 'live-exam') return { mode: 'live-exam' };
        return {};
      }
    }
    ```

  - [x] 7.2 Cập nhật `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.html`:
    ```html
    <section id="teacher-dashboard-overview-header" class="dashboard-header">
      <h1 id="teacher-dashboard-title">Tổng quan</h1>

      <div class="filter-bar">
        <label for="classFilter">Lớp:</label>
        <select
          id="classFilter"
          [ngModel]="filterClass()"
          (ngModelChange)="filterClass.set($event); onClassFilterChange()"
        >
          <option value="">Tất cả lớp</option>
          @for (cls of availableClasses(); track cls.classId) {
            <option [value]="cls.classId">{{ cls.className }}</option>
          }
        </select>
      </div>
    </section>

    @if (loadState() === 'loading') {
      <div id="teacher-dashboard-loading" class="loading-state" aria-busy="true">Đang tải...</div>
    }

    @if (loadState() === 'error') {
      <div id="teacher-dashboard-error" class="error-state" role="alert">{{ loadError() }}</div>
    }

    @if (loadState() === 'loaded' && dashboard(); as data) {
      <section id="teacher-dashboard-summary-grid" class="summary-grid" aria-label="Tóm tắt">
        <article id="teacher-dashboard-templates-card" class="summary-card">
          <h2>Đề gốc</h2>
          <span class="metric">{{ data.summary.templateCount }}</span>
          <a routerLink="/teacher/library" class="card-link">Thư viện đề →</a>
        </article>

        <article id="teacher-dashboard-homework-card" class="summary-card">
          <h2>Homework đang mở</h2>
          <span class="metric">{{ data.summary.activeHomeworkCount }}</span>
          <a [routerLink]="['/teacher/results']" [queryParams]="{ mode: 'homework' }" class="card-link">Xem kết quả →</a>
        </article>

        <article id="teacher-dashboard-live-exam-card" class="summary-card">
          <h2>Live Exam đang mở</h2>
          <span class="metric">{{ data.summary.openLiveExamCount }}</span>
        </article>

        <article id="teacher-dashboard-submissions-card" class="summary-card">
          <h2>Lượt nộp (7 ngày)</h2>
          <span class="metric">{{ data.summary.recentSubmissionCount }}</span>
          <a routerLink="/teacher/results" class="card-link">Xem kết quả →</a>
        </article>

        <article id="teacher-dashboard-speaking-card" class="summary-card"
          [class.has-pending]="data.summary.pendingSpeakingCount > 0">
          <h2>Speaking cần chấm</h2>
          <span class="metric">{{ data.summary.pendingSpeakingCount }}</span>
          @if (data.summary.pendingSpeakingCount > 0) {
            <a [routerLink]="['/teacher/results']" [queryParams]="{ skill: 'speaking', status: 'submitted' }" class="card-link urgent">Chấm ngay →</a>
          }
        </article>
      </section>

      <section id="teacher-dashboard-recent-work" class="recent-work">
        <h2>Hoạt động gần đây</h2>

        @if (data.recentWork.length === 0) {
          <p id="teacher-dashboard-empty-state" class="empty-state">
            Chưa có bài nộp nào. Giao bài từ <a routerLink="/teacher/library">Thư viện đề</a>.
          </p>
        } @else {
          <table class="recent-table" aria-label="Hoạt động gần đây">
            <thead>
              <tr>
                <th>Đề</th>
                <th>Lớp</th>
                <th>Loại</th>
                <th>Trạng thái</th>
                <th>Thời gian</th>
              </tr>
            </thead>
            <tbody>
              @for (item of data.recentWork; track item.id) {
                <tr
                  class="recent-row"
                  [attr.aria-label]="item.title + ' - ' + item.className"
                >
                  <td>
                    <a
                      [routerLink]="getRouterLink(item)"
                      [queryParams]="getQueryParams(item)"
                      class="row-link"
                    >{{ item.title || '—' }}</a>
                  </td>
                  <td>{{ item.className || '—' }}</td>
                  <td>{{ modeLabelMap[item.mode] || item.mode || '—' }}</td>
                  <td>{{ statusLabelMap[item.status] || item.status || '—' }}</td>
                  <td>{{ item.timestamp | date: 'dd/MM/yyyy HH:mm' }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    }
    ```

    **QUAN TRỌNG:** Template dùng `DatePipe` — cần thêm vào `imports` trong component:
    ```typescript
    import { DatePipe } from '@angular/common';
    // Thêm DatePipe vào imports array của @Component:
    imports: [FormsModule, RouterLink, DatePipe],
    ```

  - [x] 7.3 Cập nhật `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.css`:
    ```css
    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
      gap: 0.75rem;
    }
    .dashboard-header h1 { margin: 0; font-size: 1.25rem; font-weight: 600; }
    .filter-bar { display: flex; align-items: center; gap: 0.5rem; }
    .filter-bar label { font-size: 0.875rem; color: #374151; }
    .filter-bar select { padding: 0.35rem 0.5rem; border: 1px solid #d1d5db; border-radius: 0.375rem; font-size: 0.875rem; }
    .loading-state, .error-state { padding: 2rem; text-align: center; color: #6b7280; }
    .error-state { color: #dc2626; }
    .summary-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }
    .summary-card {
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      padding: 1rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .summary-card h2 { font-size: 0.8rem; font-weight: 500; color: #6b7280; margin: 0; text-transform: uppercase; letter-spacing: 0.03em; }
    .metric { font-size: 2rem; font-weight: 700; color: #111827; line-height: 1.2; }
    .card-link { font-size: 0.8rem; color: #059669; text-decoration: none; margin-top: 0.25rem; }
    .card-link:hover { text-decoration: underline; }
    .card-link.urgent { color: #d97706; font-weight: 600; }
    .has-pending { border-color: #fbbf24; background: #fffbeb; }
    .recent-work h2 { font-size: 1rem; font-weight: 600; margin-bottom: 0.75rem; }
    .empty-state { color: #9ca3af; font-size: 0.9rem; }
    .empty-state a { color: #059669; }
    .recent-table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
    .recent-table th { text-align: left; padding: 0.4rem 0.75rem; border-bottom: 2px solid #e5e7eb; color: #6b7280; font-weight: 500; font-size: 0.8rem; }
    .recent-table td { padding: 0.5rem 0.75rem; border-bottom: 1px solid #f3f4f6; }
    .recent-row:hover td { background: #f9fafb; }
    .row-link { color: #1d4ed8; text-decoration: none; }
    .row-link:hover { text-decoration: underline; }
    ```

- [x] Task 8: Frontend — Tests (AC1, AC4)
  - [x] 8.1 Tạo `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.spec.ts`:

    **Tests cần implement (4 tests):**
    ```
    // 1. loads and displays summary metrics
    // 2. displays empty state when recentWork is empty
    // 3. displays recent work rows when data exists
    // 4. class filter change triggers reload
    ```

    **Pattern — mock DashboardApiService và ClassesApiService:**
    ```typescript
    import { TestBed } from '@angular/core/testing';
    import { provideRouter } from '@angular/router';
    import { TeacherDashboardComponent } from './teacher-dashboard.component';
    import { DashboardApiService } from '../../core/dashboard/dashboard-api.service';
    import { ClassesApiService } from '../../core/classes/classes-api.service';

    const mockDashboardApi = {
      getDashboard: vi.fn(),
    };
    const mockClassesApi = {
      getTeacherClasses: vi.fn().mockResolvedValue([]),
    };

    describe('TeacherDashboardComponent', () => {
      beforeEach(() => {
        mockDashboardApi.getDashboard.mockResolvedValue({
          summary: {
            templateCount: 3,
            activeHomeworkCount: 1,
            openLiveExamCount: 0,
            recentSubmissionCount: 5,
            pendingSpeakingCount: 2,
          },
          recentWork: [],
        });

        TestBed.configureTestingModule({
          imports: [TeacherDashboardComponent],
          providers: [
            provideRouter([]),
            { provide: DashboardApiService, useValue: mockDashboardApi },
            { provide: ClassesApiService, useValue: mockClassesApi },
          ],
        });
      });

      // tests...
    });
    ```

  - [x] 8.2 `npm test` trong `src/EnglishTestWeb.Client` — tất cả tests pass.

- [x] Task 9: Quality gate
  - [x] 9.1 `dotnet test` — tất cả API tests pass
  - [x] 9.2 `npm test` (trong `src/EnglishTestWeb.Client`) — tất cả Angular tests pass

## Dev Notes

### Bối cảnh và mục đích

Story 6.3 hoàn thành vòng lặp Epic 6 — Teacher Dashboard đã là placeholder từ Story 1.2. Component `TeacherDashboardComponent` hiện là stub rỗng (`export class TeacherDashboardComponent {}`). Story này:
1. Tạo backend endpoint mới `GET /api/teacher/dashboard` trả về summary counts + recent work
2. Chuyển `TeacherDashboardComponent` từ stub thành functional component với metrics và recent work

Dashboard là **scan surface**, không phải workflow launcher. Không tạo đề từ dashboard — dùng Thư viện đề. Navigation đã có trong shell (`teacher-shell.component.html`).

### Cấu trúc file cần thay đổi

**Backend (NEW):**
```
src/EnglishTestWeb.Api/
  Contracts/Dashboard/
    TeacherDashboardSummaryDto.cs     NEW
    TeacherRecentWorkItemDto.cs       NEW
    TeacherDashboardDto.cs            NEW
  Application/Dashboard/
    ITeacherDashboardService.cs       NEW
  Infrastructure/Dashboard/
    TeacherDashboardService.cs        NEW
  Controllers/
    TeacherDashboardController.cs     NEW
  Program.cs                          UPDATE (AddScoped)
```

**Frontend (UPDATE/NEW):**
```
src/EnglishTestWeb.Client/src/app/
  core/dashboard/
    dashboard.models.ts               NEW
    dashboard-api.service.ts          NEW
  features/teacher-dashboard/
    teacher-dashboard.component.ts    UPDATE (stub → functional)
    teacher-dashboard.component.html  UPDATE (stub → functional)
    teacher-dashboard.component.css   UPDATE (stub → functional)
    teacher-dashboard.component.spec.ts NEW
```

**Tests (NEW):**
```
tests/EnglishTestWeb.Api.Tests/
  Dashboard/
    TeacherDashboardTests.cs          NEW
```

### Patterns phải follow từ story trước

**Từ Story 6.2 (TeacherResultsController):**
- Controller pattern: `[Route("api/teacher/...")]` + `[Authorize(Roles = IdentityRoleNames.Teacher)]`
- `ICurrentUserContext.UserId` để lấy teacherId
- Return 401 via `hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized, "auth.unauthorized", ...)`

**Từ Story 6.2 (TeacherSubmissionDetailService):**
- `AsNoTracking()` trên tất cả read queries
- Batch resolve names (không N+1 per row)
- Sequential awaits — **KHÔNG dùng Task.WhenAll trên cùng DbContext**
- Count tasks được tạo (via LINQ) TRƯỚC rồi await tuần tự

**Từ Story 6.1 (TeacherResultsComponent):**
- Angular signal pattern: `signal<T>()`, `computed()`, `inject()`
- Component đã có `FormsModule` trong imports — không cần NgModule
- `ngModel` với `(ngModelChange)` — đây là pattern đúng cho Angular 22

**Từ Story 5.3 (TeacherSpeakingGradingComponent):**
- `ClassSummary` model: `classId: string`, `className: string` (KHÔNG phải `id`/`name`)
- Dropdown populate: `*ngFor` / `@for ... track cls.classId`

### Domain entities và trường quan trọng

**`HomeworkAssignment`:** `TeacherId`, `ClassId`, `DeadlineAt`, `Status = "published"`
- Active homework = `Status == "published"` && `DeadlineAt > now`

**`LiveExamSession`:** `TeacherId`, `ClassId`, `Status` = "scheduled" | "open" | "closed"
- Open = `Status == "open"`

**`Submission`:** `HomeworkAssignmentId?`, `LiveExamSessionId?`, `StudentId`, `Status`, `SubmittedAt?`
- NavProps: `HomeworkAssignment` (with `.TeacherId`, `.ClassId`, `.Template`), `LiveExamSession` (with `.TeacherId`, `.ClassId`, `.Template`)

**`SpeakingSubmission`:** `Status` = "draft" | "submitted" | "graded"
- Pending speaking = `Status == "submitted"`
- **Cần verify NavProps:** chạy `grep -n "HomeworkAssignment\|LiveExamSession" src/EnglishTestWeb.Api/Domain/Speaking/SpeakingSubmission.cs` trước khi viết query

**`TemplateStatuses`:** `Draft`, `Ready`, `Archived` — `TemplateCount` đếm tất cả (không filter status).

### Bẫy cần tránh

1. **`Task.WhenAll` trên EF DbContext:** DbContext không thread-safe — các count tasks phải await tuần tự dù được "tạo" song song.
2. **`ClassSummary` field names:** `classId` và `className` (KHÔNG phải `id`/`name`) — verify bằng grep.
3. **`DatePipe` trong standalone component:** phải thêm `DatePipe` vào `imports` array của `@Component` (Angular 22 standalone).
4. **`RouterLink` + `[queryParams]` trong template:** cần import `RouterLink` trong component imports (không phải `RouterModule`).
5. **Empty recentWork list:** component phải render empty state (AC4) không crash khi `data.recentWork.length === 0`.
6. **No "tạo đề" shortcut on dashboard (AC4):** Dashboard không được có button/link tạo template mới — tránh vi phạm FR-19.
7. **`loadComponent` trong routes đã có:** `TeacherDashboardComponent` đã được lazy-loaded trong `app.routes.ts` — **không thêm route mới**.

### Không cần làm trong story này

- Real-time refresh (WebSocket/SignalR) — out of scope
- Pagination cho recent work — MVP hiển thị tối đa 10 rows
- Drill-down detail từ dashboard — AC5 chỉ cần navigate đến module
- Dashboard metrics cho student — không scope trong story này
- Audit trail cho dashboard views — out of scope

### References

- [Story 1.2] `teacher-shell.component.html` — nav links (Dashboard/Thư viện đề/Lớp học/Kết quả đã có)
- [Story 6.1] `teacher-results.component.ts` — signal/inject pattern, FormsModule import
- [Story 6.2] `TeacherDashboardController` pattern — auth/scope pattern
- [Story 6.2] `TeacherSubmissionDetailService.cs` — sequential await pattern (EF DbContext)
- `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignment.cs` — DeadlineAt, TeacherId, ClassId
- `src/EnglishTestWeb.Api/Domain/LiveExams/LiveExamSession.cs` — Status constants
- `src/EnglishTestWeb.Api/Domain/Speaking/SpeakingSubmission.cs` — verify nav props
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs` — available DbSets
- `src/EnglishTestWeb.Client/src/app/core/classes/classes.models.ts` — ClassSummary shape
- `src/EnglishTestWeb.Client/src/app/app.routes.ts` — existing routes, no new route needed

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- Tất cả 9 tasks hoàn thành. 322/322 API tests pass, 194/194 Angular tests pass.
- `SpeakingSubmission` đã có sẵn `HomeworkAssignment` và `LiveExamSession` nav props — không cần modify domain entity.
- Count queries EF được tạo dưới dạng IQueryable rồi await tuần tự (tránh Task.WhenAll trên DbContext không thread-safe).
- `TeacherDashboardComponent` chuyển từ stub rỗng thành functional component với Angular signals, FormsModule, RouterLink, DatePipe.
- `ClassSummary` dùng `classId`/`className` — template `@for (track cls.classId)` đúng field name.
- Tests `GetDashboard_NoData_ReturnsZeroCounts` và `GetDashboard_AsStudent_Returns403` cần gọi `SeedRolesAndUsersAsync` để seed users trước khi sign in.
- Dashboard không có "tạo đề" shortcut — scan surface đúng per FR-19.
- Code Review Round 1: APPROVED — không có required patches. 4 DEFER findings (LOW severity).
- Code Review Round 2: PATCH applied — thay C# ternary (`? :`) bằng `||` (OR) trong 3 LINQ queries (recentSubmissionCount, pendingSpeakingCount, recentSubmissions) để đảm bảo EF Core SQL Server translation. 322/322 tests pass sau patch.

### File List

**Backend (NEW):**
- `src/EnglishTestWeb.Api/Contracts/Dashboard/TeacherDashboardSummaryDto.cs`
- `src/EnglishTestWeb.Api/Contracts/Dashboard/TeacherRecentWorkItemDto.cs`
- `src/EnglishTestWeb.Api/Contracts/Dashboard/TeacherDashboardDto.cs`
- `src/EnglishTestWeb.Api/Application/Dashboard/ITeacherDashboardService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Dashboard/TeacherDashboardService.cs`
- `src/EnglishTestWeb.Api/Controllers/TeacherDashboardController.cs`
- `tests/EnglishTestWeb.Api.Tests/Dashboard/TeacherDashboardTests.cs`

**Backend (UPDATED):**
- `src/EnglishTestWeb.Api/Program.cs`

**Frontend (NEW):**
- `src/EnglishTestWeb.Client/src/app/core/dashboard/dashboard.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/dashboard/dashboard-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.spec.ts`

**Frontend (UPDATED):**
- `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.html`
- `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.css`
