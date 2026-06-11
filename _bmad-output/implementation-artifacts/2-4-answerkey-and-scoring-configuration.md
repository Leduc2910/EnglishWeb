---
baseline_commit: 0c3e45d4ad9159101d616db0c4f10ed3a46aed84
---

# Story 2.4: AnswerKey And Scoring Configuration

Status: review

## Story

Là giáo viên,
tôi muốn cấu hình số câu, đáp án đúng và điểm,
để Reading/Listening có thể tự chấm từ AnswerKey ổn định.

## Acceptance Criteria

1. **Given** draft template đã có vật liệu yêu cầu
   **When** giáo viên mở `/teacher/library/{templateId}/answer-key`
   **Then** trang hiển thị Step 3/4 với số câu, scoring mode, ô nhập điểm tổng hoặc điểm từng câu, answer grid, validation summary, và save-draft action.

2. **Given** giáo viên nhập số câu không hợp lệ (ngoài 1-200 hoặc không phải số nguyên dương)
   **When** validation chạy
   **Then** hệ thống chặn continue với `ERR_QUESTION_COUNT_INVALID`.

3. **Given** giáo viên cấu hình các rows
   **When** đáp án hoặc điểm thay đổi
   **Then** validation summary cập nhật số câu còn thiếu đáp án và tổng điểm
   **And** draft được lưu (save-draft) mà không mất dữ liệu đã nhập.

4. **Given** bất kỳ answer row nào còn thiếu đáp án
   **When** giáo viên nhấn Continue
   **Then** hệ thống chặn và xác định câu hỏi bị thiếu bằng `ERR_ANSWER_MISSING`.

5. **Given** AnswerKey và dữ liệu điểm hợp lệ
   **When** giáo viên tiếp tục review
   **Then** AnswerKey rows được lưu theo cấu trúc, độc lập với nội dung trang PDF
   **And** AnswerKey draft record được tạo/cập nhật sẵn sàng cho lịch sử nộp bài sau này.

**Phạm vi skill:**
- `reading` và `listening`: CÓ answer key (Step 3/4 đầy đủ)
- `speaking`: KHÔNG có answer key — wizard từ Step 2 (materials) nhảy thẳng sang Step 4 (review); route `answer-key` cho speaking template hiển thị thông báo "không áp dụng" + redirect về review

## Tasks / Subtasks

- [x] Domain + persistence (AC: 5)
  - [x] `Domain/TestTemplates/AnswerKeyVersion.cs` — `Id`, `TemplateId`, `Status`, `ScoringMode`, `QuestionCount`, `TotalScore?`, `RowsJson`, `RowVersion` (byte[]), `CreatedAt`, `UpdatedAt`
  - [x] `Domain/TestTemplates/AnswerKeyStatuses.cs` — constants: `Draft = "draft"`, `Ready = "ready"`, `Locked = "locked"`
  - [x] `Domain/TestTemplates/ScoringModes.cs` — constants: `Equal = "equal"`, `PerQuestion = "per-question"`
  - [x] EF configuration `Infrastructure/Persistence/Configurations/AnswerKeyVersionConfiguration.cs` — FK to TestTemplates, unique index per TemplateId, `IsRowVersion()` concurrency token
  - [x] Migration `AddAnswerKeyVersioning`; DbSet<AnswerKeyVersion> vào `EnglishTestWebDbContext`
  - [x] `Domain/TestTemplates/AnswerKeyRow.cs` — sealed record `(int QuestionNumber, string CorrectAnswer, decimal? Score)` dùng cho JSON serialization

- [x] Application + API (AC: 1–5)
  - [x] `Application/TestTemplates/IAnswerKeyService.cs` — `GetAsync`, `UpsertDraftAsync`
  - [x] `Infrastructure/TestTemplates/AnswerKeyService.cs` — implement; serialize/deserialize RowsJson bằng `System.Text.Json`
  - [x] `Contracts/TestTemplates/AnswerKeyVersionResponse.cs` — response DTO
  - [x] `Contracts/TestTemplates/UpsertAnswerKeyRequest.cs` — request DTO với validation attributes
  - [x] `Controllers/AnswerKeyController.cs` — GET + PUT `/api/test-templates/{templateId}/answer-key`
    - GET: reuse `CanViewTemplateAsTeacher`; trả 404 nếu chưa có
    - PUT: reuse `CanEditTemplateAsTeacher` (draft-only); upsert
  - [x] ProblemDetails codes: `answerKey.notFound`, `answerKey.invalid.*`, `templates.notEditable`
  - [x] `Program.cs` — đăng ký `IAnswerKeyService`

- [x] API tests (AC: 2, 4, 5)
  - [x] `tests/EnglishTestWeb.Api.Tests/TestTemplates/AnswerKeyControllerTests.cs`
  - [x] `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — bổ sung answer-key rows

- [x] Angular wizard Step 3/4 (AC: 1–5)
  - [x] `features/test-template-answer-key/test-template-answer-key.component.{ts,html,css,spec.ts}`
  - [x] `app.routes.ts` — thay placeholder bằng component thật
  - [x] `core/test-templates/test-templates-api.service.ts` — thêm `getAnswerKey`, `upsertAnswerKey`
  - [x] `core/test-templates/test-templates.models.ts` — thêm types `AnswerKeyVersionResponse`, `AnswerKeyRowResponse`, `UpsertAnswerKeyRequest`
  - [x] Vitest answer-key component specs

- [x] Docs + quality
  - [x] `docs/setup/development.md` — answer key smoke test
  - [x] `.\scripts\quality.ps1` pass

## Dev Notes

### Discovery

- `epics_content`: `_bmad-output/planning-artifacts/epics.md` — Story 2.4, FR-6, UX-DR8, NFR-5.
- `architecture_content`: `_bmad-output/planning-artifacts/architecture.md` — AnswerKey versioning, `rowversion`, `AnswerKeyVersionId`, transaction boundary, audit.
- `ux_content`: `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.6-create-test-answer-key-scoring/1.6-create-test-answer-key-scoring.md`
- Previous story: `2-3-protected-testmaterial-upload-and-preview.md` (done) — wizard Step 2, `TestMaterial`/`StoredFile`, `CanEditTemplateAsTeacher`.

### Story Foundation

Story 2.4 là **AnswerKey vertical slice**: tạo/cập nhật cấu hình đáp án và điểm cho Reading/Listening draft template. Build trên wizard Step 2 từ 2.3. Story này **không** implement:
- `ready` transition của AnswerKey (→ 2.5 mark-ready)
- Auto-grading submission (→ Epic 4)
- AnswerKey re-versioning sau khi có submissions (→ Epic 4)
- Speaking answer key (không áp dụng MVP)

**Phụ thuộc (done / in repo):** Story 2.1 read surface; Story 2.2 wizard Step 1; Story 2.3 wizard Step 2, `CanEditTemplateAsTeacher`, `CanViewTemplateAsTeacher`.

### Domain Design

**Entity `AnswerKeyVersion`:**
```csharp
public sealed class AnswerKeyVersion
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string Status { get; set; } = AnswerKeyStatuses.Draft;
    public string ScoringMode { get; set; } = ScoringModes.Equal;
    public int QuestionCount { get; set; }
    public decimal? TotalScore { get; set; }          // Equal mode only
    public string RowsJson { get; set; } = "[]";      // JSON array of AnswerKeyRow
    public byte[] RowVersion { get; set; } = [];      // SQL rowversion concurrency token
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TestTemplate? Template { get; set; }
}
```

**Value object `AnswerKeyRow`** (không phải EF entity — chỉ dùng cho JSON serialization):
```csharp
public sealed record AnswerKeyRow(int QuestionNumber, string CorrectAnswer, decimal? Score);
```

**Constants:**
```csharp
// AnswerKeyStatuses
Draft   = "draft"   // story 2.4 — set khi create/update
Ready   = "ready"   // story 2.5 — set khi mark-ready
Locked  = "locked"  // epic 4 — set khi có submissions

// ScoringModes
Equal       = "equal"         // Điểm đều — TotalScore / QuestionCount
PerQuestion = "per-question"  // Điểm từng câu — mỗi row có Score
```

**Thiết kế lưu trữ — lý do chọn JSON rows:**
- MVP: 1 `AnswerKeyVersion` per `TestTemplate`. Re-versioning sẽ add `VersionNumber int` khi Epic 4 cần.
- `RowsJson` lưu `List<AnswerKeyRow>` serialized — "stored structurally" = không parse PDF, mỗi row có questionNumber + correctAnswer + score tường minh.
- `IsRowVersion()` → `rowversion` SQL Server column, architecture yêu cầu tường minh cho concurrency.
- Unique index per TemplateId đảm bảo không duplicate draft.

**EF Configuration:**
```csharp
builder.ToTable("AnswerKeyVersions");
builder.HasKey(e => e.Id);
builder.Property(e => e.RowVersion).IsRowVersion();
builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
builder.Property(e => e.ScoringMode).HasMaxLength(32).IsRequired();
builder.Property(e => e.RowsJson).HasMaxLength(-1).IsRequired(); // nvarchar(max)
builder.HasOne<TestTemplate>().WithMany()
    .HasForeignKey(e => e.TemplateId).OnDelete(DeleteBehavior.Cascade);
builder.HasIndex(e => e.TemplateId).IsUnique();
```

### API Design

**Routes (trong `AnswerKeyController`):**

```
GET  /api/test-templates/{templateId}/answer-key
     Policy: CanViewTemplateAsTeacher
     200 AnswerKeyVersionResponse | 404 answerKey.notFound
     Note: 404 là bình thường khi chưa tạo draft

PUT  /api/test-templates/{templateId}/answer-key
     Policy: CanEditTemplateAsTeacher (draft-only; 409 templates.notEditable nếu ready/archived)
     Body: UpsertAnswerKeyRequest
     200 AnswerKeyVersionResponse (upsert — create hoặc update)
```

**`UpsertAnswerKeyRequest`:**
```csharp
public sealed record UpsertAnswerKeyRequest(
    int QuestionCount,
    string ScoringMode,
    decimal? TotalScore,
    IReadOnlyList<AnswerKeyRowRequest> Rows);

public sealed record AnswerKeyRowRequest(
    int QuestionNumber,
    string CorrectAnswer,
    decimal? Score);
```

**Server validation cho PUT (format only — không block partial save):**
- `QuestionCount` ngoài [1, 200] → `400 answerKey.invalid.questionCount`
- `ScoringMode` không phải `equal`/`per-question` → `400 answerKey.invalid.scoringMode`
- `Rows.Count != QuestionCount` (nếu rows không rỗng) → `400 answerKey.invalid.rowCount`
- Row `QuestionNumber` ngoài [1, QuestionCount] hoặc trùng → `400 answerKey.invalid.rowNumber`

**Không validate completeness ở đây.** Partial save (một số rows chưa có correctAnswer) là intentional. Completeness check là ở client Continue + 2.5 mark-ready gate.

**`AnswerKeyVersionResponse`:**
```csharp
public sealed record AnswerKeyVersionResponse(
    Guid AnswerKeyVersionId,
    Guid TemplateId,
    string Status,
    string ScoringMode,
    int QuestionCount,
    decimal? TotalScore,
    IReadOnlyList<AnswerKeyRowResponse> Rows,
    DateTimeOffset UpdatedAt);

public sealed record AnswerKeyRowResponse(
    int QuestionNumber,
    string CorrectAnswer,
    decimal? Score);
```

**`AnswerKeyService.UpsertDraftAsync` logic:**
```csharp
var existing = await dbContext.AnswerKeyVersions
    .FirstOrDefaultAsync(x => x.TemplateId == templateId, ct);

var rowsJson = JsonSerializer.Serialize(
    request.Rows.Select(r => new AnswerKeyRow(r.QuestionNumber, r.CorrectAnswer, r.Score)));

if (existing is null)
{
    var entity = new AnswerKeyVersion
    {
        Id = Guid.NewGuid(),
        TemplateId = templateId,
        Status = AnswerKeyStatuses.Draft,
        ScoringMode = request.ScoringMode,
        QuestionCount = request.QuestionCount,
        TotalScore = request.TotalScore,
        RowsJson = rowsJson,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };
    dbContext.AnswerKeyVersions.Add(entity);
}
else
{
    existing.ScoringMode = request.ScoringMode;
    existing.QuestionCount = request.QuestionCount;
    existing.TotalScore = request.TotalScore;
    existing.RowsJson = rowsJson;
    existing.UpdatedAt = DateTimeOffset.UtcNow;
    // Note: Status stays "draft" here; story 2.5 sets "ready"
}
await dbContext.SaveChangesAsync(ct);
```

**ProblemDetails codes:**
- `answerKey.notFound`
- `answerKey.invalid.questionCount`
- `answerKey.invalid.scoringMode`
- `answerKey.invalid.rowCount`
- `answerKey.invalid.rowNumber`
- `answerKey.notApplicable` — cho speaking template PUT
- `templates.notEditable` — đã có từ 2.3, CanEditTemplateAsTeacher enforce

### Speaking Skill Handling

**QUAN TRỌNG — dev agent phải xử lý cả 2 sides:**

**Backend:** Speaking template PUT `/api/test-templates/{id}/answer-key` → trả `400 answerKey.notApplicable`. GET vẫn trả 404 (bình thường — speaking chưa có). `AnswerKeyService.UpsertDraftAsync` kiểm tra `template.Skill == TemplateSkill.Speaking` và throw business error.

**Frontend (materials component — story 2.3 code):** Với speaking template, nút "Tiếp tục nhập answer key" trong `test-template-materials.component.ts` cần navigate thẳng tới review route (`/teacher/library/{id}/review` — placeholder của 2.5), không tới answer-key. Check `template.skill === 'speaking'` trước khi navigate.

**Frontend (answer-key component — story 2.4 mới):** Nếu load component mà `template.skill === 'speaking'`, hiển thị info banner "Answer key không áp dụng cho kỹ năng Speaking" + button navigate về review. Không render form.

**Wizard stepper:** Step 3 hiển thị nhưng là disabled/skipped indicator cho speaking.

### Angular Component Design

**Route thực tế:** `/teacher/library/:templateId/answer-key` (thay placeholder trong `app.routes.ts`)

**Object IDs (từ UX 01.6 — phải match để E2E tests sau này tìm được):**
- `answer-key-wizard-header` / `answer-key-stepper`
- `answer-key-controls`
  - `answer-key-question-count-input`
  - `answer-key-scoring-mode-control` (segmented: "Điểm đều" / "Điểm từng câu")
  - `answer-key-total-score-input` (hiện khi Equal mode)
- `answer-key-grid`
  - `answer-key-row` × N
  - `answer-key-answer-input` (mỗi row)
  - `answer-key-score-input` (mỗi row, hiện khi PerQuestion mode)
- `answer-key-validation-summary`
  - `answer-key-missing-count`
  - `answer-key-score-total`
  - `answer-key-warning-list`
- `answer-key-footer-actions`
  - `answer-key-back-button` → `/teacher/library/{id}/materials`
  - `answer-key-save-draft-button` → PUT (partial save, không validate completeness)
  - `answer-key-continue-button` → validate completeness → `/teacher/library/{id}/review` (placeholder 2.5)

**State management:**
- Load template info (skill, status) + GET answer-key on init
- `questionCount` change → regenerate rows array; nếu giảm count và rows có data → confirm dialog trước khi trim
- `scoringMode` change → toggle totalScore vs per-row score inputs
- `saveInFlight` flag — disable form khi saving (pattern từ 2.2/2.3)

**Continue validation (client-side — chặn navigate):**
```typescript
// Chặn Continue nếu:
const errors = [];
if (questionCount < 1 || questionCount > 200 || !Number.isInteger(questionCount))
  errors.push({ code: 'ERR_QUESTION_COUNT_INVALID', message: 'Số câu phải từ 1 đến 200.' });
for (const row of rows)
  if (!row.correctAnswer?.trim())
    errors.push({ code: 'ERR_ANSWER_MISSING', message: `Câu ${row.questionNumber} chưa có đáp án.` });
if (scoringMode === 'equal' && (!totalScore || totalScore <= 0))
  errors.push({ code: 'ERR_TOTAL_SCORE_INVALID', message: 'Tổng điểm phải lớn hơn 0.' });
if (scoringMode === 'per-question')
  for (const row of rows)
    if (!row.score || row.score <= 0)
      errors.push({ code: 'ERR_ROW_SCORE_INVALID', message: `Điểm câu ${row.questionNumber} phải lớn hơn 0.` });
```

**Models (`test-templates.models.ts`):**
```typescript
export interface AnswerKeyVersionResponse {
  answerKeyVersionId: string;
  templateId: string;
  status: string;
  scoringMode: 'equal' | 'per-question';
  questionCount: number;
  totalScore: number | null;
  rows: AnswerKeyRowResponse[];
  updatedAt: string;
}

export interface AnswerKeyRowResponse {
  questionNumber: number;
  correctAnswer: string;
  score: number | null;
}

export interface UpsertAnswerKeyRequest {
  questionCount: number;
  scoringMode: 'equal' | 'per-question';
  totalScore: number | null;
  rows: { questionNumber: number; correctAnswer: string; score: number | null }[];
}
```

**Error mapping (`mapAnswerKeyApiError`):**
```typescript
// Tạo function mới, không modify mapMaterialApiError
// Dùng extensions.code KHÔNG dùng localized string làm sentinel (bug đã fix 2.3)
function mapAnswerKeyApiError(code: string): string {
  switch (code) {
    case 'answerKey.invalid.questionCount': return 'ERR_QUESTION_COUNT_INVALID';
    case 'answerKey.invalid.rowCount': return 'ERR_ROW_COUNT_MISMATCH';
    case 'answerKey.notApplicable': return 'ERR_NOT_APPLICABLE';
    case 'templates.notEditable': return 'ERR_NOT_EDITABLE';
    default: return 'ERR_UNKNOWN';
  }
}
```

### Architecture Compliance

- **Controllers → Application → Domain**: `AnswerKeyController` → `IAnswerKeyService` → `AnswerKeyVersion` entity.
- **Cookie auth + XSRF**: GET dùng cookie only; PUT cần XSRF header — Angular `credentials.interceptor.ts` + XSRF interceptor đã handle globally.
- **`CanEditTemplateAsTeacher`**: tái sử dụng policy handler đã có — enforce draft status + teacher ownership.
- **`CanViewTemplateAsTeacher`**: tái sử dụng cho GET.
- **Hidden 404**: cross-teacher access → policy handler trả 404, không 403. Đã handled bởi existing policy.
- **`IsRowVersion()`**: EF Core fluent API → `rowversion` SQL Server column + concurrency checking.
- **ProblemDetails**: stable `extensions.code`; tests assert code, không assert message text.
- **No JWT/localStorage**: cookie session only.
- **Transaction**: `UpsertDraftAsync` một transaction. Không split writes.
- **Student DTO safety**: `AnswerKeyVersionResponse` chỉ cho teacher routes. Epic 4 student DTOs **tuyệt đối không** include `correctAnswer`.

### Previous Story Intelligence (2.3)

- **`saveInFlight` guard**: áp dụng cho `onSaveDraft` trong answer-key component.
- **`mapMaterialApiError` pattern**: tạo `mapAnswerKeyApiError` tương tự — dùng `extensions.code`, KHÔNG dùng localized string làm sentinel (lỗi đã biết 2.3).
- **Unhandled error trong `onSaveDraft`** (2.3 bug đã fix): thêm `catchError` → user feedback khi PUT fail.
- **Materials "Continue" navigate**: cần sửa `test-template-materials.component.ts` — với speaking, navigate thẳng tới review (không tới answer-key).
- **Route placeholder**: `app.routes.ts` đã có `library/:templateId/answer-key` với `data.title` → giữ nguyên `teacherGuard`, chỉ thay `loadComponent` + `data.title`.
- **Review defer từ 2.3**: Triple ownership round-trips — `AnswerKeyController` không query template ownership riêng; dùng authorization handler.

### Testing Requirements

**API Tests (`AnswerKeyControllerTests.cs`):**
```
Setup: factory + AuthTestHelper.SignInUserAsync + TestTemplatesTestHelper.SeedDemoTemplatesAsync

Test cases:
- GET chưa có → 404 answerKey.notFound
- PUT valid (equal mode) → 200, response fields đầy đủ, status = "draft"
- PUT lần 2 (update) → 200, không tạo duplicate AnswerKeyVersion
- GET sau PUT → 200, data match
- PUT với questionCount = 0 → 400 answerKey.invalid.questionCount
- PUT với questionCount = 201 → 400 answerKey.invalid.questionCount
- PUT với rows.Count != questionCount → 400 answerKey.invalid.rowCount
- PUT với row.QuestionNumber trùng → 400 answerKey.invalid.rowNumber
- PUT với template ready status → 409 templates.notEditable
- PUT với speaking template → 400 answerKey.notApplicable
- GET cross-teacher → 404
- PUT cross-teacher → 404
```

**AuthorizationMatrixTests.cs — bổ sung rows:**
```
GET /api/test-templates/{id}/answer-key:
  Teacher owner: 200/404 ✓ | Other teacher: 404 ✓ | Student: 403 ✓ | Anonymous: 401 ✓
PUT /api/test-templates/{id}/answer-key:
  Teacher owner (draft): 200 ✓ | Teacher owner (ready): 409 ✓
  Other teacher: 404 ✓ | Student: 403 ✓ | Anonymous: 401 ✓
```

**Angular Vitest:**
- Mock HTTP GET/PUT
- Verify Continue button disabled khi có missing answers
- Verify Continue button disabled khi invalid questionCount
- Verify scoring mode toggle (totalScore input hiện/ẩn)
- Verify questionCount change regenerates rows
- Verify speaking template shows not-applicable state

**Regression:** 2.1, 2.2, 2.3 tests phải pass.

### File Structure Requirements

**API (new):**
- `Domain/TestTemplates/AnswerKeyVersion.cs`
- `Domain/TestTemplates/AnswerKeyStatuses.cs`
- `Domain/TestTemplates/ScoringModes.cs`
- `Domain/TestTemplates/AnswerKeyRow.cs`
- `Application/TestTemplates/IAnswerKeyService.cs`
- `Infrastructure/TestTemplates/AnswerKeyService.cs`
- `Contracts/TestTemplates/AnswerKeyVersionResponse.cs`
- `Contracts/TestTemplates/UpsertAnswerKeyRequest.cs`
- `Controllers/AnswerKeyController.cs`
- `Infrastructure/Persistence/Configurations/AnswerKeyVersionConfiguration.cs`
- Migration: `Infrastructure/Persistence/Migrations/*AddAnswerKeyVersioning*`

**API (modify):**
- `Infrastructure/Persistence/EnglishTestWebDbContext.cs` — `DbSet<AnswerKeyVersion> AnswerKeyVersions`
- `Program.cs` — `builder.Services.AddScoped<IAnswerKeyService, AnswerKeyService>()`

**Client (new):**
- `features/test-template-answer-key/test-template-answer-key.component.{ts,html,css,spec.ts}`

**Client (modify):**
- `app.routes.ts` — thay placeholder answer-key loadComponent
- `core/test-templates/test-templates-api.service.ts` — `getAnswerKey(templateId)`, `upsertAnswerKey(templateId, request)`
- `core/test-templates/test-templates.models.ts` — thêm AnswerKey interfaces
- `features/test-template-materials/test-template-materials.component.ts` — fix Continue navigate cho speaking

**Tests (new/extend):**
- `tests/EnglishTestWeb.Api.Tests/TestTemplates/AnswerKeyControllerTests.cs`
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`

### Anti-Patterns

- **Không** lưu AnswerKey rows trong `TestTemplate` entity — `AnswerKeyVersion` là entity riêng.
- **Không** tạo `AnswerKeyRow` làm EF entity có bảng riêng — rows là JSON trong `AnswerKeyVersion`.
- **Không** parse PDF content từ materials.
- **Không** validate completeness (thiếu đáp án) tại PUT — partial save là intentional.
- **Không** query template ownership riêng trong `AnswerKeyService` — policy handler đã check.
- **Không** expose `correctAnswer` trong student-facing DTOs (kể cả response errors).
- **Không** bỏ qua `IsRowVersion()` cho AnswerKeyVersion — architecture yêu cầu.
- **Không** dùng localized string làm error sentinel — dùng `extensions.code`.
- **Không** tái sử dụng `mapMaterialApiError` cho answer-key errors — tạo function riêng.

### Latest Tech Information

- **EF Core 10**: `builder.Property(e => e.RowVersion).IsRowVersion()` — fluent API đồng nhất với file configuration hiện tại (không dùng `[Timestamp]` attribute).
- **`System.Text.Json`**: built-in trong ASP.NET Core 10. Dùng `JsonSerializer.Serialize` / `Deserialize<List<AnswerKeyRow>>`.
- **Migration**: tên `AddAnswerKeyVersioning` theo convention `EF Core migration names are intent-based` trong architecture.
- **Concurrency**: `DbUpdateConcurrencyException` khi rowversion conflict → `409 Conflict` với code `answerKey.concurrencyConflict` (low probability trong MVP nhưng cần handle không để 500).
- **`HasMaxLength(-1)`** trong EF Core fluent API → `nvarchar(max)` cho RowsJson.

### Project Context Reference

- [CLAUDE.md] — stack, ProblemDetails, no JWT, quality script, cookie auth + XSRF.
- [architecture.md] — `AnswerKeyVersion` naming, rowversion, transaction boundary, student DTO safety, migration naming.
- [docs/setup/development.md] — dev teacher account.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Story 2.4]
- [Source: `_bmad-output/C-UX-Scenarios/.../1.6-create-test-answer-key-scoring.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — AnswerKey versioning, naming]
- [Source: `2-3-protected-testmaterial-upload-and-preview.md` — previous story learnings]

## Dev Agent Record

### Agent Model Used

Claude Fable 5 (claude-fable-5)

### Debug Log References

- `dotnet ef migrations add AddAnswerKeyVersioning` — sinh migration với `rowversion`, unique index `TemplateId`, cascade FK; verify Up/Down đúng spec.
- `dotnet test --filter FullyQualifiedName~AnswerKey` — 23/23 pass (15 controller tests + 8 authorization matrix rows).
- `dotnet test` full suite — 124/124 pass (baseline 101, +23 mới, không regression).
- `npm test` — 69/69 pass (baseline 58, +11 specs answer-key component).
- `.\scripts\quality.ps1` — passed (SDK 10.0.202, Node v22.22.3, build + toàn bộ tests).

### Completion Notes List

- **Implementation Plan:** Domain entity `AnswerKeyVersion` (1-per-template, JSON rows, `rowversion` concurrency) → `IAnswerKeyService`/`AnswerKeyService` (format-only validation, partial save intentional) → `AnswerKeyController` GET/PUT tái sử dụng `CanViewTemplateAsTeacher`/`CanEditTemplateAsTeacher` → Angular Step 3/4 component với signals + computed validation summary.
- Controller chỉ gọi policy handler 1 lần cho authorize (tránh triple ownership round-trips — review defer từ 2.3); chỉ re-query decision khi denial để audit.
- Service check thứ tự: template tồn tại (404) → speaking (400 `answerKey.notApplicable`) → draft status (409 `templates.notEditable`) → format validation (400 `answerKey.invalid.*`).
- Completeness (thiếu đáp án) KHÔNG validate ở PUT — client Continue chặn bằng `ERR_ANSWER_MISSING`/`ERR_QUESTION_COUNT_INVALID`/`ERR_TOTAL_SCORE_INVALID`/`ERR_ROW_SCORE_INVALID`; mark-ready gate thuộc story 2.5.
- `DbUpdateConcurrencyException` → 409 `answerKey.concurrencyConflict` (không leak 500).
- `mapAnswerKeyApiError` tạo riêng, dùng `extensions.code` làm sentinel (đúng learning 2.3, không dùng localized string).
- `saveInFlight` guard + `catchError` feedback trong `onSaveDraft` (pattern 2.2/2.3).
- Speaking handling đủ 2 phía: materials Continue navigate thẳng `/review` (label đổi "Tiếp tục sang Review"); answer-key route hiển thị banner "không áp dụng" + nút sang Review, không render form, không gọi GET answer-key.
- Route `/teacher/library/:templateId/review` thêm placeholder (Story 2.5) để Continue/speaking flow có đích navigate.
- Giảm số câu khi rows có dữ liệu → confirm dialog trước khi trim (UX spec 01.6).
- Object IDs theo UX 01.6: `answer-key-wizard-header/stepper/controls/grid/validation-summary/footer-actions` + per-element IDs để E2E tìm được.

### File List

**API (new):**
- `src/EnglishTestWeb.Api/Domain/TestTemplates/AnswerKeyVersion.cs`
- `src/EnglishTestWeb.Api/Domain/TestTemplates/AnswerKeyStatuses.cs`
- `src/EnglishTestWeb.Api/Domain/TestTemplates/ScoringModes.cs`
- `src/EnglishTestWeb.Api/Domain/TestTemplates/AnswerKeyRow.cs`
- `src/EnglishTestWeb.Api/Application/TestTemplates/IAnswerKeyService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/TestTemplates/AnswerKeyService.cs`
- `src/EnglishTestWeb.Api/Contracts/TestTemplates/AnswerKeyVersionResponse.cs`
- `src/EnglishTestWeb.Api/Contracts/TestTemplates/UpsertAnswerKeyRequest.cs`
- `src/EnglishTestWeb.Api/Controllers/AnswerKeyController.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/AnswerKeyVersionConfiguration.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/20260611062854_AddAnswerKeyVersioning.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/20260611062854_AddAnswerKeyVersioning.Designer.cs`

**API (modified):**
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs` — DbSet AnswerKeyVersions
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/EnglishTestWebDbContextModelSnapshot.cs` — model snapshot (generated)
- `src/EnglishTestWeb.Api/Program.cs` — đăng ký IAnswerKeyService

**Client (new):**
- `src/EnglishTestWeb.Client/src/app/features/test-template-answer-key/test-template-answer-key.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/test-template-answer-key/test-template-answer-key.component.html`
- `src/EnglishTestWeb.Client/src/app/features/test-template-answer-key/test-template-answer-key.component.css`
- `src/EnglishTestWeb.Client/src/app/features/test-template-answer-key/test-template-answer-key.component.spec.ts`

**Client (modified):**
- `src/EnglishTestWeb.Client/src/app/app.routes.ts` — answer-key component thật + review placeholder route
- `src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates-api.service.ts` — getAnswerKey, upsertAnswerKey
- `src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates.models.ts` — AnswerKey types, error messages, mapAnswerKeyApiError
- `src/EnglishTestWeb.Client/src/app/features/test-template-materials/test-template-materials.component.ts` — speaking Continue → review, continueLabel
- `src/EnglishTestWeb.Client/src/app/features/test-template-materials/test-template-materials.component.html` — continueLabel binding

**Tests (new/modified):**
- `tests/EnglishTestWeb.Api.Tests/TestTemplates/AnswerKeyControllerTests.cs` (new)
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — 8 answer-key matrix rows
- `tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplatesTestHelper.cs` — EnsureSpeakingDraftTemplateAsync

**Docs (modified):**
- `docs/setup/development.md` — Story 2.4 answer key smoke test + API endpoints

## Change Log

- 2026-06-11: Story 2.4 created từ epics + UX 01.6 + architecture + 2.3 learnings; ready-for-dev.
- 2026-06-11: Implemented AnswerKey vertical slice — domain `AnswerKeyVersion` + migration, GET/PUT answer-key API với format validation + speaking/notEditable guards, Angular wizard Step 3/4 với answer grid + validation summary + speaking not-applicable state. 23 API tests + 11 Vitest specs mới; full quality gate pass. Status → review.
