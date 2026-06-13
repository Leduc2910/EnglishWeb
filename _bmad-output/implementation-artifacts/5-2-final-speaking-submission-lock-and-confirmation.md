---
baseline_commit: 36ecc0d0bac4e7af3a6eb04cb50a2274bd7aca9a
---

# Story 5.2: Final Speaking Submission Lock And Confirmation

Status: done

## Story

Là học sinh,
tôi muốn nộp bài Speaking chính thức với xác nhận rõ ràng,
để tôi biết đúng file đã được nộp cho đúng assignment/session.

## Acceptance Criteria

1. **Given** không có draft file hợp lệ đã upload
   **When** học sinh nhấn nút nộp bài
   **Then** nộp bài bị chặn với error code `speaking.fileRequired`.

2. **Given** có draft file hợp lệ
   **When** học sinh nhấn "Nộp bài chính thức"
   **Then** một modal xác nhận xuất hiện, hiển thị filename, tiêu đề bài test, lớp học, và mode (Homework / Thi trực tiếp).

3. **Given** học sinh xác nhận trong modal
   **When** API chấp nhận final submit
   **Then** `SpeakingSubmission.Status` chuyển thành `submitted`
   **And** file bị khóa, học sinh không thể thay thế hay upload thêm.

4. **Given** học sinh nhấn submit hai lần hoặc request bị retry
   **When** duplicate request đến API
   **Then** chỉ có một final submission được ghi lại
   **And** kết quả là idempotent (trả về 200 OK với DTO gốc) hoặc deterministic conflict.

5. **Given** final submit thành công
   **When** success panel xuất hiện
   **Then** hiển thị filename, submitted timestamp, lớp học, mode, và action quay lại danh sách.

6. **Given** assignment/session không còn mở
   **When** học sinh cố nộp bài chính thức
   **Then** API chặn với `speaking.sourceUnavailable`.

## Tasks / Subtasks

- [x] Task 1: Backend — Cập nhật DTO để thêm `SubmittedAt` (AC5)
  - [x] 1.1 Sửa `src/EnglishTestWeb.Api/Contracts/Speaking/SpeakingSubmissionDto.cs`:
    ```csharp
    public sealed record SpeakingSubmissionDto(
        Guid Id,
        string Status,
        string Mode,
        string TemplateTitle,
        string TemplateSkill,
        string ClassName,
        bool IsSourceOpen,
        string? CueMaterialFileId,
        string? CueMaterialFileName,
        DraftFileDto? DraftFile,
        DateTimeOffset? SubmittedAt);   // <-- thêm field này

    public sealed record DraftFileDto(
        Guid FileId,
        string OriginalFileName,
        long SizeBytes,
        DateTimeOffset UploadedAt);
    ```
  - [x] 1.2 Cập nhật `BuildDtoAsync` trong `SpeakingSubmissionService.cs` để include `submission.SubmittedAt` vào DTO.
    Tìm đoạn return `new SpeakingSubmissionDto(...)` và thêm `submission.SubmittedAt` vào cuối danh sách arguments.
  - [x] 1.3 `dotnet test` — xác nhận tests hiện có vẫn pass (record positional constructor change là non-breaking với tests dùng named args hoặc cần update).

- [x] Task 2: Backend — Thêm `FinalSubmitAsync` vào interface (AC1, AC3, AC4, AC6)
  - [x] 2.1 Sửa `src/EnglishTestWeb.Api/Application/Speaking/ISpeakingSubmissionService.cs`:
    ```csharp
    Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> FinalSubmitAsync(
        Guid speakingSubmissionId,
        string studentId,
        CancellationToken cancellationToken = default);
    ```

- [x] Task 3: Backend — Implement `FinalSubmitAsync` (AC1, AC3, AC4, AC6)
  - [x] 3.1 Thêm method `FinalSubmitAsync` vào `src/EnglishTestWeb.Api/Infrastructure/Speaking/SpeakingSubmissionService.cs`:

    **Logic:**
    ```csharp
    public async Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> FinalSubmitAsync(
        Guid speakingSubmissionId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        var submission = await db.SpeakingSubmissions
            .Include(s => s.HomeworkAssignment)
            .Include(s => s.LiveExamSession)
            .Include(s => s.DraftStoredFile)
            .Where(s => s.Id == speakingSubmissionId && s.StudentId == studentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return (false, "speaking.notFound", null);

        // Idempotent: đã submitted → trả về DTO gốc
        if (submission.Status == SpeakingSubmissionStatuses.Submitted)
        {
            var (tId, cId, isOpen) = await GetSourceInfoAsync(submission, cancellationToken);
            var existingDto = await BuildDtoAsync(submission, tId, cId, isOpen, cancellationToken);
            return (true, null, existingDto);
        }

        if (submission.Status != SpeakingSubmissionStatuses.Draft)
            return (false, "speaking.alreadySubmitted", null);

        // AC1: yêu cầu phải có draft file
        if (!submission.DraftStoredFileId.HasValue)
            return (false, "speaking.fileRequired", null);

        // AC6: kiểm tra source vẫn còn mở
        var (templateId, sourceClassId, isSourceOpen) = await GetSourceInfoAsync(submission, cancellationToken);
        if (!isSourceOpen)
            return (false, "speaking.sourceUnavailable", null);

        var now = timeProvider.GetUtcNow();
        submission.Status = SpeakingSubmissionStatuses.Submitted;
        submission.SubmittedAt = now;
        submission.UpdatedAt = now;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Row bị cập nhật đồng thời — re-query để lấy trạng thái mới nhất
            await db.Entry(submission).ReloadAsync(cancellationToken);
            if (submission.Status == SpeakingSubmissionStatuses.Submitted)
            {
                var dto2 = await BuildDtoAsync(submission, templateId, sourceClassId, isSourceOpen, cancellationToken);
                return (true, null, dto2);
            }
            throw;
        }

        var finalDto = await BuildDtoAsync(submission, templateId, sourceClassId, false, cancellationToken);
        return (true, null, finalDto);
    }
    ```

    **Lưu ý:** `SubmittedAt` field đã có sẵn trong entity từ story 5.1; chỉ cần gán giá trị.

  - [x] 3.2 `dotnet test` — xác nhận tests hiện có vẫn pass.

- [x] Task 4: Backend — Controller endpoint `POST /{id}/final-submit` (AC1-AC6)
  - [x] 4.1 Thêm vào `src/EnglishTestWeb.Api/Controllers/SpeakingSubmissionsController.cs`:
    ```csharp
    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpPost("{id:guid}/final-submit")]
    public async Task<ActionResult<SpeakingSubmissionDto>> FinalSubmit(
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

        var result = await speakingSubmissionService.FinalSubmitAsync(id, studentId, cancellationToken);

        if (!result.Success || result.Dto is null)
        {
            var statusCode = result.ErrorCode switch
            {
                "speaking.notFound" => StatusCodes.Status404NotFound,
                "speaking.fileRequired" => StatusCodes.Status422UnprocessableEntity,
                "speaking.sourceUnavailable" => StatusCodes.Status422UnprocessableEntity,
                "speaking.alreadySubmitted" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status422UnprocessableEntity,
            };

            return hiddenResourceResponseFactory.FromCode(
                statusCode,
                result.ErrorCode ?? "speaking.invalidState",
                "Final submit failed.",
                "Cannot finalize this speaking submission.");
        }

        return Ok(result.Dto);
    }
    ```

  - [x] 4.2 `dotnet test` — xác nhận tests hiện có vẫn pass.

- [x] Task 5: Backend — API tests (AC1-AC6)
  - [x] 5.1 Thêm vào `tests/EnglishTestWeb.Api.Tests/Speaking/SpeakingSubmissionsTests.cs`:
    ```
    // ---- POST /api/speaking-submissions/{id}/final-submit ----

    FinalSubmit_AsAnonymous_Returns401
      → gửi POST không có session cookie → 401 auth.unauthorized

    FinalSubmit_AsTeacher_Returns403
      → đăng nhập teacher, gửi POST → 403 auth.forbidden

    FinalSubmit_NoDraftFile_Returns422
      → tạo SpeakingSubmission chưa có DraftStoredFileId (CreateOrResume nhưng không UploadDraft)
      → gọi final-submit → 422 "speaking.fileRequired"

    FinalSubmit_WithDraftFile_Returns200
      → seed homework, đăng nhập student, CreateOrResume → UploadDraft → FinalSubmit
      → 200 OK; verify: status="submitted", submittedAt không null, draftFile không null

    FinalSubmit_Idempotent_ReturnsSameResult
      → gọi FinalSubmit 2 lần
      → lần 2 trả về 200 OK với cùng submittedAt

    FinalSubmit_SourceClosed_Returns422
      → seed homework với DeadlineAt = now.AddSeconds(-1) (đã hết hạn)
      → UploadDraft thành công (trước deadline) → thay deadline thành quá khứ trong DB → FinalSubmit
      → 422 "speaking.sourceUnavailable"
      LƯU Ý: Vì UploadDraft cũng kiểm tra source open, cần seed submission trực tiếp trong DB với DraftStoredFileId đặt trước, rồi đóng source, rồi gọi final-submit.

    FinalSubmit_OtherStudent_Returns404
      → seed submission với studentId khác trực tiếp trong DB
      → đăng nhập student1, gọi final-submit với id của submission student2 → 404

    FinalSubmit_LiveExamClosed_Returns422
      → seed live exam session với status=Closed (hoặc Scheduled)
      → seed SpeakingSubmission trực tiếp trong DB với DraftStoredFileId set
      → gọi final-submit → 422 "speaking.sourceUnavailable"
    ```

  - [x] 5.2 Thêm vào `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`:
    ```csharp
    // POST /api/speaking-submissions/{id}/final-submit
    // Unauthenticated → 401, Teacher → 403
    ```

  - [x] 5.3 Helper mới trong `SpeakingTestHelper.cs`:
    ```csharp
    internal static async Task<Guid> SeedSubmissionWithDraftAsync(
        TestApiFactory factory,
        Guid speakingSubmissionId,
        Guid homeworkOrSessionId,
        bool isHomework,
        string studentId)
    ```
    Dùng để seed `SpeakingSubmission` với `DraftStoredFileId` đặt trực tiếp qua DbContext (bypass upload endpoint), phục vụ các test cần kiểm soát source state độc lập với upload.

  - [x] 5.4 `dotnet test` — xác nhận tất cả tests pass.

- [x] Task 6: Angular — Cập nhật models và API service (AC1-AC6)
  - [x] 6.1 Sửa `src/EnglishTestWeb.Client/src/app/core/speaking/speaking.models.ts`:
    - Thêm `submittedAt: string | null;` vào `SpeakingSubmissionDto`
    - Thêm error message mới vào `SPEAKING_ERROR_MESSAGES`:
      ```typescript
      'speaking.fileRequired': 'Vui lòng tải lên file ghi âm trước khi nộp bài.',
      ```

  - [x] 6.2 Thêm method vào `src/EnglishTestWeb.Client/src/app/core/speaking/speaking-api.service.ts`:
    ```typescript
    finalSubmit(speakingSubmissionId: string): Promise<SpeakingSubmissionDto> {
      return firstValueFrom(
        this.http.post<SpeakingSubmissionDto>(
          `/api/speaking-submissions/${speakingSubmissionId}/final-submit`,
          {},
        ),
      );
    }
    ```

- [x] Task 7: Angular — Cập nhật component (AC1-AC6)
  - [x] 7.1 Sửa `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.ts`:

    **Thêm signals:**
    ```typescript
    protected readonly showConfirmModal = signal<boolean>(false);
    protected readonly finalSubmitState = signal<'idle' | 'submitting' | 'error'>('idle');
    protected readonly finalSubmitErrorCode = signal<string | null>(null);
    ```

    **Thêm computed:**
    ```typescript
    protected readonly canFinalSubmit = computed(() => {
      const d = this.dto();
      return (
        d !== null &&
        d.status === 'draft' &&
        d.isSourceOpen &&
        d.draftFile !== null &&
        this.uploadState() !== 'uploading'
      );
    });

    protected readonly finalSubmitErrorMessage = computed(() => {
      const code = this.finalSubmitErrorCode();
      if (!code) return 'Nộp bài thất bại. Vui lòng thử lại.';
      return SPEAKING_ERROR_MESSAGES[code] ?? 'Nộp bài thất bại. Vui lòng thử lại.';
    });
    ```

    **Thêm methods:**
    ```typescript
    protected onFinalSubmitClick(): void {
      if (!this.canFinalSubmit()) return;
      this.finalSubmitErrorCode.set(null);
      this.showConfirmModal.set(true);
    }

    protected onCancelSubmit(): void {
      this.showConfirmModal.set(false);
    }

    protected async onConfirmSubmit(): Promise<void> {
      const id = this.submissionId;
      if (!id) return;
      this.finalSubmitState.set('submitting');
      this.showConfirmModal.set(false);
      try {
        const updated = await this.speakingApi.finalSubmit(id);
        this.dto.set(updated);
        this.finalSubmitState.set('idle');
      } catch (err: unknown) {
        const code = this.extractErrorCode(err);
        this.finalSubmitErrorCode.set(code);
        this.finalSubmitState.set('error');
      }
    }
    ```

  - [x] 7.2 Sửa `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.html`:

    **Thay thế placeholder submit button bằng button thực:**
    Thêm block sau TRONG upload-card section, sau `@if (canUpload())` block:
    ```html
    @if (dto()!.status === 'draft') {
      <div class="final-submit-section" data-testid="final-submit-section">
        <button
          type="button"
          class="primary-button final-submit-btn"
          [disabled]="!canFinalSubmit() || finalSubmitState() === 'submitting'"
          (click)="onFinalSubmitClick()"
          data-testid="final-submit-btn"
        >
          @if (finalSubmitState() === 'submitting') {
            Đang nộp bài...
          } @else {
            Nộp bài chính thức
          }
        </button>
        @if (!dto()!.draftFile) {
          <p class="submit-hint" data-testid="no-file-hint">
            Vui lòng tải lên file ghi âm trước khi nộp bài.
          </p>
        }
        @if (finalSubmitState() === 'error') {
          <p class="submit-error" data-testid="submit-error" role="alert">
            {{ finalSubmitErrorMessage() }}
          </p>
        }
      </div>
    }
    ```

    **Thêm success panel** (thay thế nội dung khi status = submitted):
    Trong `@if (viewState() === 'loaded')` block, thêm sau speaking-body:
    ```html
    @if (dto()!.status === 'submitted') {
      <div class="success-panel" data-testid="success-panel">
        <div class="success-icon">✅</div>
        <h2 class="success-title">Đã nộp bài thành công!</h2>
        <dl class="success-details">
          <dt>File đã nộp</dt>
          <dd data-testid="success-filename">{{ dto()!.draftFile?.originalFileName ?? '—' }}</dd>
          <dt>Thời gian nộp</dt>
          <dd data-testid="success-submitted-at">
            {{ dto()!.submittedAt ? formatDate(dto()!.submittedAt!) : '—' }}
          </dd>
          <dt>Lớp học</dt>
          <dd data-testid="success-class">{{ dto()!.className }}</dd>
          <dt>Loại bài</dt>
          <dd data-testid="success-mode">{{ modeLabel() }}</dd>
        </dl>
        <button type="button" class="primary-button" (click)="backToTests()" data-testid="back-to-tests-btn">
          Quay lại danh sách bài thi
        </button>
      </div>
    }
    ```

    **Thêm confirmation modal** (trước closing `</div>` của speaking-page):
    ```html
    @if (showConfirmModal()) {
      <div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="confirm-modal-title">
        <div class="modal-card" data-testid="confirm-modal">
          <h2 id="confirm-modal-title" class="modal-title">Xác nhận nộp bài Speaking</h2>
          <dl class="modal-details">
            <dt>File</dt>
            <dd data-testid="modal-filename">{{ dto()!.draftFile?.originalFileName }}</dd>
            <dt>Bài thi</dt>
            <dd data-testid="modal-template-title">{{ dto()!.templateTitle }}</dd>
            <dt>Lớp</dt>
            <dd data-testid="modal-class">{{ dto()!.className }}</dd>
            <dt>Loại</dt>
            <dd data-testid="modal-mode">{{ modeLabel() }}</dd>
          </dl>
          <p class="modal-warning">Sau khi nộp, bạn không thể thay đổi file ghi âm.</p>
          <div class="modal-actions">
            <button type="button" class="secondary-button" (click)="onCancelSubmit()" data-testid="cancel-submit-btn">
              Hủy
            </button>
            <button type="button" class="primary-button" (click)="onConfirmSubmit()" data-testid="confirm-submit-btn">
              Xác nhận nộp bài
            </button>
          </div>
        </div>
      </div>
    }
    ```

  - [x] 7.3 Thêm styles vào `student-speaking-submission.component.css`:
    ```css
    .final-submit-section { margin-top: 1.5rem; }
    .final-submit-btn { width: 100%; }
    .submit-hint { font-size: 0.875rem; color: var(--color-text-muted); margin-top: 0.5rem; }
    .submit-error { color: var(--color-error); font-size: 0.875rem; margin-top: 0.5rem; }

    .success-panel {
      background: var(--color-surface);
      border: 1px solid var(--color-success);
      border-radius: 8px;
      padding: 2rem;
      text-align: center;
    }
    .success-icon { font-size: 3rem; margin-bottom: 1rem; }
    .success-title { color: var(--color-success); margin-bottom: 1.5rem; }
    .success-details { text-align: left; display: grid; grid-template-columns: auto 1fr; gap: 0.5rem 1rem; }
    .success-details dt { font-weight: 600; color: var(--color-text-muted); }

    .modal-backdrop {
      position: fixed; inset: 0;
      background: rgba(0,0,0,0.5);
      display: flex; align-items: center; justify-content: center;
      z-index: 100;
    }
    .modal-card {
      background: var(--color-surface);
      border-radius: 8px; padding: 2rem;
      max-width: 480px; width: 90%;
    }
    .modal-title { margin-bottom: 1.5rem; }
    .modal-details { display: grid; grid-template-columns: auto 1fr; gap: 0.5rem 1rem; margin-bottom: 1rem; }
    .modal-details dt { font-weight: 600; color: var(--color-text-muted); }
    .modal-warning { color: var(--color-warning); font-size: 0.875rem; margin-bottom: 1.5rem; }
    .modal-actions { display: flex; gap: 1rem; justify-content: flex-end; }
    ```

  - [x] 7.4 `npm test` — xác nhận tests hiện có vẫn pass.

- [x] Task 8: Angular — Unit tests (AC1-AC6)
  - [x] 8.1 Sửa `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.spec.ts`:

    **Thêm mock `finalSubmit` vào speakingApi:**
    ```typescript
    speakingApi = {
      get: ...,
      uploadDraft: ...,
      createOrResume: ...,
      finalSubmit: vi.fn().mockResolvedValue(makeDto({ status: 'submitted', submittedAt: '2026-06-13T10:00:00Z' })),
    };
    ```

    **Thêm helper draftFile vào `makeDto`:**
    ```typescript
    function makeDraftFile(): DraftFileDto {
      return { fileId: 'f-1', originalFileName: 'recording.mp3', sizeBytes: 1024, uploadedAt: '2026-06-13T09:00:00Z' };
    }
    ```

    **Test cases cần thêm:**
    ```
    shows final-submit-btn when status=draft and draftFile exists
    disables final-submit-btn when draftFile is null
    disables final-submit-btn when source is closed (isSourceOpen=false)
    shows no-file-hint when draftFile is null and status=draft
    hides final-submit-btn when status=submitted
    
    clicking final-submit-btn opens confirm-modal
    confirm-modal shows filename, templateTitle, className, mode
    clicking cancel-submit-btn closes confirm-modal
    
    clicking confirm-submit-btn calls finalSubmit API
    on finalSubmit success: dto.status = submitted → success-panel shown
    success-panel shows filename, submittedAt, className, mode
    success-panel shows back-to-tests-btn
    
    on finalSubmit error: submit-error shown with message
    final-submit-btn not disabled when finalSubmitState = error (retry allowed)
    
    submit panel: shows submitted-notice when dto loaded with status=submitted
    ```

  - [x] 8.2 `npm test` — xác nhận tất cả tests pass.

- [x] Task 9: Update sprint status
  - [x] 9.1 Cập nhật `_bmad-output/implementation-artifacts/sprint-status.yaml`:
    - Đổi `5-2-final-speaking-submission-lock-and-confirmation: backlog` → `in-progress`
    - Cập nhật `last_updated`

## Dev Notes

### Domain: Idempotent Final Submit

`FinalSubmitAsync` thiết kế **idempotent**: nếu `Status == Submitted` → trả về 200 OK với DTO hiện tại (không throw error, không 409). Điều này đơn giản hóa retry logic ở phía client.

Ngoại lệ: nếu submission đang ở trạng thái khác (ví dụ graded từ story 5.3) → return `speaking.alreadySubmitted` với 409. Nhưng trong MVP flow, Submitted là trạng thái cuối của học sinh.

### Backend: SubmittedAt đã có sẵn trong entity

`SpeakingSubmission.SubmittedAt` (`DateTimeOffset?`) đã được định nghĩa trong entity từ story 5.1 nhưng chưa bao giờ được gán (luôn null). Story 5.2 chỉ cần gán giá trị khi final submit.

### Backend: DTO Breaking Change

Thêm `SubmittedAt` vào `SpeakingSubmissionDto` record là positional constructor change. Cần kiểm tra các test trong `SpeakingSubmissionsTests.cs` có dùng object initializer hay positional constructor — nếu dùng positional, cần cập nhật call site.

**Cách an toàn nhất:** Thêm `SubmittedAt` vào cuối parameter list của record. Positional constructor call sites cần `null` argument thêm vào cuối. Tests dùng JSON deserialization không bị ảnh hưởng.

### Backend: Concurrency — RowVersion

`SpeakingSubmission` có `RowVersion` (configured làm SQL Server rowversion trong EF). `DbUpdateConcurrencyException` sẽ được throw nếu hai request đồng thời cùng final-submit. Pattern: catch exception, reload entity, nếu trạng thái là Submitted → trả về 200 OK (idempotent winner-takes-all).

### Angular: Template Structure

HTML template hiện tại ở story 5.1 có `@if (dto()!.status !== 'draft')` hiển thị "Bài đã được nộp." — cần **replace** block này bằng success panel thực sự. Không để hai notification cùng lúc.

Thứ tự render trong `speaking-body`:
1. `prompt-card` (luôn hiển thị khi loaded)
2. `upload-card` (ẩn upload section và final-submit-btn khi status = submitted)
3. `success-panel` (chỉ khi status = submitted, thay thế uploaded-file info + action buttons)
4. `confirm-modal` (overlay, fixed position)

Nếu `dto().status === 'submitted'`, ẩn upload section (đã handle bởi `canUpload()` computed) và ẩn final-submit-section. Success panel hiển thị thay thế.

### Angular: `makeDto` trong spec cần `submittedAt`

Sau khi thêm `submittedAt: string | null` vào interface `SpeakingSubmissionDto`, function `makeDto()` trong spec file cần thêm `submittedAt: null` vào default values. Không quên cập nhật tất cả test fixture.

### Angular: Modal Accessibility

Modal cần `role="dialog"`, `aria-modal="true"`, `aria-labelledby`. Focus cần được trap trong modal khi mở — đủ cho MVP, không cần full focus-trap library.

### Error Codes Mới

- `speaking.fileRequired` — 422: chưa có draft file để nộp (AC1)
- Đã có: `speaking.sourceUnavailable` — 422: source đã đóng (AC6)
- Đã có: `speaking.alreadySubmitted` — 409: đã nộp rồi (non-idempotent case)

Thêm `speaking.fileRequired` vào cả `SPEAKING_ERROR_MESSAGES` (Angular) và controller error map (C#).

### Anti-Patterns

- **KHÔNG** xóa `SubmittedStoredFileId` riêng — dùng chính `DraftStoredFileId` làm submitted file (đã locked)
- **KHÔNG** allow `UploadDraft` sau khi `Status = Submitted` — kiểm tra này đã có sẵn từ story 5.1
- **KHÔNG** auto-redirect sau submit — success panel ở cùng trang, user click "Quay lại" chủ động
- **KHÔNG** implement file deletion khi submit — file archive/retention là deferred concern
- **KHÔNG** add teacher grading endpoint trong story này — story 5.3 sẽ handle
- **KHÔNG** dùng 409 cho idempotent double-submit — return 200 OK thay

### Files Cần Tạo/Sửa

**API (update):**
- `src/EnglishTestWeb.Api/Contracts/Speaking/SpeakingSubmissionDto.cs` — thêm `SubmittedAt`
- `src/EnglishTestWeb.Api/Application/Speaking/ISpeakingSubmissionService.cs` — thêm `FinalSubmitAsync`
- `src/EnglishTestWeb.Api/Infrastructure/Speaking/SpeakingSubmissionService.cs` — implement `FinalSubmitAsync`, cập nhật `BuildDtoAsync`
- `src/EnglishTestWeb.Api/Controllers/SpeakingSubmissionsController.cs` — thêm `POST /{id}/final-submit`
- `tests/EnglishTestWeb.Api.Tests/Speaking/SpeakingTestHelper.cs` — thêm `SeedSubmissionWithDraftAsync`
- `tests/EnglishTestWeb.Api.Tests/Speaking/SpeakingSubmissionsTests.cs` — thêm tests cho final-submit
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — thêm endpoint mới

**Angular (update):**
- `src/EnglishTestWeb.Client/src/app/core/speaking/speaking.models.ts` — thêm `submittedAt`, error message
- `src/EnglishTestWeb.Client/src/app/core/speaking/speaking-api.service.ts` — thêm `finalSubmit`
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.ts` — thêm signals, methods
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.html` — thêm final-submit-btn, confirm-modal, success-panel
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.css` — thêm styles
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.spec.ts` — thêm/cập nhật tests

### Architecture Compliance

- **Controller không access DbContext** — delegate sang `ISpeakingSubmissionService`
- **Idempotency** — second submit → 200 OK (winner-takes-all via concurrency exception or status check)
- **RowVersion concurrency** — `DbUpdateConcurrencyException` caught và handled
- **Student scope** — `FinalSubmitAsync` verify `StudentId == studentId`
- **Source open check** — re-checked từ DB state, không trust client
- **ProblemDetails** — tất cả error response qua `hiddenResourceResponseFactory`
- **TimeProvider** — dùng `timeProvider.GetUtcNow()` cho `SubmittedAt`

### Context Từ Previous Stories

1. **FinalSubmit pattern** — `SubmissionsController.FinalSubmit` (story 4.4): same idempotency pattern, `DbUpdateConcurrencyException`, RowVersion
2. **GetSourceInfoAsync** — đã có sẵn trong `SpeakingSubmissionService` từ story 5.1; dùng lại
3. **BuildDtoAsync** — đã có sẵn; chỉ cần update để include `SubmittedAt`
4. **Auth test helpers** — `AuthTestHelper.SignInStudentWithClassAsync`, `SignInTeacherAsync`
5. **SpeakingTestHelper** — `SeedSpeakingHomeworkAsync`, `SeedSpeakingLiveExamAsync`, `CreateSpeakingSubmissionAsync` từ story 5.1
6. **Upload pattern** — story 5.1: `CreateSpeakingSubmissionAsync` → `UploadDraft` → có DraftStoredFileId

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 5, Story 5.2]
- [Source: `_bmad-output/implementation-artifacts/5-1-student-speaking-prompt-and-upload-draft.md` — Previous story context]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Speaking/SpeakingSubmissionService.cs` — Service to extend]
- [Source: `src/EnglishTestWeb.Api/Controllers/SpeakingSubmissionsController.cs` — Controller to extend]
- [Source: `src/EnglishTestWeb.Api/Application/Speaking/ISpeakingSubmissionService.cs` — Interface to extend]
- [Source: `src/EnglishTestWeb.Api/Contracts/Speaking/SpeakingSubmissionDto.cs` — DTO to update]
- [Source: `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs` — FinalSubmit pattern reference]
- [Source: `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.ts` — Component to extend]
- [Source: `tests/EnglishTestWeb.Api.Tests/Speaking/SpeakingTestHelper.cs` — Test helpers to extend]
- [Source: `tests/EnglishTestWeb.Api.Tests/Speaking/SpeakingSubmissionsTests.cs` — Tests to extend]

## Senior Developer Review (AI)

**Review Date:** 2026-06-13
**Outcome:** Changes Requested
**Action Items:** 7 total (1 decision-needed, 6 patch)
**Severity:** 2 High, 2 Medium, 2 Low

### Action Items

- [x] [Review][Defer] F1 — `graded` status trả về 409 `speaking.alreadySubmitted` — deferred to story 5.3 (graded state not yet creatable)
- [x] [Review][Patch] F2 — `submitted-notice` và `success-panel` cùng render khi status=submitted — FIXED: xóa submitted-notice block
- [x] [Review][Patch] F3 — `confirm-submit-btn` thiếu `[disabled]` khi submitting — FIXED: added [disabled]="finalSubmitState() === 'submitting'"
- [x] [Review][Patch] F4 — Test confirm-modal thiếu assertion modal-class/modal-mode — FIXED: added assertions
- [x] [Review][Patch] F5 — `success-submitted-at` test dùng `.toBeTruthy()` — FIXED: strengthened assertion (not '—', length > 0)
- [x] [Review][Patch] F6 — Thiếu test uploadState='uploading' — FIXED: added test case
- [x] [Review][Patch] F7 — Already had ReadProblemCodeAsync assertion — dismissed as false positive
- [x] [Review][Defer] D1 — `isSourceOpen` hardcoded `false` sau final submit trong BuildDtoAsync — intentional design [SpeakingSubmissionService.cs] — deferred, intentional: post-submit source status is irrelevant
- [x] [Review][Defer] D2 — `DbUpdateConcurrencyException` dead code trong in-memory test DB — không thể test concurrency path [SpeakingSubmissionService.cs:324-336] — deferred, pre-existing architectural constraint

## Dev Agent Record

### Completion Notes

- Tất cả 9 tasks hoàn thành, 277 backend tests pass, 172 Angular tests pass.
- `FinalSubmitAsync` idempotent: status=Submitted → 200 OK (không throw 409).
- `DbUpdateConcurrencyException` được catch và handle: reload entity, nếu đã Submitted → trả 200 OK.
- `SeedSubmissionWithDraftAsync` helper bypass upload endpoint, seed trực tiếp qua DbContext để test source-closed và other-student scenarios.
- Success panel hiển thị inline trong cùng trang (không auto-redirect).
- Confirmation modal có đủ accessibility attributes (role, aria-modal, aria-labelledby).

### File List

- `src/EnglishTestWeb.Api/Contracts/Speaking/SpeakingSubmissionDto.cs` (modified)
- `src/EnglishTestWeb.Api/Application/Speaking/ISpeakingSubmissionService.cs` (modified)
- `src/EnglishTestWeb.Api/Infrastructure/Speaking/SpeakingSubmissionService.cs` (modified)
- `src/EnglishTestWeb.Api/Controllers/SpeakingSubmissionsController.cs` (modified)
- `tests/EnglishTestWeb.Api.Tests/Speaking/SpeakingTestHelper.cs` (modified)
- `tests/EnglishTestWeb.Api.Tests/Speaking/SpeakingSubmissionsTests.cs` (modified)
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` (modified)
- `src/EnglishTestWeb.Client/src/app/core/speaking/speaking.models.ts` (modified)
- `src/EnglishTestWeb.Client/src/app/core/speaking/speaking-api.service.ts` (modified)
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.ts` (modified)
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.html` (modified)
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.css` (modified)
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.spec.ts` (modified)

## Senior Developer Review (AI) — Round 2

**Review Date:** 2026-06-13
**Outcome:** Approved (after patches)
**Action Items:** 3 patched, 3 deferred

### Action Items

- [x] [Review][Patch] R2-F1 — `Assert.NotNull(ternary)` on line 741 trivially passes, redundant (real check on line 743) — FIXED: removed
- [x] [Review][Patch] R2-F2 — Dead variable `studentId` in `FinalSubmit_OtherStudent_Returns404` — FIXED: removed dead var
- [x] [Review][Patch] R2-F3 — `closed-notice` renders alongside `success-panel` when status=submitted/graded — FIXED: `@else` → `@else if (status === 'draft')`. Added test coverage.
- [x] [Review][Defer] R2-D1 — No hint for "source closed" near disabled submit button — header badge "Đã đóng" sufficient; deferred
- [x] [Review][Defer] R2-D2 — `canFinalSubmit` doesn't guard `finalSubmitState=submitting` — template `[disabled]` covers; deferred
- [x] [Review][Defer] R2-D3 — `FinalSubmit_LiveExamClosed` bypasses SeedSubmissionWithDraftAsync — maintenance concern only; deferred

**Final counts: 277 backend tests, 174 Angular tests — all pass.**

## Change Log

- 2026-06-13: Story 5.2 created — Final Speaking Submission Lock And Confirmation (claude-sonnet-4-6)
- 2026-06-13: Story 5.2 implementation complete — 277 backend + 172 Angular tests pass (claude-sonnet-4-6)
- 2026-06-13: Code review round 1 complete — 6 patches applied (claude-sonnet-4-6)
- 2026-06-13: Code review round 2 complete — 3 patches applied; story status → done; 277 backend + 174 Angular tests pass (claude-sonnet-4-6)
