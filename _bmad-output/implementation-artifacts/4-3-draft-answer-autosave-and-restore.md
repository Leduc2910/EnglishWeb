---
baseline_commit: 9840da251651e47cd008504edcad4d79fd0379c8
---

# Story 4.3: Draft Answer Autosave And Restore

Status: done

## Story

Là học sinh,
tôi muốn câu trả lời Reading/Listening được lưu tự động khi tôi làm bài,
để reload hoặc mất mạng tạm thời không xóa mất tiến trình của tôi.

## Acceptance Criteria

1. **Given** học sinh nhập một câu trả lời
   **When** input thay đổi
   **Then** câu trả lời được lưu cục bộ (in-memory signal) ngay lập tức
   **And** một autosave lên server được đặt vào hàng đợi cho attempt đó.

2. **Given** kết nối online và bình thường
   **When** autosave thành công
   **Then** UI hiển thị "Đã lưu" trong vòng 1 giây.

3. **Given** autosave đang pending hoặc thất bại
   **When** học sinh tiếp tục làm bài
   **Then** UI hiển thị trạng thái saving/offline/degraded mà không tuyên bố "nộp bài thành công".

4. **Given** học sinh reload một attempt đang làm dở
   **When** có draft được lưu trên server
   **Then** câu trả lời được khôi phục từ server
   **And** trạng thái khôi phục đủ rõ để tránh nhầm lẫn.

5. **Given** final submission đã tồn tại
   **When** một autosave request muộn đến
   **Then** API từ chối hoặc bỏ qua một cách có hệ thống
   **And** các câu trả lời đã submit vẫn bị lock.

6. **Given** nhiều autosave requests đến không theo thứ tự
   **When** API xử lý chúng
   **Then** timestamp server-side đảm bảo dữ liệu mới nhất không bị ghi đè bởi request cũ.

## Tasks / Subtasks

- [x] Task 1: Backend — SubmissionAnswer entity (AC1, AC4, AC5, AC6)
  - [x] 1.1 Tạo `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionAnswer.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Domain.Submissions;

    public sealed class SubmissionAnswer
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public int QuestionNumber { get; set; }
        public string? Answer { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Submission? Submission { get; set; }
    }
    ```
  - [x] 1.2 Thêm navigation property vào `Submission.cs`:
    ```csharp
    public ICollection<SubmissionAnswer> Answers { get; set; } = [];
    ```

- [x] Task 2: Backend — EF Core config + migration (AC1, AC4, AC5, AC6)
  - [x] 2.1 Tạo `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionAnswerConfiguration.cs`:
    ```csharp
    public sealed class SubmissionAnswerConfiguration : IEntityTypeConfiguration<SubmissionAnswer>
    {
        public void Configure(EntityTypeBuilder<SubmissionAnswer> entity)
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Answer).HasMaxLength(500);
            entity.HasIndex(a => new { a.SubmissionId, a.QuestionNumber }).IsUnique();
            entity.HasOne(a => a.Submission)
                .WithMany(s => s.Answers)
                .HasForeignKey(a => a.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
    ```
    **Lưu ý**: `OnDelete(Cascade)` là đúng ở đây — nếu Submission bị xóa, answers cũng xóa theo. Đây khác với các FKs khác dùng Restrict.
  - [x] 2.2 Thêm `DbSet<SubmissionAnswer>` vào `EnglishTestWebDbContext.cs`:
    ```csharp
    public DbSet<SubmissionAnswer> SubmissionAnswers => Set<SubmissionAnswer>();
    ```
  - [x] 2.3 `dotnet ef migrations add AddSubmissionAnswers --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj`
  - [x] 2.4 Inspect migration file — xác nhận có unique index `(SubmissionId, QuestionNumber)` và FK với Cascade DELETE
  - [x] 2.5 `dotnet test` — xác nhận tests hiện có vẫn pass

- [x] Task 3: Backend — Contracts/DTOs cho autosave (AC1, AC5)
  - [x] 3.1 Tạo `src/EnglishTestWeb.Api/Contracts/Submissions/AutosaveAnswersRequest.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Submissions;

    public sealed record AutosaveAnswersRequest(IReadOnlyList<AnswerRowDto> Rows);
    ```
  - [x] 3.2 Tạo `src/EnglishTestWeb.Api/Contracts/Submissions/AutosaveAnswersResult.cs`:
    ```csharp
    namespace EnglishTestWeb.Api.Contracts.Submissions;

    public sealed record AutosaveAnswersResult(bool Success, string? ErrorCode);
    ```

- [x] Task 4: Backend — Update ISubmissionService + implementation (AC1, AC4, AC5, AC6)
  - [x] 4.1 Thêm method vào `ISubmissionService.cs`:
    ```csharp
    Task<AutosaveAnswersResult> AutosaveAnswersAsync(
        Guid submissionId,
        string studentId,
        AutosaveAnswersRequest request,
        CancellationToken cancellationToken = default);
    ```
  - [x] 4.2 Implement `AutosaveAnswersAsync` trong `SubmissionService.cs`:
    ```csharp
    public async Task<AutosaveAnswersResult> AutosaveAnswersAsync(
        Guid submissionId,
        string studentId,
        AutosaveAnswersRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Load submission — verify ownership và status
        var submission = await db.Submissions
            .Include(s => s.Answers)
            .Where(s => s.Id == submissionId && s.StudentId == studentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return new AutosaveAnswersResult(false, "submission.notFound");

        // AC5: Reject autosave nếu đã submitted
        if (submission.Status != SubmissionStatuses.Draft)
            return new AutosaveAnswersResult(false, "submission.notDraft");

        var now = timeProvider.GetUtcNow();

        // AC6: Upsert từng row — server controls UpdatedAt
        var existingMap = submission.Answers.ToDictionary(a => a.QuestionNumber);

        foreach (var row in request.Rows)
        {
            if (existingMap.TryGetValue(row.QuestionNumber, out var existing))
            {
                existing.Answer = row.Answer;
                existing.UpdatedAt = now;
            }
            else
            {
                db.SubmissionAnswers.Add(new SubmissionAnswer
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    QuestionNumber = row.QuestionNumber,
                    Answer = row.Answer,
                    UpdatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AutosaveAnswersResult(true, null);
    }
    ```
  - [x] 4.3 Cập nhật `GetWorkspaceAsync` trong `SubmissionService.cs` để populate `AnswerRows`:
    - Sau khi query submission workspace, thêm query:
    ```csharp
    var answerRows = await db.SubmissionAnswers
        .AsNoTracking()
        .Where(a => a.SubmissionId == submissionId)
        .OrderBy(a => a.QuestionNumber)
        .Select(a => new AnswerRowDto(a.QuestionNumber, a.Answer))
        .ToListAsync(cancellationToken);
    ```
    - Truyền `answerRows` (thay vì `[]`) vào `SubmissionWorkspaceDto`
  - [x] 4.4 `dotnet test` — xác nhận tests hiện có vẫn pass

- [x] Task 5: Backend — Controller endpoint (AC1, AC2, AC3, AC5)
  - [x] 5.1 Thêm endpoint vào `SubmissionsController.cs`:
    ```csharp
    /// <summary>
    /// PUT /api/submissions/{id}/answers — autosave draft answers
    /// </summary>
    [HttpPut("{id:guid}/answers")]
    public async Task<IActionResult> AutosaveAnswers(
        Guid id,
        [FromBody] AutosaveAnswersRequest request,
        CancellationToken cancellationToken)
    {
        var studentId = currentUserContext.UserId;
        var result = await submissionService.AutosaveAnswersAsync(id, studentId!, request, cancellationToken);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "submission.notFound" => hiddenResourceFactory.NotFound("submission.notFound"),
                "submission.notDraft" => Conflict(new ProblemDetails
                {
                    Extensions = { ["code"] = "submission.notDraft" }
                }),
                _ => BadRequest()
            };
        }

        return NoContent(); // 204 — autosave thành công
    }
    ```
    **Lưu ý**: Trả về `204 No Content` cho autosave thành công (không cần response body).
    Dùng `hiddenResourceFactory` theo pattern của project (xem `SubmissionsController` các methods khác).
  - [x] 5.2 `dotnet test` — xác nhận tests hiện có vẫn pass

- [x] Task 6: Backend — API tests (AC1, AC2, AC4, AC5, AC6)
  - [x] 6.1 Thêm helper `CreateSubmissionWithAnswersAsync` vào `SubmissionsTestHelper.cs`:
    - Tạo submission, autosave answers, trả về `(submissionId, homeworkId, classId)`
  - [x] 6.2 Tạo hoặc cập nhật `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsAutosaveTests.cs`:
    - `AutosaveAnswers_AsAnonymous_Returns401`
    - `AutosaveAnswers_AsTeacher_Returns403`
    - `AutosaveAnswers_AsOwnerStudent_Returns204`
    - `AutosaveAnswers_AsOtherStudent_Returns404` (submission của student khác)
    - `AutosaveAnswers_Idempotent_LastValueWins` (POST answers → GET workspace → answers khớp)
    - `AutosaveAnswers_AfterSubmitted_Returns409` (submission.notDraft — cần story 4.4 để set status Submitted; skip hoặc seed manually với status="submitted")
    - `GetWorkspace_AfterAutosave_ReturnsAnswerRows` — xác nhận `answerRows` populated từ DB
  - [x] 6.3 Thêm `PUT /api/submissions/{id}/answers` vào `AuthorizationMatrixTests.cs`
  - [x] 6.4 `dotnet test` — xác nhận tất cả tests pass

- [x] Task 7: Angular — Update SubmissionsApiService (AC1, AC2, AC3)
  - [x] 7.1 Thêm interface và method vào `submissions-api.service.ts`:
    ```typescript
    autosaveAnswers(
      submissionId: string,
      rows: { questionNumber: number; answer: string | null }[],
    ): Promise<void> {
      return firstValueFrom(
        this.http.put<void>(`/api/submissions/${submissionId}/answers`, { rows }),
      );
    }
    ```

- [x] Task 8: Angular — Autosave logic trong workspace component (AC1, AC2, AC3, AC4)
  - [x] 8.1 Thêm imports và signals vào `student-attempt-workspace.component.ts`:
    ```typescript
    import { Subject, debounceTime, takeUntilDestroyed } from 'rxjs';
    import { DestroyRef } from '@angular/core';

    type AutosaveStatus = 'idle' | 'saving' | 'saved' | 'error';

    protected readonly autosaveStatus = signal<AutosaveStatus>('idle');
    private readonly autosaveTrigger$ = new Subject<void>();
    private readonly destroyRef = inject(DestroyRef);
    ```
  - [x] 8.2 Thiết lập debounce pipeline trong `ngOnInit()` (sau khi `submissionId` được set):
    ```typescript
    this.autosaveTrigger$
      .pipe(debounceTime(800), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.performAutosave());
    ```
  - [x] 8.3 Cập nhật `onAnswerChange()`:
    ```typescript
    protected onAnswerChange(questionNumber: number, value: string): void {
      this.answerInputs.update((current) => ({ ...current, [questionNumber]: value }));
      this.autosaveTrigger$.next(); // trigger debounced autosave
    }
    ```
  - [x] 8.4 Thêm method `performAutosave()`:
    ```typescript
    private async performAutosave(): Promise<void> {
      const id = this.submissionId;
      const ws = this.workspace();
      if (!id || !ws || ws.status === 'submitted') return;

      this.autosaveStatus.set('saving');

      const rows = Object.entries(this.answerInputs()).map(([qn, ans]) => ({
        questionNumber: Number(qn),
        answer: ans || null,
      }));

      try {
        await this.submissionsApi.autosaveAnswers(id, rows);
        this.autosaveStatus.set('saved');
      } catch {
        this.autosaveStatus.set('error');
      }
    }
    ```
  - [x] 8.5 Cập nhật template — vùng autosave status (`data-testid="autosave-status"`):
    ```html
    <span data-testid="autosave-status" [attr.aria-live]="'polite'">
      @switch (autosaveStatus()) {
        @case ('saving') { <span class="autosave-saving">Đang lưu...</span> }
        @case ('saved') { <span class="autosave-saved">Đã lưu</span> }
        @case ('error') { <span class="autosave-error">Lưu thất bại</span> }
        @default { <span class="autosave-idle">—</span> }
      }
    </span>
    ```
    **Lưu ý**: Dùng `aria-live="polite"` để screen reader thông báo khi status thay đổi. Dùng `@switch` (Angular 17+ control flow), KHÔNG dùng `*ngSwitch`.
  - [x] 8.6 Xác nhận `loadWorkspace` đã restore answers từ `ws.answerRows` (story 4.2 đã implement):
    ```typescript
    // Đã có trong story 4.2 — KHÔNG thay đổi:
    const initialAnswers: Record<number, string> = {};
    for (const row of ws.answerRows) {
      if (row.answer !== null) {
        initialAnswers[row.questionNumber] = row.answer;
      }
    }
    this.answerInputs.set(initialAnswers);
    ```
    Restore hoạt động vì `GetWorkspaceAsync` bây giờ populate `answerRows` từ DB (Task 4.3).

- [x] Task 9: Angular — Unit tests (AC1, AC2, AC3, AC4)
  - [x] 9.1 Cập nhật `student-attempt-workspace.component.spec.ts`:
    - Mock `SubmissionsApiService` — thêm `autosaveAnswers: vi.fn().mockResolvedValue(undefined)`
    - Test: `nhập câu trả lời → autosaveStatus chuyển thành saving rồi saved`
    - Test: `autosave thất bại → autosaveStatus hiển thị error`
    - Test: `load workspace có answerRows → answerInputs được khôi phục`
    - Test: `autosave-status region có aria-live`
    - Test: `workspace status=submitted → không gọi autosaveAnswers`
  - [x] 9.2 `npm test` — xác nhận tất cả tests pass

## Dev Notes

### Backend: SubmissionAnswer Entity Design

`SubmissionAnswer` lưu câu trả lời draft của học sinh. Mỗi row = một câu hỏi. Unique index trên `(SubmissionId, QuestionNumber)` đảm bảo chỉ một câu trả lời per câu hỏi per attempt.

**Tại sao dùng Cascade DELETE**: `SubmissionAnswer` là owned data của `Submission` — nếu xóa Submission (không xảy ra trong MVP nhưng cần safe), answers cũng phải xóa. Khác với FKs tới `HomeworkAssignment`/`LiveExamSession` dùng Restrict (đó là lookup data).

### Backend: Autosave Concurrency Strategy (AC6)

Vì học sinh chỉ có một session làm bài tại một thời điểm, "out of order" requests thực tế đến từ debounce race condition (ví dụ: học sinh type nhanh, 2 requests được gửi gần nhau). Strategy:

1. Client debounce 800ms → chỉ gửi sau khi user ngừng type
2. Server upsert với server-side `UpdatedAt = now` → last-write-wins
3. In-memory `answerInputs` signal luôn là source of truth hiển thị → user không thấy stale data dù request nào win

Không cần client-provided timestamps. Không cần optimistic concurrency token cho autosave rows vì:
- Single student, single device assumption (MVP)
- Client debounce ngăn storm requests
- Upsert semantics đủ safe

### Backend: Validation Trong AutosaveAnswersAsync

**QuestionNumber bounds**: Story không yêu cầu validate question numbers trong bounds của questionCount. Tuy nhiên, nếu muốn defensive, có thể query `Submission.AnswerKeyVersionId → AnswerKeyVersion.QuestionCount` và reject rows với `QuestionNumber > questionCount`. Không bắt buộc cho story này — KHÔNG làm extra work.

**Answer length**: `HasMaxLength(500)` đủ cho trắc nghiệm. Adjust nếu cần.

### Backend: Controller Pattern — IHiddenResourceResponseFactory

Xem `SubmissionsController` story 4.2 — dùng `hiddenResourceFactory.NotFound(...)` cho `submission.notFound`. Pattern này injected qua constructor.

Xem `FilesController.cs` hoặc `SubmissionsController.cs` để tìm pattern inject `IHiddenResourceResponseFactory`.

### Backend: GetWorkspaceAsync — Populate AnswerRows

**Trước story 4.3**: `AnswerRows = []` (empty list hardcoded)
**Sau story 4.3**: Query thêm `SubmissionAnswers` table

Đảm bảo query efficient — không cần JOIN, chỉ cần WHERE `SubmissionId = id` với index.

### Angular: Debounce Pattern Với RxJS

Dùng `Subject` + `debounceTime` thay vì `setTimeout`:

```typescript
// Trong constructor hoặc field initializer:
private readonly autosaveTrigger$ = new Subject<void>();

// Trong ngOnInit (sau khi có submissionId):
this.autosaveTrigger$
  .pipe(debounceTime(800), takeUntilDestroyed(this.destroyRef))
  .subscribe(() => void this.performAutosave());
```

**Tại sao không dùng `setTimeout`**: Subject + debounceTime tự cancel pending timers khi component bị destroy (thông qua `takeUntilDestroyed`). setTimeout gây memory leak nếu component destroy trước khi timer fire.

**Tại sao 800ms**: NFR-2 yêu cầu "autosave acknowledgement within 1 second". 800ms debounce + ~100-200ms API call = ~1 second total. Không thay đổi giá trị này khi không cần.

### Angular: Autosave Status Display

Dùng `@switch` (Angular 17+ syntax), KHÔNG dùng `*ngSwitch`:
```html
@switch (autosaveStatus()) {
  @case ('saving') { ... }
  @case ('saved') { ... }
  @case ('error') { ... }
  @default { ... }
}
```

`aria-live="polite"` đảm bảo screen reader thông báo "Đã lưu" sau khi autosave thành công (AC2 accessibility requirement).

### Angular: Restore Answers On Load (AC4)

Story 4.2 đã viết `loadWorkspace` với:
```typescript
const initialAnswers: Record<number, string> = {};
for (const row of ws.answerRows) {
  if (row.answer !== null) {
    initialAnswers[row.questionNumber] = row.answer;
  }
}
this.answerInputs.set(initialAnswers);
```

Story 4.3 KHÔNG thay đổi code này. Việc restore hoạt động tự động khi `GetWorkspaceAsync` backend bây giờ trả về `answerRows` từ DB thay vì `[]`. Angular code không cần sửa.

**Xác nhận**: Sau khi implement Task 4.3 (backend populate AnswerRows), các test "load workspace có answerRows → answerInputs được khôi phục" sẽ pass.

### Angular: Import RxJS Trong Standalone Component

Verify rằng `Subject`, `debounceTime`, `takeUntilDestroyed` có sẵn. Không cần thêm module — đây là RxJS utilities được import trực tiếp. `takeUntilDestroyed` đến từ `@angular/core/rxjs-interop` (Angular 16+).

```typescript
import { Subject, debounceTime } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
```

### Angular: Error Code Constants

Thêm vào `SUBMISSION_ERROR_MESSAGES` trong `submissions.models.ts`:
```typescript
'submission.notDraft': 'Bài làm đã được nộp, không thể lưu thêm.',
```

### Files Being Created/Modified

**API (new):**
- `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionAnswer.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/AutosaveAnswersRequest.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/AutosaveAnswersResult.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionAnswerConfiguration.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/` — migration AddSubmissionAnswers
- `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsAutosaveTests.cs`

**API (update):**
- `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs` — thêm `Answers` nav property
- `src/EnglishTestWeb.Api/Application/Submissions/ISubmissionService.cs` — thêm `AutosaveAnswersAsync`
- `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs` — implement `AutosaveAnswersAsync` + update `GetWorkspaceAsync`
- `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs` — thêm `PUT /{id}/answers`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs` — thêm `DbSet<SubmissionAnswer>`
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — thêm PUT endpoint

**Angular (update):**
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions.models.ts` — thêm error code
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions-api.service.ts` — thêm `autosaveAnswers`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.ts` — autosave logic
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.html` — autosave status region
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.spec.ts` — new tests

### Architecture Compliance

- **Controller không access DbContext** — delegate hoàn toàn sang `ISubmissionService`
- **TimeProvider** — inject `TimeProvider` (không `DateTimeOffset.UtcNow` trực tiếp) trong SubmissionService
- **Student scope** — `AutosaveAnswersAsync` verify `StudentId == studentId` trước khi save
- **Idempotent autosave** — upsert semantics, gửi nhiều lần với cùng data = kết quả giống nhau
- **Lock after submit** — AC5: check `Status == Draft` trước khi upsert; trả về `409 Conflict` nếu đã Submitted
- **Error codes** — `submission.notFound`, `submission.notDraft` theo dot-notation pattern
- **204 No Content** — autosave endpoint không cần response body
- **AnswerRows populated** — `GetWorkspaceAsync` phải trả về answers đã lưu (không phải `[]`)

### Anti-Patterns

- **KHÔNG** lưu `correctAnswer` hay bất cứ thông tin AnswerKey nào trong `SubmissionAnswer` — chỉ lưu student's answer
- **KHÔNG** implement timer-based auto-submit — autosave và final submit là hai action riêng biệt
- **KHÔNG** dùng client-provided timestamps cho stale-write protection — server timestamps đủ cho MVP
- **KHÔNG** gửi autosave request per keystroke — phải debounce (800ms)
- **KHÔNG** hiển thị "Đã lưu" trước khi API confirm thành công (AC2)
- **KHÔNG** reset `autosaveStatus` về `idle` sau `saved` — giữ "Đã lưu" visible cho đến keystroke tiếp theo
- **KHÔNG** sửa `loadWorkspace` Angular code — restore hoạt động tự động qua backend change

### Context Từ Previous Stories

1. **`flushPromises()` pattern** — dùng trong tất cả Angular tests async
2. **`@if` / `@for` / `@switch`** — Angular 17+ control flow syntax
3. **`data-testid` attributes** — theo pattern story 4.1, 4.2
4. **`IHiddenResourceResponseFactory`** — inject và dùng cho `submission.notFound` 404
5. **`TimeProvider`** — inject qua constructor parameter (xem `SubmissionService` constructor hiện tại)
6. **DI registration** — tìm nơi `ISubmissionService` được register, KHÔNG cần thêm registration mới vì `SubmissionService` đã registered
7. **HttpClient pattern** — dùng `firstValueFrom(this.http.put(...))` theo pattern của `SubmissionsApiService` hiện tại
8. **`takeUntilDestroyed`** — từ `@angular/core/rxjs-interop`, cần `DestroyRef` được inject

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 4, Story 4.3]
- [Source: `_bmad-output/implementation-artifacts/4-2-reading-listening-attempt-workspace.md` — Previous story, existing patterns]
- [Source: `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs` — Entity cần extend]
- [Source: `src/EnglishTestWeb.Api/Application/Submissions/ISubmissionService.cs` — Interface cần extend]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs` — Implementation cần extend]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs` — EF config pattern]
- [Source: `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs` — Controller patterns]
- [Source: `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.ts` — Component cần extend]
- [Source: `src/EnglishTestWeb.Client/src/app/core/submissions/submissions-api.service.ts` — API service cần extend]
- [Source: `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsControllerTests.cs` — Test patterns]

### Review Findings

- [x] [Review][Patch] Duplicate QuestionNumber trong request body → unhandled DbUpdateException (500) [`SubmissionService.cs:AutosaveAnswersAsync`] — Fixed: dedup qua `GroupBy(r => r.QuestionNumber).Select(g => g.Last())` trước loop.
- [x] [Review][Patch] `request.Rows` null → NullReferenceException (500) [`SubmissionService.cs:AutosaveAnswersAsync`] — Fixed: early return `AutosaveAnswersResult(true, null)` nếu `request.Rows is null or { Count: 0 }`.
- [x] [Review][Patch] Answer > 500 ký tự → unhandled DbUpdateException (500) [`SubmissionService.cs`] — Fixed: truncate về 500 ký tự trước khi upsert (`row.Answer?.Length > 500 ? row.Answer[..500] : row.Answer`).
- [x] [Review][Defer] Concurrent PUT requests racing trên cùng QuestionNumber mới → unique index violation — deferred, MVP single-session assumption
- [x] [Review][Defer] Không có upper bound trên số lượng Rows — deferred, hardening bảo mật ngoài phạm vi MVP
- [x] [Review][Defer] QuestionNumber <= 0 không được validate — deferred, spec explicitly nói không bắt buộc validate bounds
- [x] [Review][Defer] Ownership không verify against assignment active/expired — deferred, ngoài phạm vi story
- [x] [Review][Defer] TOCTOU giữa status check và SaveChangesAsync — deferred, MVP single-session assumption

#### Round 2 Findings

- [x] [Review][Patch] Silent truncation answer > 500 ký tự vi phạm AC4 — Fixed: thêm `maxlength="500"` lên HTML input, UI không cho phép gửi > 500 ký tự từ normal path.
- [x] [Review][Defer] Surrogate pair split khi truncate chuỗi > 500 chars qua direct API — deferred, chỉ xảy ra khi bypass HTML maxlength; MCQ answers không có emoji.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

Không có issues đặc biệt. AuthorizationMatrixTests cho teacher cần `SeedDemoTemplatesAsync` trước khi sign-in (pattern đã tồn tại nhưng bị bỏ sót lần đầu).

### Completion Notes List

- ✅ SubmissionAnswer entity + EF config + migration `AddSubmissionAnswers`
- ✅ `AutosaveAnswersAsync` trong SubmissionService: verify ownership + draft status + upsert rows
- ✅ `GetWorkspaceAsync` cập nhật: populate `answerRows` từ `SubmissionAnswers` table
- ✅ Controller: `PUT /api/submissions/{id}/answers` → 204 NoContent
- ✅ 9 API tests mới (7 trong SubmissionsAutosaveTests + 2 trong AuthorizationMatrix)
- ✅ Angular: `AutosaveAnswersRow` interface, `autosaveAnswers()` method, `SUBMISSION_ERROR_MESSAGES` thêm `submission.notDraft`
- ✅ Angular component: `autosaveStatus` signal, `autosaveTrigger$` Subject + debounce 800ms, `performAutosave()`
- ✅ Template: `@switch` autosave status region với `aria-live="polite"`
- ✅ 7 tests Angular mới (autosave success/error, restore từ answerRows, submitted guard, aria-live)
- ✅ Tất cả 237 API tests + 133 Angular tests pass

### File List

**API (new):**
- `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionAnswer.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/AutosaveAnswersRequest.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/AutosaveAnswersResult.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionAnswerConfiguration.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/20260612082020_AddSubmissionAnswers.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/20260612082020_AddSubmissionAnswers.Designer.cs`
- `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsAutosaveTests.cs`

**API (update):**
- `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs` — thêm `Answers` nav property
- `src/EnglishTestWeb.Api/Application/Submissions/ISubmissionService.cs` — thêm `AutosaveAnswersAsync`
- `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs` — implement `AutosaveAnswersAsync` + update `GetWorkspaceAsync`
- `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs` — thêm `PUT /{id}/answers`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs` — thêm `DbSet<SubmissionAnswer>`
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — thêm PUT endpoint tests

**Angular (update):**
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions.models.ts` — thêm `AutosaveAnswersRow`, `submission.notDraft`
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions-api.service.ts` — thêm `autosaveAnswers`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.ts` — autosave logic
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.html` — autosave status region
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.spec.ts` — tests mới
