---
baseline_commit: 718b7dc8b7971c1c525460881782cf24c824c0d5
---

# Story 4.4: Final Submission And Reading/Listening Auto-Grading

Status: done

## Story

Là học sinh,
tôi muốn nộp bài Reading/Listening và biết bài đã được khóa,
để giáo viên nhận được bài làm ổn định, được chấm điểm tự động.

## Acceptance Criteria

1. **Given** bài đang làm dở có câu chưa điền
   **When** học sinh bấm Nộp bài
   **Then** một confirmation modal hiển thị số câu còn thiếu
   **And** học sinh có thể quay lại tiếp tục làm.

2. **Given** học sinh xác nhận nộp bài
   **When** API chấp nhận lệnh submit
   **Then** Submission status chuyển thành `submitted` hoặc `auto-graded`
   **And** các câu trả lời trở thành read-only cho học sinh.

3. **Given** AnswerKey có phiên bản hiện tại cho template
   **When** final submit hoàn thành
   **Then** SubmissionAnswer rows được chấm điểm theo phiên bản đó
   **And** auto_score và AnswerKeyVersionId được lưu trên Submission.

4. **Given** học sinh double-click submit hoặc request bị retry
   **When** API nhận duplicate submit commands
   **Then** chỉ có một kết quả Submission final tồn tại
   **And** duplicate request trả về kết quả gốc (200 OK idempotent).

5. **Given** Homework deadline đã qua hoặc LiveExamSession đã đóng trước khi submit
   **When** học sinh nộp bài
   **Then** API từ chối với `submission.sourceUnavailable`
   **And** UI hiển thị thông báo rõ ràng có thể recover.

6. **Given** final submit thành công
   **When** success state hiển thị
   **Then** hiển thị submitted timestamp, tiêu đề bài thi, mode, điểm (nếu có), và nút quay lại Assigned Tests.

## Tasks / Subtasks

- [x] Task 1: Backend — Mở rộng domain entities (AC2, AC3)
  - [x] 1.1 Thêm `AutoGraded` constant vào `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionStatuses.cs`:
    ```csharp
    public const string AutoGraded = "auto-graded";
    ```
    File hiện tại chỉ có `Draft = "draft"` và `Submitted = "submitted"`.
  - [x] 1.2 Thêm fields vào `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs` (sau `UpdatedAt`):
    ```csharp
    public DateTimeOffset? SubmittedAt { get; set; }
    public decimal? AutoScore { get; set; }
    ```
  - [x] 1.3 Thêm fields vào `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionAnswer.cs` (sau `UpdatedAt`):
    ```csharp
    public bool? IsCorrect { get; set; }
    public decimal? Score { get; set; }
    ```

- [x] Task 2: Backend — EF Core config + migration (AC2, AC3)
  - [x] 2.1 Cập nhật `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs` — thêm precision cho `AutoScore` trong `Configure()`:
    ```csharp
    entity.Property(s => s.AutoScore).HasColumnType("decimal(18,2)");
    ```
    Thêm sau dòng `entity.Property(s => s.Status).HasMaxLength(50).IsRequired();`. `SubmittedAt` là `DateTimeOffset?` — EF tự map sang `datetimeoffset(7) NULL`, không cần config thêm.
  - [x] 2.2 Cập nhật `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionAnswerConfiguration.cs` — thêm precision cho `Score`:
    ```csharp
    entity.Property(a => a.Score).HasColumnType("decimal(18,2)");
    ```
    Thêm sau dòng `entity.Property(a => a.Answer).HasMaxLength(500);`. `IsCorrect` là `bool?` — EF tự map sang `bit NULL`.
  - [x] 2.3 Tạo migration:
    ```powershell
    dotnet ef migrations add AddFinalSubmitFields --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj
    ```
  - [x] 2.4 Inspect migration file — xác nhận có:
    - `SubmittedAt datetimeoffset nullable` trên table `Submissions`
    - `AutoScore decimal(18,2) nullable` trên table `Submissions`
    - `IsCorrect bit nullable` trên table `SubmissionAnswers`
    - `Score decimal(18,2) nullable` trên table `SubmissionAnswers`
  - [x] 2.5 `dotnet test` — xác nhận 237 tests hiện có vẫn pass

- [x] Task 3: Backend — Contracts/DTOs cho final submit (AC2, AC4, AC5, AC6)
  - [x] 3.1 Tạo `src/EnglishTestWeb.Api/Contracts/Submissions/FinalSubmitResult.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Submissions;

    public sealed record FinalSubmitResult(
        bool Success,
        string? ErrorCode,
        SubmissionResultDto? Result);
    ```
  - [x] 3.2 Tạo `src/EnglishTestWeb.Api/Contracts/Submissions/SubmissionResultDto.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Submissions;

    public sealed record SubmissionResultDto(
        Guid SubmissionId,
        string Status,
        string Mode,
        string TemplateTitle,
        DateTimeOffset SubmittedAt,
        decimal? AutoScore,
        int QuestionCount,
        int CorrectCount);
    ```

- [x] Task 4: Backend — ISubmissionService + FinalSubmitAsync (AC2, AC3, AC4, AC5)
  - [x] 4.1 Thêm method vào `src/EnglishTestWeb.Api/Application/Submissions/ISubmissionService.cs`:
    ```csharp
    Task<FinalSubmitResult> FinalSubmitAsync(
        Guid submissionId,
        string studentId,
        CancellationToken cancellationToken = default);
    ```
  - [x] 4.2 Implement `FinalSubmitAsync` trong `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs`.
    Thêm các `using` cần thiết ở đầu file nếu chưa có:
    ```csharp
    using System.Text.Json;
    using EnglishTestWeb.Api.Domain.TestTemplates;
    ```
    Implement method:
    ```csharp
    public async Task<FinalSubmitResult> FinalSubmitAsync(
        Guid submissionId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        // Load submission — verify ownership, include navigation props needed for deadline check
        var submission = await db.Submissions
            .Include(s => s.Answers)
            .Include(s => s.HomeworkAssignment)
            .Include(s => s.LiveExamSession)
            .Where(s => s.Id == submissionId && s.StudentId == studentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return new FinalSubmitResult(false, "submission.notFound", null);

        // AC4: Idempotency — already submitted → return existing result
        if (submission.Status != SubmissionStatuses.Draft)
        {
            var existingResult = await BuildResultDtoAsync(submission, cancellationToken);
            return new FinalSubmitResult(true, null, existingResult);
        }

        // AC5: Re-verify source is still open at submit time
        var now = timeProvider.GetUtcNow();
        if (submission.HomeworkAssignmentId.HasValue)
        {
            if (submission.HomeworkAssignment!.DeadlineAt < now)
                return new FinalSubmitResult(false, "submission.sourceUnavailable", null);
        }
        else
        {
            if (submission.LiveExamSession!.Status != LiveExamSessionStatuses.Open)
                return new FinalSubmitResult(false, "submission.sourceUnavailable", null);
        }

        // AC3: Auto-grade if AnswerKey version was snapped at submission creation
        var correctCount = 0;
        decimal? autoScore = null;

        if (submission.AnswerKeyVersionId.HasValue)
        {
            var akv = await db.AnswerKeyVersions
                .AsNoTracking()
                .Where(a => a.Id == submission.AnswerKeyVersionId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (akv is not null)
            {
                var rows = JsonSerializer.Deserialize<List<AnswerKeyRow>>(
                    akv.RowsJson,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (rows is not null && rows.Count > 0)
                {
                    var keyMap = rows.ToDictionary(r => r.QuestionNumber);
                    var scorePerQuestion = akv.ScoringMode == ScoringModes.Equal && akv.QuestionCount > 0
                        ? (akv.TotalScore ?? 0m) / akv.QuestionCount
                        : 0m;

                    decimal totalEarned = 0m;
                    foreach (var answer in submission.Answers)
                    {
                        if (keyMap.TryGetValue(answer.QuestionNumber, out var keyRow))
                        {
                            var isCorrect = string.Equals(
                                answer.Answer?.Trim(),
                                keyRow.CorrectAnswer.Trim(),
                                StringComparison.OrdinalIgnoreCase);

                            var earned = isCorrect
                                ? (akv.ScoringMode == ScoringModes.PerQuestion
                                    ? keyRow.Score ?? 0m
                                    : scorePerQuestion)
                                : 0m;

                            answer.IsCorrect = isCorrect;
                            answer.Score = earned;
                            if (isCorrect) correctCount++;
                            totalEarned += earned;
                        }
                        else
                        {
                            answer.IsCorrect = false;
                            answer.Score = 0m;
                        }
                    }

                    autoScore = totalEarned;
                }
            }
        }

        // Finalize — status depends on whether grading ran
        submission.Status = submission.AnswerKeyVersionId.HasValue
            ? SubmissionStatuses.AutoGraded
            : SubmissionStatuses.Submitted;
        submission.SubmittedAt = now;
        submission.AutoScore = autoScore;
        submission.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        var result = await BuildResultDtoAsync(submission, cancellationToken);
        return new FinalSubmitResult(true, null, result);
    }

    private async Task<SubmissionResultDto> BuildResultDtoAsync(
        Submission submission,
        CancellationToken cancellationToken)
    {
        var templateId = submission.HomeworkAssignment?.TestTemplateId
            ?? submission.LiveExamSession?.TestTemplateId;

        var templateTitle = string.Empty;
        if (templateId.HasValue)
        {
            templateTitle = await db.TestTemplates
                .AsNoTracking()
                .Where(t => t.Id == templateId.Value)
                .Select(t => t.Title)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        }

        var mode = submission.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";
        var answeredCorrectly = submission.Answers.Count(a => a.IsCorrect == true);
        var questionCount = submission.Answers.Count;

        return new SubmissionResultDto(
            submission.Id,
            submission.Status,
            mode,
            templateTitle,
            submission.SubmittedAt ?? submission.UpdatedAt,
            submission.AutoScore,
            questionCount,
            answeredCorrectly);
    }
    ```
    **Quan trọng**: `BuildResultDtoAsync` cần `submission.HomeworkAssignment` và `submission.LiveExamSession` đã được load. Cả hai path (idempotent + normal) đều dùng cùng Include query ở đầu → navigation props luôn sẵn có.
  - [x] 4.3 `dotnet test` — xác nhận tests hiện có vẫn pass

- [x] Task 5: Backend — Controller endpoint (AC2, AC4, AC5)
  - [x] 5.1 Thêm endpoint vào `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs` (sau `AutosaveAnswers`):
    ```csharp
    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<SubmissionResultDto>> FinalSubmit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var studentId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status401Unauthorized,
                "auth.unauthorized",
                "Unauthorized.",
                "Authentication is required.");
        }

        var result = await submissionService.FinalSubmitAsync(id, studentId, cancellationToken);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "submission.sourceUnavailable" => hiddenResourceResponseFactory.FromCode(
                    StatusCodes.Status422UnprocessableEntity,
                    "submission.sourceUnavailable",
                    "Cannot submit.",
                    "The submission source is no longer available (deadline passed or session closed)."),
                _ => hiddenResourceResponseFactory.FromCode(
                    StatusCodes.Status404NotFound,
                    result.ErrorCode ?? "submission.notFound",
                    "Submission not found.",
                    "The requested submission could not be found."),
            };
        }

        return Ok(result.Result);
    }
    ```
    **Không** trả về 201 Created — đây là state transition command, không phải tạo resource mới. Trả về 200 OK với `SubmissionResultDto`.
  - [x] 5.2 `dotnet test` — xác nhận tests hiện có vẫn pass

- [x] Task 6: Backend — API tests (AC1-AC6)
  - [x] 6.1 Cập nhật `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsTestHelper.cs` — thêm helper `CreateSubmissionAsync` để tái sử dụng trong tests:
    ```csharp
    internal static async Task<Guid> CreateSubmissionAsync(
        HttpClient client,
        Guid? homeworkAssignmentId,
        Guid? liveExamSessionId)
    {
        var response = await Auth.AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId,
            liveExamSessionId
        });
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await System.Text.Json.JsonDocument.ParseAsync(body);
        return doc.RootElement.GetProperty("id").GetGuid();
    }
    ```
  - [x] 6.2 Tạo `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsFinalSubmitTests.cs`:
    ```
    FinalSubmit_AsAnonymous_Returns401
    FinalSubmit_AsTeacher_Returns403
    FinalSubmit_AsOwnerStudent_Returns200WithResult
      → verify response có: submissionId, status (auto-graded), mode, templateTitle, submittedAt, autoScore, questionCount, correctCount
    FinalSubmit_AsOtherStudent_Returns404
      → submit Guid.NewGuid() submission id, verify 404 + "submission.notFound"
    FinalSubmit_Idempotent_Returns200SameResult
      → submit 2 lần → cả hai 200, submittedAt giống nhau
    FinalSubmit_AutoGrades_CorrectAnswer
      → autosave answer "A" (CorrectAnswer trong seed = "A") → submit → correctCount == 1, autoScore == 10
    FinalSubmit_AutoGrades_WrongAnswer
      → autosave answer "B" (sai) → submit → correctCount == 0, autoScore == 0
    FinalSubmit_NoAnswer_AutoGrades_ZeroScore
      → submit mà không autosave → correctCount == 0, autoScore == 0
    FinalSubmit_ExpiredHomework_Returns422
      → SeedHomeworkWithReadyTemplateAsync(factory, deadlineAt: DateTimeOffset.UtcNow.AddDays(-1))
      → create submission, submit → 422 "submission.sourceUnavailable"
    FinalSubmit_ClosedLiveExam_Returns422
      → SeedLiveExamWithReadyTemplateAsync(factory, status: LiveExamSessionStatuses.Closed)
      → KHÔNG thể create submission (vì session closed), nên cần seed manual:
        seed một Open session → create submission → đóng session bằng DB manipulation → submit → 422
      Hoặc: seed Open session → create submission → update session status to Closed via EF directly → submit → 422
    FinalSubmit_AfterSubmit_AutosaveReturns409
      → submit thành công → PUT /answers → 409 "submission.notDraft"
    GetWorkspace_AfterFinalSubmit_ReturnsNonDraftStatus
      → submit → GET workspace → status != "draft"
    ```
    **Lưu ý cho `FinalSubmit_ClosedLiveExam_Returns422`**: Cần tạo submission khi session còn Open, sau đó đổi status thành Closed trực tiếp qua DB context. Pattern:
    ```csharp
    // Sau khi create submission thành công:
    using (var scope = factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var session = await db.LiveExamSessions.FindAsync(sessionId);
        session!.Status = LiveExamSessionStatuses.Closed;
        session.ClosedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
    // Sau đó submit → expect 422
    ```
  - [x] 6.3 Thêm vào `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`:
    ```csharp
    // POST /api/submissions/{id}/submit
    [Fact]
    public async Task Unauthenticated_PostSubmissionSubmit_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/submit", new {});
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task Teacher_PostSubmissionSubmit_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/submit", new {});
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }
    ```
  - [x] 6.4 `dotnet test` — xác nhận tất cả tests pass

- [x] Task 7: Angular — Update models và API service (AC2, AC5, AC6)
  - [x] 7.1 Thêm interface vào `src/EnglishTestWeb.Client/src/app/core/submissions/submissions.models.ts`:
    ```typescript
    export interface SubmissionResultDto {
      submissionId: string;
      status: string;
      mode: 'homework' | 'live-exam';
      templateTitle: string;
      submittedAt: string;
      autoScore: number | null;
      questionCount: number;
      correctCount: number;
    }
    ```
    Giữ nguyên tất cả interfaces và constants hiện có. Không cần cập nhật `SUBMISSION_ERROR_MESSAGES` — `'submission.sourceUnavailable'` đã có với message `'Bài thi này hiện không còn khả dụng.'` đủ rõ.
  - [x] 7.2 Thêm import và method vào `src/EnglishTestWeb.Client/src/app/core/submissions/submissions-api.service.ts`:
    ```typescript
    import { ..., SubmissionResultDto } from './submissions.models';  // thêm SubmissionResultDto vào import

    finalSubmit(submissionId: string): Promise<SubmissionResultDto> {
      return firstValueFrom(
        this.http.post<SubmissionResultDto>(`/api/submissions/${submissionId}/submit`, {}),
      );
    }
    ```

- [x] Task 8: Angular — Submit logic trong workspace component (AC1, AC2, AC4, AC5, AC6)
  - [x] 8.1 Thêm imports và signals vào `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.ts`:
    ```typescript
    import { SubmissionResultDto, ... } from '../../core/submissions/submissions.models';
    // Thêm SubmissionResultDto vào import đã có

    protected readonly isSubmitConfirmOpen = signal<boolean>(false);
    protected readonly submitState = signal<'idle' | 'submitting' | 'error'>('idle');
    protected readonly submitError = signal<string | null>(null);
    protected readonly submitResult = signal<SubmissionResultDto | null>(null);
    ```
  - [x] 8.2 Thêm `missingCount` computed (dùng `answeredCount()` đã có):
    ```typescript
    protected readonly missingCount = computed(() => {
      const ws = this.workspace();
      return ws ? ws.questionCount - this.answeredCount() : 0;
    });
    ```
  - [x] 8.3 Thêm helper `formatDate` (tránh phụ thuộc DatePipe):
    ```typescript
    protected formatDate(iso: string): string {
      return new Intl.DateTimeFormat('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }).format(new Date(iso));
    }
    ```
  - [x] 8.4 Cập nhật `onSubmit()` (thay thế placeholder hoàn toàn):
    ```typescript
    protected onSubmit(): void {
      const ws = this.workspace();
      if (!ws || ws.status !== 'draft') return;
      this.isSubmitConfirmOpen.set(true);
    }
    ```
  - [x] 8.5 Thêm `onCancelSubmit()` và `onConfirmSubmit()`:
    ```typescript
    protected onCancelSubmit(): void {
      this.isSubmitConfirmOpen.set(false);
    }

    protected async onConfirmSubmit(): Promise<void> {
      const id = this.submissionId;
      if (!id) return;

      this.isSubmitConfirmOpen.set(false);
      this.submitState.set('submitting');
      this.submitError.set(null);

      try {
        const result = await this.submissionsApi.finalSubmit(id);
        this.submitResult.set(result);
        this.submitState.set('idle');
      } catch (err: unknown) {
        const code = this.extractErrorCode(err);
        this.submitError.set(
          SUBMISSION_ERROR_MESSAGES[code ?? ''] ?? 'Nộp bài thất bại. Vui lòng thử lại.',
        );
        this.submitState.set('error');
      }
    }
    ```
  - [x] 8.6 Cập nhật `performAutosave()` — mở rộng guard từ `'submitted'` thành `!== 'draft'`:
    ```typescript
    private async performAutosave(): Promise<void> {
      const id = this.submissionId;
      const ws = this.workspace();
      if (!id || !ws || ws.status !== 'draft') return;  // Thay === 'submitted' thành !== 'draft'
      // ... phần còn lại không thay đổi
    }
    ```

- [x] Task 9: Angular — Template updates (AC1, AC2, AC5, AC6)
  - [x] 9.1 Cập nhật submit button trong `student-attempt-workspace.component.html` — bỏ `disabled` hardcoded, bỏ text "(Chưa triển khai)":
    ```html
    @if (!submitResult()) {
      <button
        type="button"
        class="submit-button"
        [disabled]="submitState() === 'submitting' || workspace()!.status !== 'draft'"
        (click)="onSubmit()"
        data-testid="submit-button"
      >
        @if (submitState() === 'submitting') {
          Đang nộp bài...
        } @else {
          Nộp bài
        }
      </button>
    }
    ```
  - [x] 9.2 Thêm submit error message sau submit button:
    ```html
    @if (submitState() === 'error' && submitError()) {
      <p class="submit-error" data-testid="submit-error">{{ submitError() }}</p>
    }
    ```
  - [x] 9.3 Thêm lock cho answer inputs — thêm `[disabled]` binding vào `<input>` trong answer form:
    ```html
    <input
      class="answer-input"
      type="text"
      [id]="'answer-' + qn"
      [attr.data-testid]="'answer-input-' + qn"
      [attr.aria-label]="'Câu ' + qn"
      [value]="answerInputs()[qn] || ''"
      (input)="onAnswerChange(qn, $any($event.target).value)"
      autocomplete="off"
      maxlength="500"
      [disabled]="workspace()!.status !== 'draft' || !!submitResult()"
    />
    ```
  - [x] 9.4 Thêm confirmation modal overlay (thêm ngay trước `</div>` đóng của `workspace-page`, trong `@else` loaded block):
    ```html
    @if (isSubmitConfirmOpen()) {
      <div class="submit-confirm-overlay" data-testid="submit-confirm-modal" role="dialog" aria-modal="true" aria-labelledby="confirm-title">
        <div class="submit-confirm-dialog">
          <h2 id="confirm-title" class="confirm-title">Xác nhận nộp bài</h2>
          @if (missingCount() > 0) {
            <p class="confirm-missing" data-testid="confirm-missing-count">
              Còn {{ missingCount() }} câu chưa điền. Bạn có chắc muốn nộp bài?
            </p>
          } @else {
            <p class="confirm-complete" data-testid="confirm-all-answered">
              Bạn đã điền đủ {{ workspace()!.questionCount }} câu. Xác nhận nộp bài?
            </p>
          }
          <div class="confirm-actions">
            <button
              type="button"
              class="primary-button"
              (click)="onConfirmSubmit()"
              data-testid="confirm-submit-btn"
            >
              Xác nhận nộp bài
            </button>
            <button
              type="button"
              class="text-button"
              (click)="onCancelSubmit()"
              data-testid="cancel-submit-btn"
            >
              Quay lại làm bài
            </button>
          </div>
        </div>
      </div>
    }
    ```
  - [x] 9.5 Thêm success state panel (thêm sau `.workspace-body`, trong loaded block, sau confirm modal):
    ```html
    @if (submitResult()) {
      <div class="submit-success" data-testid="submit-success">
        <h2 class="success-title">Đã nộp bài thành công!</h2>
        <dl class="result-details">
          <dt>Bài thi</dt>
          <dd data-testid="result-template-title">{{ submitResult()!.templateTitle }}</dd>
          <dt>Hình thức</dt>
          <dd data-testid="result-mode">{{ modeLabels[submitResult()!.mode] }}</dd>
          <dt>Thời gian nộp</dt>
          <dd data-testid="result-submitted-at">{{ formatDate(submitResult()!.submittedAt) }}</dd>
          @if (submitResult()!.autoScore !== null) {
            <dt>Điểm</dt>
            <dd data-testid="result-score">
              {{ submitResult()!.autoScore }} điểm ({{ submitResult()!.correctCount }}/{{ submitResult()!.questionCount }} câu đúng)
            </dd>
          }
        </dl>
        <button
          type="button"
          class="primary-button"
          (click)="backToTests()"
          data-testid="back-to-tests-after-submit-btn"
        >
          Quay lại danh sách bài thi
        </button>
      </div>
    }
    ```

- [x] Task 10: Angular — Unit tests (AC1-AC6)
  - [x] 10.1 Cập nhật `student-attempt-workspace.component.spec.ts`:
    Thêm `finalSubmit: vi.fn().mockResolvedValue(makeSubmitResult())` vào mock `submissionsApi` trong hàm `setup()`.

    Thêm helper function:
    ```typescript
    import { SubmissionResultDto } from '../../core/submissions/submissions.models';

    function makeSubmitResult(overrides: Partial<SubmissionResultDto> = {}): SubmissionResultDto {
      return {
        submissionId: 'sub-1',
        status: 'auto-graded',
        mode: 'homework',
        templateTitle: 'Unit 1 Reading Test',
        submittedAt: '2026-06-12T10:00:00Z',
        autoScore: 10,
        questionCount: 1,
        correctCount: 1,
        ...overrides,
      };
    }
    ```

    Thêm các test cases:
    - `submit button enabled khi workspace status = draft`
    - `onSubmit() mở confirmation modal khi status = draft`
    - `confirmation modal hiển thị missing count khi có câu chưa điền`
    - `confirmation modal hiển thị "đủ câu" khi tất cả đã điền`
    - `onCancelSubmit() đóng modal (isSubmitConfirmOpen = false)`
    - `onConfirmSubmit() gọi finalSubmit và hiển thị success state`
    - `success state hiển thị templateTitle, mode, submittedAt`
    - `onConfirmSubmit() thất bại → submitState = error, submit-error hiển thị`
    - `inputs bị disabled khi workspace.status = submitted (load lại sau submit)`
    - `submit button disabled khi workspace.status = submitted`
    - `performAutosave không gọi khi workspace.status = auto-graded`
    - `onSubmit() không mở modal khi workspace.status != draft`
  - [x] 10.2 `npm test` — xác nhận tất cả tests pass

## Dev Notes

### Backend: Grading Logic Chi Tiết (AC3)

**AnswerKeyRow deserialization**: `AnswerKeyVersion.RowsJson` được serialize với `JsonSerializerDefaults.Web` (camelCase). Phải deserialize với cùng options để `AnswerKeyRow` record constructor params được map đúng:
```csharp
var rows = JsonSerializer.Deserialize<List<AnswerKeyRow>>(
    akv.RowsJson,
    new JsonSerializerOptions(JsonSerializerDefaults.Web));
```

`AnswerKeyRow` record tại `Domain/TestTemplates/AnswerKeyRow.cs`:
```csharp
public sealed record AnswerKeyRow(int QuestionNumber, string CorrectAnswer, decimal? Score);
```
JSON format trong DB: `[{"questionNumber":1,"correctAnswer":"A","score":null}]`

**Case-insensitive comparison**: `StringComparison.OrdinalIgnoreCase` sau `Trim()` cả hai phía. Đủ cho MCQ (A/B/C/D). Không cần Unicode normalization cho scope này.

**Equal scoring mode** (`ScoringModes.Equal = "equal"`):
- `scorePerQuestion = (akv.TotalScore ?? 0m) / akv.QuestionCount`
- Nếu `QuestionCount == 0` (không xảy ra nhưng defensive) → `scorePerQuestion = 0m`
- Mỗi câu đúng nhận `scorePerQuestion` điểm

**PerQuestion scoring mode** (`ScoringModes.PerQuestion = "per-question"`):
- Mỗi câu đúng nhận `keyRow.Score ?? 0m` điểm
- Câu trong AnswerKey nhưng `Score == null` → 0 điểm nếu đúng

**Seed data trong tests**: `SeedHomeworkWithReadyTemplateAsync` tạo template với:
- `QuestionCount = 1`, `TotalScore = 10m`, `ScoringMode = Equal`
- `CorrectAnswer = "A"` cho QuestionNumber 1
- Tức là: autosave "A" → submit → `correctCount = 1`, `autoScore = 10`

**Status sau grading**:
- `AnswerKeyVersionId != null` → `SubmissionStatuses.AutoGraded` ("auto-graded")
- `AnswerKeyVersionId == null` → `SubmissionStatuses.Submitted` ("submitted")

### Backend: Idempotency Strategy (AC4)

**Không** implement X-Idempotency-Key infrastructure cho story này (scope quá lớn, chưa có infrastructure). Thay vào đó: kiểm tra `submission.Status != Draft` → trả về kết quả hiện tại (200 OK). Đây là "status-based idempotency" đủ cho MVP single-session assumption.

**Test idempotency**: Gọi submit 2 lần. Xác nhận `submittedAt` là cùng một timestamp (không override). Navigation props `HomeworkAssignment`/`LiveExamSession` được load cùng Include query → `BuildResultDtoAsync` luôn có dữ liệu.

### Backend: AC5 — Check Deadline/Session Tại Submit Time

Check lại state tại thời điểm submit, không tin vào state lúc workspace load. Học sinh có thể làm bài rồi deadline hết hạn — phải trả về error để UI inform rõ ràng.

**Homework deadline**: `submission.HomeworkAssignment!.DeadlineAt < now` → 422 `submission.sourceUnavailable`

**LiveExamSession**: `submission.LiveExamSession!.Status != LiveExamSessionStatuses.Open` → 422 `submission.sourceUnavailable`

Navigation props (`HomeworkAssignment`, `LiveExamSession`) được `Include()`d trong query đầu — không cần query riêng.

### Backend: SubmissionService — Usings Cần Thêm

`SubmissionService.cs` hiện đã có nhiều usings. Kiểm tra nếu thiếu, thêm:
```csharp
using System.Text.Json;
using EnglishTestWeb.Api.Domain.TestTemplates;  // cho AnswerKeyRow, ScoringModes
```

`LiveExamSessionStatuses` đã được import (`Domain.LiveExams`). `SubmissionStatuses` đã có.

### Angular: Confirmation Modal (AC1)

Sử dụng in-template CSS overlay (`@if (isSubmitConfirmOpen())`) thay vì HTML `<dialog>` element hoặc external library. Phù hợp với pattern của project — không có modal component nào tồn tại. CSS overlay đủ cho MVP.

**Không dùng** `window.confirm()` — không accessible, không styleable.

**`missingCount` computed** tái sử dụng `answeredCount()` đã có:
```typescript
protected readonly missingCount = computed(() => {
  const ws = this.workspace();
  return ws ? ws.questionCount - this.answeredCount() : 0;
});
```

### Angular: Success State (AC6)

`submitResult` signal chứa kết quả sau submit thành công. Template dùng `@if (submitResult())` để hiển thị success panel. Form bài làm và submit button bị ẩn (`@if (!submitResult())`).

**`formatDate` helper** thay vì DatePipe — tránh cần thêm import:
```typescript
protected formatDate(iso: string): string {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  }).format(new Date(iso));
}
```
`Intl.DateTimeFormat` available natively trong modern browsers, không cần import.

### Angular: Input Lock (AC2)

Inputs bị `disabled` trong 2 trường hợp:
1. `workspace().status !== 'draft'` — workspace load với status đã submitted (reload sau submit)
2. `!!submitResult()` — submit thành công trong session hiện tại (trước khi reload)

Pattern: `[disabled]="workspace()!.status !== 'draft' || !!submitResult()"`

**`performAutosave()` guard** phải được cập nhật từ `ws.status === 'submitted'` → `ws.status !== 'draft'` để bao gồm cả `'auto-graded'`. Đây là BUG tiềm ẩn nếu bỏ qua.

### Angular: Submit Button Changes

Hiện tại button có `disabled` hardcoded và text "Nộp bài (Chưa triển khai)". Thay thế hoàn toàn:
- Bỏ `disabled` attribute tĩnh
- Bỏ text "(Chưa triển khai)"
- Bọc trong `@if (!submitResult())` để ẩn sau submit thành công
- Binding `[disabled]="submitState() === 'submitting' || workspace()!.status !== 'draft'"`

### Angular: Component Decorator

`StudentAttemptWorkspaceComponent` không có `standalone: true` explicit trong decorator, nhưng được lazy-loaded qua `loadComponent()` → là standalone component trong Angular 22. Không cần thêm module imports. Tất cả Angular directives (`@if`, `@for`, `@switch`) dùng built-in control flow syntax — không cần import `NgIf`/`NgFor`.

### Context Từ Previous Stories

1. **`flushPromises()` pattern** — dùng trong tất cả Angular async tests (đã có)
2. **`vi.fn().mockResolvedValue()`** — mock pattern cho async methods
3. **`data-testid` attributes** — theo pattern stories 4.1, 4.2, 4.3
4. **`hiddenResourceResponseFactory.FromCode(statusCode, code, title, detail)`** — pattern nhất quán trong `SubmissionsController`
5. **`TimeProvider`** — đã inject qua constructor `SubmissionService`, dùng `timeProvider.GetUtcNow()`
6. **`SeedHomeworkWithReadyTemplateAsync` returns** `(homeworkId, classId, pdfFileId)` — helper đã tồn tại
7. **`SeedLiveExamWithReadyTemplateAsync(factory, status)`** — truyền `LiveExamSessionStatuses.Closed` để seed closed session
8. **`AuthTestHelper.SignInStudentWithClassAsync(client, classId)`** — cần classId
9. **`AuthTestHelper.PostJsonAsync / PutJsonAsync`** — helper methods cho test HTTP calls
10. **`SubmissionsTestHelper.CreateSubmissionAsync`** — helper mới được thêm Task 6.1, tái sử dụng cho tất cả submit tests
11. **`TestTemplatesTestHelper.SeedDemoTemplatesAsync`** — cần gọi trước `SignInTeacherAsync` trong AuthorizationMatrix tests

### Anti-Patterns

- **KHÔNG** implement X-Idempotency-Key infrastructure — deferred, status-based idempotency đủ cho MVP
- **KHÔNG** dùng live AnswerKey tại submit time — phải dùng `submission.AnswerKeyVersionId` đã snap từ lúc CreateOrResume
- **KHÔNG** optimistic update status trước khi API confirm
- **KHÔNG** dùng `window.confirm()` cho confirmation dialog
- **KHÔNG** expose `correctAnswer` hay nội dung AnswerKey trong response cho học sinh
- **KHÔNG** thêm `autoScore` hay `submittedAt` vào `SubmissionWorkspaceDto` — chỉ trả về trong `SubmissionResultDto`
- **KHÔNG** thêm `dotnet ef database update` vào CI — migration chỉ apply khi startup (in-memory DB tự-migrate trong tests)
- **KHÔNG** bỏ qua `UpdatedAt` khi set trạng thái submitted — phải update cùng lúc với `SubmittedAt`

### Files Being Created/Modified

**API (new):**
- `src/EnglishTestWeb.Api/Contracts/Submissions/FinalSubmitResult.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/SubmissionResultDto.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/` — migration `AddFinalSubmitFields`
- `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsFinalSubmitTests.cs`

**API (update):**
- `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs` — thêm `SubmittedAt`, `AutoScore`
- `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionAnswer.cs` — thêm `IsCorrect`, `Score`
- `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionStatuses.cs` — thêm `AutoGraded = "auto-graded"`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs` — thêm decimal(18,2) cho AutoScore
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionAnswerConfiguration.cs` — thêm decimal(18,2) cho Score
- `src/EnglishTestWeb.Api/Application/Submissions/ISubmissionService.cs` — thêm `FinalSubmitAsync`
- `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs` — implement `FinalSubmitAsync` + `BuildResultDtoAsync`
- `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs` — thêm `POST /{id}/submit`
- `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsTestHelper.cs` — thêm `CreateSubmissionAsync`
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — thêm POST submit tests

**Angular (update):**
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions.models.ts` — thêm `SubmissionResultDto`
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions-api.service.ts` — thêm `finalSubmit`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.ts` — submit logic + signals
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.html` — modal, success state, lock inputs, updated submit button
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.spec.ts` — tests mới

### Architecture Compliance

- **Controller không access DbContext** — delegate hoàn toàn sang `ISubmissionService`
- **TimeProvider** — dùng `timeProvider.GetUtcNow()` trong `SubmissionService`, không `DateTimeOffset.UtcNow`
- **Student scope** — `FinalSubmitAsync` verify `StudentId == studentId` trước khi process
- **Server authoritative for deadline/session state** — check lại source state tại submit time
- **No optimistic update** — Angular chờ server confirm trước khi set `submitResult`
- **AnswerKeyVersionId snapshot** — grading dùng version đã snap tại CreateOrResume, không query live version
- **Score precision** — `decimal(18,2)` theo SQL Server convention trong project
- **Status transitions server-side** — Angular không tự set workspace status

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 4, Story 4.4]
- [Source: `_bmad-output/implementation-artifacts/4-3-draft-answer-autosave-and-restore.md` — Patterns từ previous story]
- [Source: `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs` — Entity cần extend]
- [Source: `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionAnswer.cs` — Entity cần extend]
- [Source: `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionStatuses.cs` — Constants cần extend]
- [Source: `src/EnglishTestWeb.Api/Domain/TestTemplates/AnswerKeyVersion.cs` — Grading source entity]
- [Source: `src/EnglishTestWeb.Api/Domain/TestTemplates/AnswerKeyRow.cs` — Row record cho deserialization]
- [Source: `src/EnglishTestWeb.Api/Domain/TestTemplates/ScoringModes.cs` — Equal/PerQuestion constants]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs` — EF config cần update]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionAnswerConfiguration.cs` — EF config cần update]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs` — Service cần extend]
- [Source: `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs` — Controller pattern]
- [Source: `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsTestHelper.cs` — Seed helpers]
- [Source: `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsAutosaveTests.cs` — Test pattern]
- [Source: `src/EnglishTestWeb.Client/.../student-attempt-workspace.component.ts` — Component cần extend]
- [Source: `src/EnglishTestWeb.Client/.../student-attempt-workspace.component.html` — Template cần extend]
- [Source: `src/EnglishTestWeb.Client/.../student-attempt-workspace.component.spec.ts` — Spec cần extend]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

Không có issue đáng kể. MSBuild cache file intermittent errors được resolve bằng cách delete cache và rebuild.

### Completion Notes List

- Task 1: Thêm `AutoGraded = "auto-graded"` constant, `SubmittedAt`/`AutoScore` vào Submission, `IsCorrect`/`Score` vào SubmissionAnswer
- Task 2: EF config decimal(18,2) cho AutoScore và Score, migration `AddFinalSubmitFields` tạo thành công với đủ 4 fields, 237 existing tests pass
- Task 3: Tạo `FinalSubmitResult` và `SubmissionResultDto` sealed records
- Task 4: Thêm `FinalSubmitAsync` vào interface và implement trong SubmissionService với auto-grading logic (JsonSerializerDefaults.Web), idempotency (status-based), deadline/session check, `BuildResultDtoAsync` helper
- Task 5: Thêm `POST {id}/submit` endpoint trả về 200 OK với SubmissionResultDto
- Task 6: 14 tests mới (11 functional + 2 auth matrix + 1 CreateSubmissionAsync helper) — 251 total pass
- Task 7: Thêm `SubmissionResultDto` interface và `finalSubmit()` method vào Angular service
- Task 8: Thêm 4 signals (`isSubmitConfirmOpen`, `submitState`, `submitError`, `submitResult`), `missingCount` computed, `formatDate` helper, implement `onSubmit()`, `onCancelSubmit()`, `onConfirmSubmit()`, fix `performAutosave()` guard từ `=== 'submitted'` thành `!== 'draft'`
- Task 9: Template update — submit button enabled/disabled binding, submit error message, input lock `[disabled]`, confirmation modal overlay, success state panel
- Task 10: 12 tests mới + cập nhật 1 test cũ (placeholder behavior) — 145 total Angular tests pass

### File List

**API (new):**
- `src/EnglishTestWeb.Api/Contracts/Submissions/FinalSubmitResult.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/SubmissionResultDto.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/20260612103443_AddFinalSubmitFields.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/20260612103443_AddFinalSubmitFields.Designer.cs`
- `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsFinalSubmitTests.cs`

**API (updated):**
- `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionStatuses.cs`
- `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs`
- `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionAnswer.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionAnswerConfiguration.cs`
- `src/EnglishTestWeb.Api/Application/Submissions/ISubmissionService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs`
- `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs`
- `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsTestHelper.cs`
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`

**Angular (updated):**
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.html`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.spec.ts`

**Sprint tracking:**
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-06-12: Story 4.4 implemented — Final submission and Reading/Listening auto-grading (claude-sonnet-4-6)
  - Backend: Domain entities extended với SubmittedAt, AutoScore, IsCorrect, Score fields
  - Backend: EF migration AddFinalSubmitFields; FinalSubmitAsync service method với idempotency + auto-grading
  - Backend: POST /api/submissions/{id}/submit endpoint (200 OK idempotent)
  - Angular: SubmissionResultDto model, finalSubmit() API service method
  - Angular: Confirmation modal, success state panel, input locking, performAutosave guard fix
  - Tests: 14 API integration tests + 12 Angular unit tests thêm mới
