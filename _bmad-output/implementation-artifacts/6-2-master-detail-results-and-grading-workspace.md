---
baseline_commit: 51bf0d7
---

# Story 6.2: Master-Detail Results And Grading Workspace

Status: done

## Story

Là giáo viên,
tôi muốn một master-detail grading workspace,
để tôi có thể xem chi tiết bài nộp và chấm điểm Speaking mà không mất ngữ cảnh danh sách kết quả.

## Acceptance Criteria

1. **Given** các result rows đã được tải
   **When** giáo viên chọn một row
   **Then** detail panel mở ra bên cạnh (hoặc bên dưới) bảng, không navigate sang trang khác
   **And** row được chọn có trạng thái visual rõ ràng (highlighted)
   **And** panel có nút "Đóng" để đóng detail.

2. **Given** row được chọn là Reading hoặc Listening
   **When** detail tải
   **Then** hiển thị bảng tóm tắt câu trả lời: số thứ tự câu, đáp án học sinh, đáp án đúng, đúng/sai
   **And** hiển thị AutoScore (tổng điểm tự chấm).

3. **Given** row được chọn là Speaking
   **When** detail tải
   **Then** hiển thị audio player (reuse behavior từ Story 5.3)
   **And** hiển thị score input (0–10), feedback textarea, nút "Lưu chấm điểm"
   **And** trạng thái grading (idle/submitting/success/error) được xử lý đúng.

4. **Given** giáo viên lưu chấm điểm Speaking thành công
   **When** save hoàn thành
   **Then** row status và score trong bảng được cập nhật ngay (không mất filter hiện tại)
   **And** "Đã chấm" badge xuất hiện trên row.

5. **Given** còn có Speaking submissions với status "submitted" trong kết quả hiện tại
   **When** giáo viên nhấn "Chấm tiếp"
   **Then** detail panel chuyển sang row Speaking pending tiếp theo trong kết quả đã lọc.

6. **Given** workspace được thao tác bằng bàn phím
   **When** focus di chuyển giữa filters, table, detail panel, player, score, feedback, save
   **Then** focus order đúng thứ tự visual và luôn visible.

7. **Given** filter bar hiện tại thiếu class filter và template filter (deferred từ 6.1)
   **When** trang results mở
   **Then** filter bar bổ sung dropdown "Lớp" (populated từ `GET /api/classes`) và dropdown "Đề" (populated từ templates của giáo viên)
   **And** chọn lớp/đề trigger filter reload như các filter khác.

## Tasks / Subtasks

- [x] Task 1: Backend — TeacherSubmissionDetailDto và endpoint (AC2)
  - [x] 1.1 Tạo `src/EnglishTestWeb.Api/Contracts/Results/TeacherAnswerRowDto.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Results;

    public sealed record TeacherAnswerRowDto(
        int QuestionNumber,
        string? StudentAnswer,
        string CorrectAnswer,
        bool? IsCorrect,
        decimal? Score);
    ```
  - [x] 1.2 Tạo `src/EnglishTestWeb.Api/Contracts/Results/TeacherSubmissionDetailDto.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Results;

    public sealed record TeacherSubmissionDetailDto(
        Guid Id,
        string StudentName,
        string ClassName,
        string TemplateTitle,
        string Skill,           // "reading" | "listening"
        string Mode,            // "homework" | "live-exam"
        string Status,          // "submitted" | "auto-graded"
        decimal? AutoScore,
        DateTimeOffset? SubmittedAt,
        IReadOnlyList<TeacherAnswerRowDto> Answers);
    ```
  - [x] 1.3 Tạo `src/EnglishTestWeb.Api/Application/Results/ITeacherSubmissionDetailService.cs`:
    ```csharp
    using EnglishTestWeb.Api.Contracts.Results;

    namespace EnglishTestWeb.Api.Application.Results;

    public interface ITeacherSubmissionDetailService
    {
        Task<(bool Success, string? ErrorCode, TeacherSubmissionDetailDto? Dto)> GetForTeacherAsync(
            Guid submissionId,
            string teacherId,
            CancellationToken cancellationToken = default);
    }
    ```
  - [x] 1.4 Tạo `src/EnglishTestWeb.Api/Infrastructure/Results/TeacherSubmissionDetailService.cs`:

    **Logic:**
    1. Load `Submission` with `HomeworkAssignment.Template` + `LiveExamSession.Template` + `Answers`
    2. Teacher scope check: `HomeworkAssignment.TeacherId == teacherId || LiveExamSession.TeacherId == teacherId`
    3. Load AnswerKeyVersion từ `submission.AnswerKeyVersionId` (nếu có) để lấy CorrectAnswers
    4. Batch resolve studentName và className
    5. Map từng `SubmissionAnswer` sang `TeacherAnswerRowDto`

    ```csharp
    using System.Text.Json;
    using EnglishTestWeb.Api.Application.Results;
    using EnglishTestWeb.Api.Contracts.Results;
    using EnglishTestWeb.Api.Domain.TestTemplates;
    using EnglishTestWeb.Api.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    namespace EnglishTestWeb.Api.Infrastructure.Results;

    public sealed class TeacherSubmissionDetailService(EnglishTestWebDbContext db)
        : ITeacherSubmissionDetailService
    {
        public async Task<(bool Success, string? ErrorCode, TeacherSubmissionDetailDto? Dto)> GetForTeacherAsync(
            Guid submissionId,
            string teacherId,
            CancellationToken cancellationToken = default)
        {
            var submission = await db.Submissions
                .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
                .Include(s => s.LiveExamSession).ThenInclude(l => l!.Template)
                .Include(s => s.Answers)
                .Where(s => s.Id == submissionId)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (submission is null)
                return (false, "submission.notFound", null);

            var sourceTeacherId = submission.HomeworkAssignment?.TeacherId
                               ?? submission.LiveExamSession?.TeacherId;
            if (sourceTeacherId != teacherId)
                return (false, "submission.notFound", null);

            var template = submission.HomeworkAssignment?.Template
                        ?? submission.LiveExamSession?.Template;
            var classId  = submission.HomeworkAssignment?.ClassId
                        ?? submission.LiveExamSession?.ClassId
                        ?? Guid.Empty;
            var mode     = submission.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";

            // Batch resolve names
            var studentName = await db.Users
                .Where(u => u.Id == submission.StudentId)
                .Select(u => u.UserName ?? u.Email ?? submission.StudentId)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken) ?? submission.StudentId;

            var className = await db.Classes
                .Where(c => c.Id == classId)
                .Select(c => c.Name)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            // Load AnswerKeyVersion for correct answers
            Dictionary<int, AnswerKeyRow> correctAnswers = [];
            if (submission.AnswerKeyVersionId.HasValue)
            {
                var akv = await db.AnswerKeyVersions
                    .Where(a => a.Id == submission.AnswerKeyVersionId.Value)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);

                if (akv is not null)
                {
                    try
                    {
                        var rows = JsonSerializer.Deserialize<List<AnswerKeyRow>>(akv.RowsJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
                        correctAnswers = rows.ToDictionary(r => r.QuestionNumber);
                    }
                    catch (JsonException) { /* corrupt RowsJson — show empty correct answers */ }
                }
            }

            var answerRows = submission.Answers
                .OrderBy(a => a.QuestionNumber)
                .Select(a =>
                {
                    correctAnswers.TryGetValue(a.QuestionNumber, out var akRow);
                    return new TeacherAnswerRowDto(
                        QuestionNumber: a.QuestionNumber,
                        StudentAnswer:  a.Answer,
                        CorrectAnswer:  akRow?.CorrectAnswer ?? string.Empty,
                        IsCorrect:      a.IsCorrect,
                        Score:          a.Score);
                })
                .ToList();

            var dto = new TeacherSubmissionDetailDto(
                Id:             submission.Id,
                StudentName:    studentName,
                ClassName:      className,
                TemplateTitle:  template?.Title ?? string.Empty,
                Skill:          template?.Skill ?? string.Empty,
                Mode:           mode,
                Status:         submission.Status,
                AutoScore:      submission.AutoScore,
                SubmittedAt:    submission.SubmittedAt,
                Answers:        answerRows);

            return (true, null, dto);
        }
    }
    ```

    **QUAN TRỌNG:**
    - `AnswerKeyRow` là `record(int QuestionNumber, string CorrectAnswer, decimal? Score)` trong `Domain/TestTemplates/`
    - `db.AnswerKeyVersions` — DbSet đã có trong `EnglishTestWebDbContext`
    - `AsNoTracking()` trước `FirstOrDefaultAsync()` — đúng thứ tự
    - Không truy vấn N+1: batch studentName và className (2 queries riêng, không phải per-row)

  - [x] 1.5 Thêm route vào `TeacherResultsController.cs`:
    ```csharp
    // Inject ITeacherSubmissionDetailService trong constructor
    [HttpGet("submissions/{submissionId:guid}")]
    public async Task<ActionResult<TeacherSubmissionDetailDto>> GetSubmissionDetail(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                "auth.unauthorized", "Unauthorized.", "Authentication required.");

        var (success, errorCode, dto) = await submissionDetailService.GetForTeacherAsync(
            submissionId, teacherId, cancellationToken);

        if (!success)
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status404NotFound,
                errorCode!, "Not found.", "Submission not found or out of scope.");

        return Ok(dto);
    }
    ```
    URL: `GET /api/teacher/results/submissions/{submissionId}`
  - [x] 1.6 Đăng ký DI trong `Program.cs`:
    ```csharp
    builder.Services.AddScoped<ITeacherSubmissionDetailService, TeacherSubmissionDetailService>();
    ```
  - [x] 1.7 `dotnet build` — xác nhận build thành công.

- [x] Task 2: Backend — Tests (AC2, AC3)
  - [x] 2.1 Tạo `tests/EnglishTestWeb.Api.Tests/Results/TeacherSubmissionDetailTests.cs`:

    **Tests cần implement (6 tests):**
    ```
    GetSubmissionDetail_AsTeacher_ReturnsDetail_WithAnswers
    GetSubmissionDetail_SpeakingRowId_Returns404 (speaking submissions route khác)
    GetSubmissionDetail_OutOfScope_Returns404
    GetSubmissionDetail_Unauthenticated_Returns401
    GetSubmissionDetail_AsStudent_Returns403
    GetSubmissionDetail_NotFound_Returns404
    ```

    **Pattern — reuse ResultsTestHelper:**
    ```csharp
    await using var factory = new TestApiFactory();
    var (homeworkId, classId, templateId) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

    using var studentClient = factory.CreateClient();
    await AuthTestHelper.SignInStudentAsync(studentClient);
    var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
    var submissionId = await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(factory, homeworkId, studentId);

    using var client = factory.CreateClient();
    await AuthTestHelper.SignInTeacherAsync(client);
    var resp = await client.GetAsync($"/api/teacher/results/submissions/{submissionId}");
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    // parse JSON, assert dto.AutoScore, dto.Answers.Count > 0
    ```
  - [x] 2.2 `dotnet test` — tất cả tests pass.

- [x] Task 3: Frontend — Models, API service (AC2, AC3)
  - [x] 3.1 Thêm vào `src/EnglishTestWeb.Client/src/app/core/results/results.models.ts`:
    ```typescript
    export interface TeacherAnswerRowDto {
      questionNumber: number;
      studentAnswer: string | null;
      correctAnswer: string;
      isCorrect: boolean | null;
      score: number | null;
    }

    export interface TeacherSubmissionDetailDto {
      id: string;
      studentName: string;
      className: string;
      templateTitle: string;
      skill: 'reading' | 'listening';
      mode: 'homework' | 'live-exam';
      status: string;
      autoScore: number | null;
      submittedAt: string | null;
      answers: TeacherAnswerRowDto[];
    }
    ```
  - [x] 3.2 Thêm method vào `src/EnglishTestWeb.Client/src/app/core/results/results-api.service.ts`:
    ```typescript
    getSubmissionDetail(submissionId: string): Promise<TeacherSubmissionDetailDto> {
      return firstValueFrom(
        this.http.get<TeacherSubmissionDetailDto>(
          `/api/teacher/results/submissions/${submissionId}`,
        ),
      );
    }
    ```

- [x] Task 4: Frontend — Split-panel layout và detail panel (AC1, AC2, AC3, AC4, AC5, AC6)
  - [x] 4.1 Cập nhật `teacher-results.component.ts` — thêm detail panel logic:

    **Thêm imports:**
    ```typescript
    import { SpeakingApiService } from '../../core/speaking/speaking-api.service';
    import { TeacherSpeakingSubmissionDto, GradeSpeakingRequest, SPEAKING_ERROR_MESSAGES } from '../../core/speaking/speaking.models';
    import { TeacherSubmissionDetailDto } from '../../core/results/results.models';
    import { ClassesApiService } from '../../core/classes/classes-api.service';
    import { ClassSummary } from '../../core/classes/classes.models';
    import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
    import { TestTemplateListItem } from '../../core/test-templates/test-templates.models';
    import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
    ```

    **Thêm injections:**
    ```typescript
    private readonly speakingApi = inject(SpeakingApiService);
    private readonly resultsApi = inject(ResultsApiService);
    private readonly classesApi = inject(ClassesApiService);
    private readonly templatesApi = inject(TestTemplatesApiService);
    private readonly sanitizer = inject(DomSanitizer);
    ```

    **Thêm filter signals cho class và template:**
    ```typescript
    protected readonly filterClass    = signal<string>('');
    protected readonly filterTemplate = signal<string>('');
    protected readonly availableClasses   = signal<ClassSummary[]>([]);
    protected readonly availableTemplates = signal<TestTemplateListItem[]>([]);
    ```

    **Detail panel signals:**
    ```typescript
    type DetailState = 'closed' | 'loading' | 'rl-loaded' | 'speaking-loaded' | 'error';
    type GradeState  = 'idle' | 'submitting' | 'success' | 'error';

    protected readonly detailState         = signal<DetailState>('closed');
    protected readonly rlDetail            = signal<TeacherSubmissionDetailDto | null>(null);
    protected readonly speakingDetail      = signal<TeacherSpeakingSubmissionDto | null>(null);
    protected readonly detailErrorMessage  = signal<string | null>(null);

    // Grading signals (for speaking panel)
    protected readonly scoreInput          = signal<string>('');
    protected readonly feedbackInput       = signal<string>('');
    protected readonly gradeState          = signal<GradeState>('idle');
    protected readonly gradeErrorMessage   = signal<string | null>(null);

    // Safe audio URL
    protected readonly audioUrl = computed((): SafeResourceUrl | null => {
      const row = this.results()?.items.find(r => r.id === this.selectedRowId());
      if (!row || row.type !== 'speaking') return null;
      const url = this.speakingApi.getTeacherSubmissionFileUrl(row.id);
      return this.sanitizer.bypassSecurityTrustResourceUrl(url);
    });
    ```

    **Load filter dropdowns trong ngOnInit:**
    ```typescript
    ngOnInit(): void {
      void this.loadFilterDropdowns();
      void this.loadResults();
    }

    private async loadFilterDropdowns(): Promise<void> {
      const [classes, templates] = await Promise.all([
        this.classesApi.getTeacherClasses().catch(() => []),
        this.templatesApi.listTemplates({}).catch(() => []),
      ]);
      this.availableClasses.set(classes);
      this.availableTemplates.set(templates);
    }
    ```

    **onSelectRow — mở detail panel:**
    ```typescript
    protected onSelectRow(row: ResultRowDto): void {
      if (this.selectedRowId() === row.id) return; // đã chọn
      this.selectedRowId.set(row.id);
      this.gradeState.set('idle');
      this.gradeErrorMessage.set(null);
      void this.loadDetail(row);
    }

    protected onCloseDetail(): void {
      this.selectedRowId.set(null);
      this.detailState.set('closed');
      this.rlDetail.set(null);
      this.speakingDetail.set(null);
    }

    private async loadDetail(row: ResultRowDto): Promise<void> {
      this.detailState.set('loading');
      this.detailErrorMessage.set(null);
      try {
        if (row.type === 'speaking') {
          const dto = await this.speakingApi.getForTeacher(row.id);
          this.speakingDetail.set(dto);
          this.scoreInput.set(dto.score !== null ? String(dto.score) : '');
          this.feedbackInput.set(dto.feedback ?? '');
          this.detailState.set('speaking-loaded');
        } else {
          const dto = await this.resultsApi.getSubmissionDetail(row.id);
          this.rlDetail.set(dto);
          this.detailState.set('rl-loaded');
        }
      } catch {
        this.detailState.set('error');
        this.detailErrorMessage.set('Không thể tải chi tiết. Vui lòng thử lại.');
      }
    }
    ```

    **Grading Speaking:**
    ```typescript
    protected async onGradeSubmit(): Promise<void> {
      if (this.gradeState() === 'submitting') return;
      const scoreStr = this.scoreInput().trim();
      const score = scoreStr === '' ? null : Number(scoreStr);
      if (score === null || !Number.isInteger(score) || score < 0 || score > 10) {
        this.gradeErrorMessage.set('Điểm số phải là số nguyên từ 0 đến 10.');
        return;
      }
      const rowId = this.selectedRowId();
      if (!rowId) return;

      this.gradeState.set('submitting');
      this.gradeErrorMessage.set(null);
      const request: GradeSpeakingRequest = {
        score,
        feedback: this.feedbackInput().trim() || null,
      };
      try {
        const updated = await this.speakingApi.grade(rowId, request);
        this.speakingDetail.set(updated);
        this.gradeState.set('success');
        // Cập nhật row trong danh sách (AC4)
        this.updateResultRow(rowId, 'graded', score);
      } catch (err: unknown) {
        this.gradeState.set('error');
        const code = this.extractErrorCode(err);
        this.gradeErrorMessage.set(
          SPEAKING_ERROR_MESSAGES[code ?? ''] ?? 'Chấm điểm thất bại. Vui lòng thử lại.',
        );
      }
    }

    private updateResultRow(rowId: string, newStatus: string, score: number): void {
      const current = this.results();
      if (!current) return;
      const updatedItems = current.items.map(r =>
        r.id === rowId ? { ...r, status: newStatus, score } : r,
      );
      this.results.set({ ...current, items: updatedItems });
    }

    private extractErrorCode(err: unknown): string | null {
      if (err && typeof err === 'object' && 'error' in err) {
        const body = (err as { error: unknown }).error;
        if (body && typeof body === 'object' && 'extensions' in body) {
          const ext = (body as { extensions: unknown }).extensions;
          if (ext && typeof ext === 'object' && 'code' in ext)
            return String((ext as { code: unknown }).code);
        }
      }
      return null;
    }
    ```

    **Next pending Speaking (AC5):**
    ```typescript
    protected readonly nextPendingSpeakingRow = computed((): ResultRowDto | null => {
      const currentId = this.selectedRowId();
      const items = this.results()?.items ?? [];
      const pendingSpeaking = items.filter(r => r.type === 'speaking' && r.status === 'submitted');
      if (pendingSpeaking.length === 0) return null;
      const currentIdx = pendingSpeaking.findIndex(r => r.id === currentId);
      if (currentIdx === -1) return pendingSpeaking[0] ?? null;
      return pendingSpeaking[currentIdx + 1] ?? pendingSpeaking[0] ?? null;
    });

    protected onNextPending(): void {
      const next = this.nextPendingSpeakingRow();
      if (next) void this.onSelectRow(next);
    }
    ```

    **Filter thay đổi — cập nhật loadResults để dùng filterClass và filterTemplate:**
    ```typescript
    // Cập nhật loadResults filter object:
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
    ```

    **onClearFilters — thêm reset filterClass và filterTemplate:**
    ```typescript
    protected onClearFilters(): void {
      if (this.debounceTimer !== null) {
        clearTimeout(this.debounceTimer);
        this.debounceTimer = null;
      }
      this.filterClass.set('');
      this.filterMode.set('');
      this.filterTemplate.set('');
      this.filterStudent.set('');
      this.filterSkill.set('');
      this.filterStatus.set('');
      this.currentPage.set(1);
      void this.loadResults();
    }
    ```

    **Thêm DomSanitizer vào imports của component:**
    Component cần `imports: [FormsModule]` — KHÔNG cần thêm gì khác (DomSanitizer là service không phải module).

  - [x] 4.2 Cập nhật `teacher-results.component.html` — thêm class/template filters và detail panel:

    **Bổ sung class filter dropdown (AC7) vào filter bar:**
    ```html
    <select [ngModel]="filterClass()" (ngModelChange)="filterClass.set($event); onFilterChange()">
      <option value="">Tất cả lớp</option>
      @for (cls of availableClasses(); track cls.id) {
        <option [value]="cls.id">{{ cls.name }}</option>
      }
    </select>

    <select [ngModel]="filterTemplate()" (ngModelChange)="filterTemplate.set($event); onFilterChange()">
      <option value="">Tất cả đề</option>
      @for (tmpl of availableTemplates(); track tmpl.id) {
        <option [value]="tmpl.id">{{ tmpl.title }}</option>
      }
    </select>
    ```
    Thêm 2 select này vào filter bar trước select mode.

    **Cấu trúc split-panel (bao quanh toàn bộ khu vực bên dưới filter bar):**
    ```html
    <div class="workspace" [class.has-detail]="detailState() !== 'closed'">
      <!-- List panel (left) -->
      <div class="list-panel">
        <!-- ... (toàn bộ nội dung table hiện tại: loading/error/table/pagination) ... -->
      </div>

      <!-- Detail panel (right) — chỉ render khi detail open -->
      @if (detailState() !== 'closed') {
        <div class="detail-panel" role="complementary" aria-label="Chi tiết bài nộp">
          <div class="detail-header">
            <h2 class="detail-title">Chi tiết bài nộp</h2>
            <button
              type="button"
              class="close-btn"
              (click)="onCloseDetail()"
              aria-label="Đóng chi tiết"
            >✕</button>
          </div>

          @if (detailState() === 'loading') {
            <div class="detail-loading">Đang tải...</div>
          }

          @if (detailState() === 'error') {
            <div class="detail-error" role="alert">{{ detailErrorMessage() }}</div>
          }

          <!-- RL Detail (AC2) -->
          @if (detailState() === 'rl-loaded' && rlDetail(); as detail) {
            <div class="rl-detail">
              <div class="detail-meta">
                <span>{{ detail.studentName }}</span> · <span>{{ detail.className }}</span> · <span>{{ detail.templateTitle }}</span>
              </div>
              @if (detail.autoScore !== null) {
                <div class="auto-score">
                  Tổng điểm: <strong>{{ detail.autoScore.toFixed(1) }}</strong>
                </div>
              }
              @if (detail.answers.length > 0) {
                <table class="answers-table">
                  <thead>
                    <tr>
                      <th>Câu</th>
                      <th>Học sinh</th>
                      <th>Đáp án đúng</th>
                      <th>Kết quả</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (ans of detail.answers; track ans.questionNumber) {
                      <tr [class.correct]="ans.isCorrect === true" [class.incorrect]="ans.isCorrect === false">
                        <td>{{ ans.questionNumber }}</td>
                        <td>{{ ans.studentAnswer ?? '—' }}</td>
                        <td>{{ ans.correctAnswer || '—' }}</td>
                        <td>
                          @if (ans.isCorrect === true) { <span class="correct-badge">✓</span> }
                          @if (ans.isCorrect === false) { <span class="incorrect-badge">✗</span> }
                          @if (ans.isCorrect === null) { <span>—</span> }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              } @else {
                <p class="no-answers">Chưa có câu trả lời.</p>
              }
            </div>
          }

          <!-- Speaking Detail (AC3) -->
          @if (detailState() === 'speaking-loaded' && speakingDetail(); as speaking) {
            <div class="speaking-detail">
              <div class="detail-meta">
                <span>{{ speaking.studentName }}</span> · <span>{{ speaking.className }}</span> · <span>{{ speaking.templateTitle }}</span>
              </div>

              <!-- Audio player -->
              @if (speaking.submittedFileId && !speaking.isFileMissing) {
                <div class="audio-section">
                  <p class="file-info">{{ speaking.submittedFileName }} ({{ speaking.submittedFileSizeBytes ? formatFileSize(speaking.submittedFileSizeBytes) : '' }})</p>
                  <audio
                    [src]="audioUrl()"
                    controls
                    class="audio-player"
                    aria-label="File nói của học sinh"
                  ></audio>
                </div>
              }
              @if (speaking.isFileMissing) {
                <div class="file-missing" role="alert">File không tìm thấy.</div>
              }

              <!-- Grading form -->
              @if (speaking.status !== 'draft') {
                <div class="grade-form">
                  <label for="scoreInput">Điểm (0–10)</label>
                  <input
                    id="scoreInput"
                    type="number"
                    min="0"
                    max="10"
                    step="1"
                    [ngModel]="scoreInput()"
                    (ngModelChange)="scoreInput.set($event)"
                    [disabled]="gradeState() === 'submitting'"
                  />

                  <label for="feedbackInput">Nhận xét</label>
                  <textarea
                    id="feedbackInput"
                    rows="3"
                    [ngModel]="feedbackInput()"
                    (ngModelChange)="feedbackInput.set($event)"
                    [disabled]="gradeState() === 'submitting'"
                    placeholder="Nhận xét cho học sinh (tuỳ chọn)"
                  ></textarea>

                  @if (gradeErrorMessage()) {
                    <div class="grade-error" role="alert">{{ gradeErrorMessage() }}</div>
                  }
                  @if (gradeState() === 'success') {
                    <div class="grade-success">Đã lưu chấm điểm.</div>
                  }

                  <button
                    type="button"
                    class="save-btn"
                    (click)="onGradeSubmit()"
                    [disabled]="gradeState() === 'submitting'"
                  >
                    {{ gradeState() === 'submitting' ? 'Đang lưu...' : 'Lưu chấm điểm' }}
                  </button>

                  <!-- Next pending (AC5) -->
                  @if (nextPendingSpeakingRow()) {
                    <button
                      type="button"
                      class="next-btn"
                      (click)="onNextPending()"
                    >Chấm tiếp →</button>
                  }
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
    ```

  - [x] 4.3 Cập nhật `teacher-results.component.css` — thêm split-panel styles:
    ```css
    .workspace { display: flex; gap: 0; }
    .list-panel { flex: 1 1 auto; min-width: 0; }
    .workspace.has-detail .list-panel { max-width: 60%; }
    .detail-panel {
      flex: 0 0 38%;
      min-width: 320px;
      border-left: 1px solid #e5e7eb;
      padding: 1rem 1.25rem;
      overflow-y: auto;
      max-height: calc(100vh - 120px);
      background: #fff;
    }
    .detail-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .detail-title { font-size: 1rem; font-weight: 600; margin: 0; }
    .close-btn { background: none; border: none; font-size: 1.2rem; cursor: pointer; color: #6b7280; padding: 0.25rem; }
    .close-btn:hover { color: #111827; }
    .detail-loading, .detail-error { padding: 1rem; text-align: center; color: #6b7280; }
    .detail-error { color: #dc2626; }
    .detail-meta { font-size: 0.85rem; color: #6b7280; margin-bottom: 0.75rem; }
    .auto-score { font-size: 1rem; margin-bottom: 0.75rem; }
    .answers-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
    .answers-table th { text-align: left; padding: 0.35rem 0.5rem; border-bottom: 2px solid #e5e7eb; }
    .answers-table td { padding: 0.35rem 0.5rem; border-bottom: 1px solid #f3f4f6; }
    .answers-table tr.correct { background: #f0fdf4; }
    .answers-table tr.incorrect { background: #fef2f2; }
    .correct-badge { color: #16a34a; font-weight: bold; }
    .incorrect-badge { color: #dc2626; font-weight: bold; }
    .no-answers { color: #9ca3af; font-size: 0.85rem; }
    .audio-section { margin-bottom: 1rem; }
    .audio-player { width: 100%; margin-top: 0.5rem; }
    .file-info { font-size: 0.8rem; color: #6b7280; margin: 0 0 0.25rem; }
    .file-missing { color: #dc2626; padding: 0.5rem; background: #fef2f2; border-radius: 0.375rem; }
    .grade-form { display: flex; flex-direction: column; gap: 0.5rem; margin-top: 1rem; }
    .grade-form label { font-size: 0.85rem; font-weight: 600; color: #374151; }
    .grade-form input[type="number"] { width: 80px; padding: 0.4rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .grade-form textarea { padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 0.375rem; resize: vertical; font-size: 0.9rem; }
    .grade-error { color: #dc2626; font-size: 0.85rem; }
    .grade-success { color: #059669; font-size: 0.85rem; }
    .save-btn { padding: 0.5rem 1.25rem; background: #059669; color: #fff; border: none; border-radius: 0.375rem; cursor: pointer; font-size: 0.9rem; }
    .save-btn:disabled { opacity: 0.6; cursor: default; }
    .next-btn { padding: 0.4rem 1rem; background: #f9fafb; border: 1px solid #d1d5db; border-radius: 0.375rem; cursor: pointer; font-size: 0.85rem; }
    .next-btn:hover { background: #f3f4f6; }
    ```

  - [x] 4.4 Thêm `formatFileSize` vào component (đã có trong TeacherSpeakingGradingComponent):
    ```typescript
    protected formatFileSize(bytes: number): string {
      if (bytes < 1024) return `${bytes} B`;
      if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }
    ```

- [x] Task 5: Frontend — Tests (AC1, AC3, AC4, AC7)
  - [x] 5.1 Cập nhật `teacher-results.component.spec.ts` — thêm tests cho detail panel:

    **Tests cần thêm (tối thiểu 4 tests mới):**
    ```
    // 6. Row selection opens detail panel (detailState changes to 'loading' then 'rl-loaded' or 'speaking-loaded')
    // 7. Close button hides detail panel
    // 8. Grade submit calls speakingApi.grade and updates row status
    // 9. onClearFilters resets filterClass and filterTemplate signals
    ```

    **Mock SpeakingApiService và ResultsApiService:**
    ```typescript
    const mockSpeakingApi = {
      getForTeacher: vi.fn(),
      grade: vi.fn(),
      getTeacherSubmissionFileUrl: vi.fn().mockReturnValue('/api/mock-url'),
    };
    const mockResultsApi = {
      getResults: vi.fn(),
      getSubmissionDetail: vi.fn(),
    };
    // Thêm DomSanitizer mock hoặc cung cấp thực
    ```
  - [x] 5.2 `npm test` trong `src/EnglishTestWeb.Client` — tất cả tests pass.

- [x] Task 6: Quality gate
  - [x] 6.1 `dotnet test` — tất cả API tests pass
  - [x] 6.2 `npm test` (trong `src/EnglishTestWeb.Client`) — tất cả Angular tests pass

## Dev Notes

### Bối cảnh và mục đích

Story 6.2 xây dựng master-detail grading workspace trên nền `teacher-results` đã có từ Story 6.1. Khi giáo viên click một row trong bảng, một detail panel mở ra bên phải (split layout):
- **RL rows**: bảng tóm tắt câu trả lời + auto_score
- **Speaking rows**: audio player + grading form (reuse pattern từ Story 5.3)

Story này cũng bổ sung class/template filter dropdowns (deferred từ Story 6.1).

### Cấu trúc file cần thay đổi

**Backend (NEW):**
```
src/EnglishTestWeb.Api/
  Contracts/Results/
    TeacherAnswerRowDto.cs            NEW
    TeacherSubmissionDetailDto.cs     NEW
  Application/Results/
    ITeacherSubmissionDetailService.cs  NEW
  Infrastructure/Results/
    TeacherSubmissionDetailService.cs   NEW
  Controllers/
    TeacherResultsController.cs       UPDATE (thêm injection + route)
  Program.cs                          UPDATE (AddScoped)
```

**Frontend (UPDATE):**
```
src/EnglishTestWeb.Client/src/app/
  core/results/
    results.models.ts                 UPDATE (thêm TeacherAnswerRowDto, TeacherSubmissionDetailDto)
    results-api.service.ts            UPDATE (thêm getSubmissionDetail)
  features/teacher-results/
    teacher-results.component.ts      UPDATE (split-panel, detail, grading, next-pending)
    teacher-results.component.html    UPDATE (filter bar + detail panel)
    teacher-results.component.css     UPDATE (workspace/list-panel/detail-panel styles)
    teacher-results.component.spec.ts UPDATE (thêm detail panel tests)
```

**Tests (NEW):**
```
tests/EnglishTestWeb.Api.Tests/
  Results/
    TeacherSubmissionDetailTests.cs   NEW
```

### Patterns từ story trước — quan trọng phải follow

**Từ Story 6.1 (teacher-results component):**
- Component đã có `selectedRowId` signal — cần expand thành full detail state
- Component đã có `debounceTimer` + `currentRequestId` pattern — **giữ nguyên**
- `ResultRowDto.type` phân biệt "reading-listening" vs "speaking" — dùng để branch detail logic

**Từ Story 5.3 (TeacherSpeakingGradingComponent):**
- `SpeakingApiService.getForTeacher(id)` trả `TeacherSpeakingSubmissionDto`
- `SpeakingApiService.grade(id, request)` trả updated `TeacherSpeakingSubmissionDto`
- `SpeakingApiService.getTeacherSubmissionFileUrl(id)` trả URL string (cần `DomSanitizer.bypassSecurityTrustResourceUrl`)
- Score validation: integer 0–10
- Audio player dùng `<audio [src]="..." controls>` với SafeResourceUrl

**Từ TeacherSpeakingGradingService (Infrastructure):**
- Teacher scope check: `HomeworkAssignment?.TeacherId ?? LiveExamSession?.TeacherId != teacherId → notFound`
- `BuildDtoAsync`: batch resolve studentName + className — đây là pattern cho `TeacherSubmissionDetailService` cũng phải follow

**Từ ResultsService (Infrastructure):**
- Đã có `db.AnswerKeyVersions` trong context — có thể query thẳng
- `AnswerKeyRow` là record với `QuestionNumber`, `CorrectAnswer`, `Score` — deserialize từ `RowsJson`

### Dữ liệu domain quan trọng

**`Submission.AnswerKeyVersionId`:**
- Được set khi auto-grade (Story 4.4) — đây là "snapshot" của AnswerKey tại thời điểm chấm
- Khi `AnswerKeyVersionId` là null (submission chưa auto-graded hoặc submitted trước khi có AnswerKey), trả answers list rỗng hoặc không có CorrectAnswer

**`SubmissionAnswer`:**
- `IsCorrect` và `Score` được set trong auto-grade flow (Story 4.4)
- Nếu submission status là "submitted" (chưa auto-grade), `IsCorrect` có thể null

**Speaking grading:**
- Sau khi `speakingApi.grade()` thành công, cần cập nhật row trong `results` signal (AC4):
  - `row.status = 'graded'`
  - `row.score = score`
  - Không reload toàn bộ results — update in-place để giữ filter state

### Các bẫy thường gặp cần tránh

1. **DomSanitizer trong standalone component:** `inject(DomSanitizer)` — không cần import module, đây là service của Angular.
2. **`<audio [src]="audioUrl()">`:** `audioUrl()` phải là `SafeResourceUrl` (từ `bypassSecurityTrustResourceUrl`) — nếu không có UNSAFE URL warning sẽ bị block.
3. **`computed()` cho audioUrl:** Chỉ tính khi `selectedRowId` thay đổi — đây là pattern đúng.
4. **`db.AnswerKeyVersions`:** DbSet này đã được đăng ký trong `EnglishTestWebDbContext` — verify bằng `grep AnswerKeyVersions` trước khi dùng.
5. **Route URL của detail:** `GET /api/teacher/results/submissions/{id}` — nằm dưới `TeacherResultsController` với `[Route("api/teacher/results")]`, thêm `[HttpGet("submissions/{submissionId:guid}")]`.
6. **`listTemplates({})` trong dropdown:** `TestTemplatesApiService.listTemplates()` nhận `TestTemplateListFilters` — truyền `{}` để lấy tất cả (không filter). Templates có thể là Draft/Ready/Archived — tất cả đều có thể đã được giao.
7. **`Promise.all` cho loadFilterDropdowns:** Hai requests song song, catch riêng từng cái để một fail không block cái kia.
8. **`[disabled]` trên `<textarea>`:** Angular hỗ trợ — không cần `[attr.disabled]`.
9. **Keyboard focus (AC6):** Detail panel cần `tabindex="-1"` và focus khi mở (dùng `ViewChild` + `focus()` hoặc đơn giản là button close có `autofocus`).
10. **`[class.has-detail]="detailState() !== 'closed'"` trên `.workspace`:** Đây là cách thêm class conditional trong Angular — đúng syntax.

### Không cần làm trong story này

- Navigation sang `/teacher/results/{id}` route (không route-based, panel-based)
- Backend new API for Speaking (đã có: `GET /api/teacher/speaking-submissions/{id}`)
- Pagination trong detail answers (MVP: hiển thị tất cả answers trong panel)
- Real-time update khi teacher khác grade (out of scope)

### Thông tin kỹ thuật bổ sung

**Imports C# cho `TeacherSubmissionDetailService`:**
```csharp
using System.Text.Json;
using EnglishTestWeb.Api.Application.Results;
using EnglishTestWeb.Api.Contracts.Results;
using EnglishTestWeb.Api.Domain.TestTemplates;          // AnswerKeyRow
using EnglishTestWeb.Api.Infrastructure.Persistence;    // EnglishTestWebDbContext
using Microsoft.EntityFrameworkCore;
```

**Đăng ký DI (Program.cs) — tìm đoạn hiện có:**
```csharp
builder.Services.AddScoped<IResultsService, ResultsService>();
// Thêm dưới:
builder.Services.AddScoped<ITeacherSubmissionDetailService, TeacherSubmissionDetailService>();
```

**TeacherResultsController — thêm constructor injection:**
```csharp
public sealed class TeacherResultsController(
    IResultsService resultsService,
    ITeacherSubmissionDetailService submissionDetailService,   // THÊM
    ICurrentUserContext currentUserContext,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory) : ControllerBase
```

**`ClassSummary` model (Angular) — verify fields:**
```bash
grep -n "id\|name" src/EnglishTestWeb.Client/src/app/core/classes/classes.models.ts
```
Cần `id: string` và `name: string` để populate dropdown.

**`TestTemplateListItem` model (Angular) — verify fields:**
```bash
grep -n "id\|title" src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates.models.ts
```
Cần `id: string` và `title: string`.

### References

- [Story 6.1] `teacher-results.component.ts` — base component to extend
- [Story 5.3] `teacher-speaking-grading.component.ts` — speaking grading logic to reuse inline
- [Story 5.3] `TeacherSpeakingGradingService.cs` — scope check pattern
- [Story 4.4] `SubmissionService.cs` — auto-grade logic to understand `AnswerKeyVersionId` population
- `src/EnglishTestWeb.Api/Domain/TestTemplates/AnswerKeyVersion.cs` — `RowsJson` field
- `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionAnswer.cs` — `IsCorrect`, `Score` fields
- `src/EnglishTestWeb.Api/Contracts/Speaking/TeacherSpeakingSubmissionDto.cs` — speaking DTO shape
- `src/EnglishTestWeb.Client/src/app/core/classes/classes-api.service.ts` — `getTeacherClasses()`
- `src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates-api.service.ts` — `listTemplates()`

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- Tất cả 6 tasks hoàn thành. 317/317 API tests pass, 190/190 Angular tests pass.
- `TestTemplateListFilters` yêu cầu 3 required fields (`skill`, `status`, `q`) — phải pass `{ skill: '', status: '', q: '' }` thay vì `{}`.
- `ClassSummary` dùng `classId`/`className`, `TestTemplateListItem` dùng `templateId`/`title` — template HTML đã dùng đúng tên field.
- `DomSanitizer` inject trực tiếp qua `inject()` trong standalone component, không cần module import.
- `audioUrl` là computed signal phụ thuộc `selectedRowId()` — tự recalculate khi row thay đổi.
- Test spec cần mock 4 services: `ResultsApiService`, `SpeakingApiService`, `ClassesApiService`, `TestTemplatesApiService`.

### File List

**Backend (NEW):**
- `src/EnglishTestWeb.Api/Contracts/Results/TeacherAnswerRowDto.cs`
- `src/EnglishTestWeb.Api/Contracts/Results/TeacherSubmissionDetailDto.cs`
- `src/EnglishTestWeb.Api/Application/Results/ITeacherSubmissionDetailService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Results/TeacherSubmissionDetailService.cs`
- `tests/EnglishTestWeb.Api.Tests/Results/TeacherSubmissionDetailTests.cs`

**Backend (UPDATED):**
- `src/EnglishTestWeb.Api/Controllers/TeacherResultsController.cs`
- `src/EnglishTestWeb.Api/Program.cs`
- `tests/EnglishTestWeb.Api.Tests/Results/ResultsTestHelper.cs`

**Frontend (UPDATED):**
- `src/EnglishTestWeb.Client/src/app/core/results/results.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/results/results-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.html`
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.css`
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.spec.ts`

### Senior Developer Review (AI)

**Review Date:** 2026-06-13
**Outcome:** Changes Requested
**Layers:** Blind Hunter · Edge Case Hunter · Acceptance Auditor

#### Action Items

##### Patch (fix before done)

- [x] [Review][Patch][Med] TypeScript `skill` union too narrow — `TeacherSubmissionDetailDto.skill` typed as `'reading' | 'listening'` but backend returns any string (incl. 'speaking', '') [`results.models.ts`]
- [x] [Review][Patch][Med] `ToDictionary(r => r.QuestionNumber)` throws `ArgumentException` on duplicate question numbers in RowsJson — wrap in try/catch or use GroupBy+First [`TeacherSubmissionDetailService.cs`]
- [x] [Review][Patch][Med] Same-row retry silently blocked after detail load error — `onSelectRow` short-circuits on same id even when `detailState()` is `'error'`; add error-state exception [`teacher-results.component.ts:146`]
- [x] [Review][Patch][Med] AC6 — "Chấm tiếp →" unreachable when selected speaking row is `draft`; button is inside `@if (speaking.status !== 'draft')` guard — move it outside [`teacher-results.component.html:294`]
- [x] [Review][Patch][Low] AC3 — Status badge missing from speaking detail panel header [`teacher-results.component.html:229`]
- [x] [Review][Patch][Low] AC1 — Close button renders `✕` icon only; spec requires visible text `Đóng` [`teacher-results.component.html:169`]
- [x] [Review][Patch][Low] AC4 — Success message `"Đã lưu chấm điểm."` has trailing period; spec omits it [`teacher-results.component.html:283`]
- [x] [Review][Patch][Low] `JsonSerializerOptions` allocated fresh per call — make `private static readonly` [`TeacherSubmissionDetailService.cs`]
- [x] [Review][Patch][Low] 3 sequential DB round-trips for studentName/className/answerKeyVersion — parallelize with `Task.WhenAll` [`TeacherSubmissionDetailService.cs`]

##### Defer

- [x] [Review][Defer] `Guid.Empty` classId fallback unreachable — ownership check returns notFound first [`TeacherSubmissionDetailService.cs`] — deferred, pre-existing
- [x] [Review][Defer] `updateResultRow` no-ops silently if row not in current page after filter change — acceptable UX tradeoff [`teacher-results.component.ts`] — deferred, pre-existing
- [x] [Review][Defer] In-flight grade race with concurrent filter change — rowId null-guard fires; gradeState left at 'success' in closed panel is cosmetic [`teacher-results.component.ts`] — deferred, acceptable
- [x] [Review][Defer] `audioUrl` briefly returns null when `results()` replaced while speaking panel open — cosmetic flash [`teacher-results.component.ts`] — deferred, cosmetic
- [x] [Review][Defer] Filter dropdown load failures silently swallowed — dropdowns are non-critical auxiliary UX [`teacher-results.component.ts`] — deferred, by design
- [x] [Review][Defer] `template?.Skill ?? string.Empty` returns empty when template nav prop null — unreachable path given ownership check [`TeacherSubmissionDetailService.cs`] — deferred, pre-existing

#### Review Follow-ups (AI)

- [x] [AI-Review][Med] Fix TypeScript `skill` union in `TeacherSubmissionDetailDto` — change to `string` or add `'speaking'` to union
- [x] [AI-Review][Med] Wrap `ToDictionary` in try/catch for duplicate QuestionNumber safety in `TeacherSubmissionDetailService`
- [x] [AI-Review][Med] Add error-state exception to `onSelectRow` short-circuit — allow retry on same row when `detailState() === 'error'`
- [x] [AI-Review][Med] Move "Chấm tiếp →" button outside `@if (speaking.status !== 'draft')` guard
- [x] [AI-Review][Low] Add status badge to speaking detail panel
- [x] [AI-Review][Low] Change close button content from `✕` to `Đóng`
- [x] [AI-Review][Low] Remove trailing period from `"Đã lưu chấm điểm."` success message
- [x] [AI-Review][Low] Make `JsonSerializerOptions` static readonly in `TeacherSubmissionDetailService`
- [x] [AI-Review][Low] Parallelize studentName/className/answerKeyVersion DB queries with `Task.WhenAll`

### Senior Developer Review (AI) — Round 2

**Review Date:** 2026-06-13
**Outcome:** Changes Requested (1 critical patch)
**Layers:** Blind Hunter · Edge Case Hunter · Acceptance Auditor
**AC Status:** All 7 ACs pass

#### Action Items

##### Patch

- [x] [Review2][Patch][Critical] `Task.WhenAll(studentNameTask, classNameTask)` runs two concurrent async queries on the same scoped `DbContext` — EF Core DbContext is NOT thread-safe; throws `InvalidOperationException` at runtime. Revert to sequential awaits. [`TeacherSubmissionDetailService.cs`]

##### Defer

- [x] [Review2][Defer] Silent `catch (JsonException) { }` in AnswerKeyVersion deserialization — teachers see blank correct-answer column with no indicator; add structured log when logging infrastructure is in place [`TeacherSubmissionDetailService.cs`] — deferred, pre-existing
- [x] [Review2][Defer] `listTemplates({ skill: '', status: '', q: '' })` returns Archived templates in dropdown — product decision needed on whether to filter to status='ready' only [`teacher-results.component.ts`] — deferred, product decision
- [x] [Review2][Defer] `mode` defaults to `"live-exam"` when both HomeworkAssignmentId and LiveExamSessionId are null — unreachable due to ownership check returning notFound first [`TeacherSubmissionDetailService.cs`] — deferred, pre-existing
- [x] [Review2][Defer] Test gap: no test for non-null feedback forwarded through `onGradeSubmit` [`teacher-results.component.spec.ts`] — deferred, acceptable coverage
- [x] [Review2][Defer] Test gap: no test verifying `onSelectRow` retry is triggered when `detailState === 'error'` [`teacher-results.component.spec.ts`] — deferred, acceptable coverage

#### Review Follow-ups (AI) — Round 2

- [x] [AI-Review2][Critical] Fix `Task.WhenAll` concurrent DbContext queries — revert `studentNameTask`/`classNameTask` to sequential `await`
