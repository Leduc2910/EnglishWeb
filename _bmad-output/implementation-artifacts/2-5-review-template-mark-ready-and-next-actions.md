---
baseline_commit: 9e6c2f1
---

# Story 2.5: Review Template, Mark Ready, And Next Actions

Status: done

## Story

Là giáo viên,
tôi muốn review cấu hình đề gốc và đánh dấu sẵn sàng sử dụng,
để tự tin dùng cùng một đề gốc cho Homework hoặc Thi trực tiếp.

## Acceptance Criteria

1. **Given** setup, tài liệu và AnswerKey hoàn tất
   **When** giáo viên mở `/teacher/library/{templateId}/review`
   **Then** trang hiển thị Step 4/4, card thông tin cơ bản, card tài liệu, card AnswerKey/điểm (chỉ với reading/listening), readiness checklist, warnings, và nút "Đánh dấu sẵn sàng".

2. **Given** bất kỳ điều kiện readiness check nào thất bại
   **When** giáo viên nhấn "Đánh dấu sẵn sàng"
   **Then** hệ thống focus vào issue đầu tiên bị block
   **And** template vẫn ở trạng thái Draft.

3. **Given** tất cả checks pass
   **When** giáo viên xác nhận "Đánh dấu sẵn sàng"
   **Then** template status thay đổi thành Ready đúng một lần
   **And** double-click hoặc retry trả về cùng kết quả thay vì tạo duplicate transition.

4. **Given** template đã Ready
   **When** success state hiển thị
   **Then** hiển thị "Giao homework" và "Tạo phiên thi trực tiếp" là 2 next action riêng biệt
   **And** không có class/deadline/session timing được lưu trực tiếp vào template.

5. **Given** Ready transition thành công
   **When** audit records được kiểm tra
   **Then** actor, previous state, next state, template id, và timestamp được ghi lại.

## Tasks / Subtasks

- [x] API mark-ready endpoint (AC: 2, 3, 5)
  - [x] Thêm `MarkReadyResult` record vào `Application/TestTemplates/ITestTemplateService.cs`
  - [x] Thêm `MarkReadyAsync(Guid templateId, string teacherId, CancellationToken)` vào `ITestTemplateService`
  - [x] Implement `MarkReadyAsync` trong `Infrastructure/TestTemplates/TestTemplateService.cs`:
    - Load template (+ materials count + answerKey) trong ít DB round-trips nhất
    - Validate readiness (xem Dev Notes → Readiness Checks)
    - Idempotent: nếu đã Ready → trả 200 ngay không thay đổi gì
    - Transition: `template.Status = "ready"`, `answerKey.Status = "ready"` (reading/listening), `template.UpdatedAt = now`
    - Structured log audit event
    - Return `MarkReadyResult(true, MapDetail(template), null, 200)`
  - [x] Thêm controller action `POST /api/test-templates/{id}/mark-ready` vào `TestTemplatesController.cs`
    - Policy: `CanViewTemplateAsTeacher` (không phải `CanEditTemplateAsTeacher` vì mark-ready IS the draft→ready transition)
  - [x] ProblemDetails codes: `review.missingRequiredMaterial`, `review.answerKeyIncomplete`, `review.scoringInvalid`, `review.templateInfoMissing`, `templates.archived`

- [x] API tests (AC: 2, 3, 5)
  - [x] `tests/EnglishTestWeb.Api.Tests/TestTemplates/MarkReadyControllerTests.cs` (new)
  - [x] `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — bổ sung mark-ready rows
  - [x] Thêm helpers vào `TestTemplatesTestHelper.cs`:
    - `EnsureDraftWithMaterialsAsync` — tạo draft template + PDF TestMaterial
    - `EnsureDraftWithCompleteAnswerKeyAsync` — tạo draft + material + complete AnswerKey

- [x] Angular review component (AC: 1–4)
  - [x] `features/test-template-review/test-template-review.component.{ts,html,css,spec.ts}` (new)
  - [x] `app.routes.ts`:
    - Thay placeholder `library/:templateId/review` bằng component thật
    - Thêm placeholder `homework/new` (Story 3.1)
    - Thêm placeholder `live-exams/new` (Story 3.2)
  - [x] `core/test-templates/test-templates-api.service.ts` — thêm `markReady(templateId)`
  - [x] `core/test-templates/test-templates.models.ts` — thêm `MarkReadyErrorCode` type + error messages + `mapMarkReadyError`
  - [x] Vitest review component specs

- [x] Docs + quality
  - [x] `.\scripts\quality.ps1` pass

## Dev Notes

### Discovery

- `epics_content`: `_bmad-output/planning-artifacts/epics.md` — Story 2.5, FR-7, UX-DR9, NFR-5, NFR-7.
- `ux_content`: `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.7-create-test-review-publish/1.7-create-test-review-publish.md`
- `architecture_content`: `_bmad-output/planning-artifacts/architecture.md` — idempotency, audit, state machines, `409 Conflict`.
- Previous story: `2-4-answerkey-and-scoring-configuration.md` (done) — `AnswerKeyVersion`, `AnswerKeyStatuses`, pattern `CanViewTemplateAsTeacher` / `CanEditTemplateAsTeacher`.

### Story Foundation

Story 2.5 là **mark-ready vertical slice**: trang review đề + transition `draft → ready` cho `TestTemplate` và `AnswerKeyVersion`. Build trên wizard Steps 1-3 từ 2.1-2.4. Story này **không** implement:
- Homework creation (→ Story 3.1, navigate placeholder)
- Live Exam creation (→ Story 3.2, navigate placeholder)
- AnswerKey re-versioning sau khi có submissions (→ Epic 4)
- Archive template action (post-MVP)
- Template duplication (post-MVP)

**Phụ thuộc (done / in repo):**
- Story 2.1: library list, `TestTemplateDetailResponse`, `CanViewTemplateAsTeacher`
- Story 2.2: wizard Step 1, `TestTemplateSetupResponse`, `CanEditTemplateAsTeacher`
- Story 2.3: wizard Step 2, `TestMaterial`, `StoredFile`, `MaterialRoles`, `GET /api/test-templates/{id}/materials`
- Story 2.4: wizard Step 3, `AnswerKeyVersion`, `AnswerKeyStatuses`, `ScoringModes`, `GET /api/test-templates/{id}/answer-key`

### API Design

**New endpoint:**

```
POST /api/test-templates/{templateId}/mark-ready
     Policy: CanViewTemplateAsTeacher
     Body: {} (empty — no body needed)
     200 TestTemplateDetailResponse (updated template, status = "ready")
     404 templates.notFound (ownership hidden)
     400 review.templateInfoMissing
     400 review.missingRequiredMaterial
     400 review.answerKeyIncomplete
     400 review.scoringInvalid
     409 templates.archived (can't mark-ready an archived template)
```

**Idempotency:** Nếu template đã `status = "ready"` khi nhận POST mark-ready:
- KHÔNG tạo duplicate transition
- KHÔNG thay đổi gì
- Trả `200 TestTemplateDetailResponse` với state hiện tại

**Idempotency với `status = "archived"`:**
- Trả `409 Conflict` với code `templates.archived`

**`MarkReadyResult` record (thêm vào `ITestTemplateService.cs`):**
```csharp
public sealed record MarkReadyResult(
    bool Succeeded,
    TestTemplateDetailResponse? Response,
    string? ErrorCode,
    int StatusCode);
```

**Interface extension (`ITestTemplateService.cs`):**
```csharp
Task<MarkReadyResult> MarkReadyAsync(
    Guid templateId,
    string teacherId,
    CancellationToken cancellationToken = default);
```

### Readiness Checks (Server-Side)

Thứ tự check trong `MarkReadyAsync`:

**1. Ownership + existence** (do policy handler handle, nhưng service cũng load và verify):
- Template không tồn tại / không thuộc teacher → 404 (handled bởi policy, không cần duplicate trong service nếu đã authorize trước)

**2. Archived check:**
```csharp
if (template.Status == TemplateStatuses.Archived)
    return MarkReadyResult(false, null, "templates.archived", 409);
```

**3. Idempotent ready check:**
```csharp
if (template.Status == TemplateStatuses.Ready)
    return MarkReadyResult(true, MapDetail(template), null, 200);
```

**4. Template info check** (defensive — luôn có cho draft nhưng cần verify):
```csharp
if (string.IsNullOrWhiteSpace(template.Title) || string.IsNullOrWhiteSpace(template.Skill))
    return MarkReadyResult(false, null, "review.templateInfoMissing", 400);
```

**5. Material check:**
```csharp
var isReading = template.Skill == TemplateSkill.Reading;
var isListening = template.Skill == TemplateSkill.Listening;
var isSpeaking = template.Skill == TemplateSkill.Speaking;

bool hasPdf = await dbContext.TestMaterials
    .AnyAsync(m => m.TemplateId == templateId && m.Role == MaterialRoles.Pdf && m.IsActive, ct);

bool hasMaterial = await dbContext.TestMaterials
    .AnyAsync(m => m.TemplateId == templateId && m.IsActive, ct);

if ((isReading || isListening) && !hasPdf)
    return MarkReadyResult(false, null, "review.missingRequiredMaterial", 400);

if (isSpeaking && !hasMaterial)
    return MarkReadyResult(false, null, "review.missingRequiredMaterial", 400);
```

**6. AnswerKey check (reading/listening only):**
```csharp
if (isReading || isListening)
{
    var answerKey = await dbContext.AnswerKeyVersions
        .FirstOrDefaultAsync(a => a.TemplateId == templateId, ct);

    if (answerKey is null || answerKey.QuestionCount < 1)
        return MarkReadyResult(false, null, "review.answerKeyIncomplete", 400);

    var rows = JsonSerializer.Deserialize<List<AnswerKeyRow>>(answerKey.RowsJson, ...) ?? [];
    if (rows.Count != answerKey.QuestionCount || rows.Any(r => string.IsNullOrWhiteSpace(r.CorrectAnswer)))
        return MarkReadyResult(false, null, "review.answerKeyIncomplete", 400);

    // Scoring check
    if (answerKey.ScoringMode == ScoringModes.Equal && (answerKey.TotalScore is null || answerKey.TotalScore <= 0))
        return MarkReadyResult(false, null, "review.scoringInvalid", 400);

    if (answerKey.ScoringMode == ScoringModes.PerQuestion && rows.Any(r => r.Score is null || r.Score <= 0))
        return MarkReadyResult(false, null, "review.scoringInvalid", 400);
}
```

**7. Transition (trong một `SaveChangesAsync`):**
```csharp
var now = DateTimeOffset.UtcNow;
template.Status = TemplateStatuses.Ready;
template.UpdatedAt = now;

if ((isReading || isListening) && answerKey is not null)
    answerKey.Status = AnswerKeyStatuses.Ready;

// Audit log
logger.LogInformation(
    "TemplateMarkedReady: templateId={TemplateId} teacherId={TeacherId} previousStatus=draft newStatus=ready at={Timestamp}",
    templateId, teacherId, now);

await dbContext.SaveChangesAsync(ct);
return MarkReadyResult(true, MapDetail(template), null, 200);
```

**Controller action:**
```csharp
[Authorize(Roles = IdentityRoleNames.Teacher)]
[HttpPost("{id:guid}/mark-ready")]
public async Task<ActionResult> MarkReady(Guid id, CancellationToken cancellationToken)
{
    var teacherId = currentUserContext.UserId;
    if (string.IsNullOrWhiteSpace(teacherId))
        return hiddenResourceResponseFactory.FromCode(401, "auth.unauthorized", ...);

    // Use CanViewTemplateAsTeacher (not CanEdit) — mark-ready IS the draft→ready transition
    var authResult = await authorizationService.AuthorizeAsync(User, id, AuthorizationPolicies.CanViewTemplateAsTeacher);
    if (!authResult.Succeeded)
        return await HiddenTemplateResponseAsync(id, teacherId, cancellationToken);

    var result = await testTemplateService.MarkReadyAsync(id, teacherId, cancellationToken);
    if (!result.Succeeded || result.Response is null)
    {
        if (result.ErrorCode == "templates.notFound")
            return await HiddenTemplateResponseAsync(id, teacherId, cancellationToken);

        return hiddenResourceResponseFactory.FromCode(
            result.StatusCode,
            result.ErrorCode ?? "review.markReadyFailed",
            "Mark ready failed.",
            "The template could not be marked ready.");
    }
    return Ok(result.Response);
}
```

**QUAN TRỌNG về `HiddenTemplateResponseAsync`**: `TestTemplatesController` hiện tại không có helper method này như `AnswerKeyController`. Cần tái sử dụng cùng pattern — inject `ITemplateAuthorizationService` + `AuthorizationDenialAuditor` (đã có sẵn trong constructor) và gọi `RequireTeacherTemplateAccessAsync` để lấy decision.

### Angular Component Design

**Route:** `/teacher/library/:templateId/review` (thay placeholder)

**Component:** `test-template-review.component.ts`

**Object IDs (từ UX 01.7):**
- `review-publish-wizard-header` / `review-publish-stepper`
- `review-publish-review-cards`
  - `review-publish-basic-info-card` + edit link → `/teacher/library/{id}/setup`
  - `review-publish-material-card` + preview link + edit link → `/teacher/library/{id}/materials`
  - `review-publish-answer-key-card` (chỉ reading/listening) + edit link → `/teacher/library/{id}/answer-key`
- `review-publish-readiness-panel`
  - `review-publish-checklist` (checkmarks per item)
  - `review-publish-warning-list`
- `review-publish-footer-actions`
  - `review-publish-back-button` → navigate theo skill:
    - reading/listening → `/teacher/library/{id}/answer-key`
    - speaking → `/teacher/library/{id}/materials`
  - `review-publish-save-draft-button` (ẩn khi đã Ready)
  - `review-publish-button` → open confirm modal / focus first blocking issue
- `review-publish-confirmation`
  - `review-publish-confirm-modal`
  - `review-publish-success-banner` (hiện sau mark-ready success)
  - `review-create-homework-button` → navigate `/teacher/homework/new?templateId={id}`
  - `review-create-live-exam-button` → navigate `/teacher/live-exams/new?templateId={id}`

**State loading:**
```typescript
// Load trong parallel on init
ngOnInit() {
  const templateId = this.route.snapshot.params['templateId'];
  forkJoin({
    template: this.testTemplatesApi.getById(templateId),
    materials: this.testTemplatesApi.getMaterials(templateId),
    answerKey: this.testTemplatesApi.getAnswerKey(templateId).pipe(catchError(() => of(null)))
  }).subscribe(...);
}
```

**Computed readiness checks (client-side — hiển thị checklist):**
```typescript
// Client-side checks (mirror server checks — giúp user biết vấn đề trước khi submit)
get readinessChecks() {
  const checks = [];
  checks.push({
    id: 'template-info',
    label: 'Thông tin đề',
    pass: !!this.template()?.title && !!this.template()?.skill,
    error: 'ERR_TEMPLATE_INFO_MISSING'
  });
  const hasPdf = this.materials()?.some(m => m.role === 'pdf' && m.isActive);
  const hasAnyMaterial = this.materials()?.some(m => m.isActive);
  const isReadingListening = ['reading', 'listening'].includes(this.template()?.skill ?? '');
  const isSpeaking = this.template()?.skill === 'speaking';
  checks.push({
    id: 'material',
    label: isReadingListening ? 'File PDF đề bài' : 'Tài liệu',
    pass: isReadingListening ? !!hasPdf : !!hasAnyMaterial,
    error: 'ERR_REVIEW_PDF_MISSING'
  });
  if (isReadingListening) {
    const ak = this.answerKey();
    const allAnswered = ak && ak.rows.length === ak.questionCount && ak.rows.every(r => !!r.correctAnswer?.trim());
    checks.push({
      id: 'answer-key',
      label: 'Answer key hoàn tất',
      pass: !!allAnswered,
      error: 'ERR_REVIEW_ANSWER_KEY_INCOMPLETE'
    });
    const scoringValid = ak && (
      (ak.scoringMode === 'equal' && (ak.totalScore ?? 0) > 0) ||
      (ak.scoringMode === 'per-question' && ak.rows.every(r => (r.score ?? 0) > 0))
    );
    checks.push({
      id: 'scoring',
      label: 'Cấu hình điểm hợp lệ',
      pass: !!scoringValid,
      error: 'ERR_REVIEW_SCORE_INVALID'
    });
  }
  return checks;
}

get allChecksPassed() {
  return this.readinessChecks.every(c => c.pass);
}
```

**Mark-ready flow:**
```typescript
onMarkReady() {
  if (!this.allChecksPassed) {
    // Focus first failing check (scroll into view + announce)
    const first = this.readinessChecks.find(c => !c.pass);
    document.getElementById(`check-${first?.id}`)?.scrollIntoView({ behavior: 'smooth' });
    return;
  }
  this.showConfirmModal.set(true);
}

onConfirmMarkReady() {
  this.markReadyInFlight.set(true);
  this.testTemplatesApi.markReady(this.templateId).pipe(
    catchError(err => { ... })
  ).subscribe(result => {
    this.markReadyInFlight.set(false);
    this.showConfirmModal.set(false);
    this.template.set(result);     // update to returned state
    this.showSuccessBanner.set(true);
  });
}
```

**Error handling (`mapMarkReadyError`):**
```typescript
function mapMarkReadyError(code: string): string {
  switch (code) {
    case 'review.templateInfoMissing': return 'ERR_TEMPLATE_INFO_MISSING';
    case 'review.missingRequiredMaterial': return 'ERR_REVIEW_PDF_MISSING';
    case 'review.answerKeyIncomplete': return 'ERR_REVIEW_ANSWER_KEY_INCOMPLETE';
    case 'review.scoringInvalid': return 'ERR_REVIEW_SCORE_INVALID';
    case 'templates.archived': return 'ERR_TEMPLATE_ARCHIVED';
    default: return 'ERR_TEMPLATE_READY_FAILED';
  }
}
```

**API service thêm:**
```typescript
markReady(templateId: string): Observable<TestTemplateDetailResponse> {
  return this.http.post<TestTemplateDetailResponse>(
    `/api/test-templates/${templateId}/mark-ready`,
    {}
  );
}
```

**App routes — thay đổi:**
```typescript
// Thay placeholder review
{
  path: 'library/:templateId/review',
  loadComponent: () =>
    import('./features/test-template-review/test-template-review.component')
      .then(m => m.TestTemplateReviewComponent),
},
// Thêm placeholder homework/live-exam (Story 3.x)
{
  path: 'homework/new',
  loadComponent: () =>
    import('./features/teacher-placeholder/teacher-placeholder.component')
      .then(m => m.TeacherPlaceholderComponent),
  data: { title: 'Giao Homework', description: 'Story 3.1 sẽ triển khai Homework creation.' },
},
{
  path: 'live-exams/new',
  loadComponent: () =>
    import('./features/teacher-placeholder/teacher-placeholder.component')
      .then(m => m.TeacherPlaceholderComponent),
  data: { title: 'Tạo Phiên Thi Trực Tiếp', description: 'Story 3.2 sẽ triển khai Live Exam creation.' },
},
```

### Architecture Compliance

- **Policy choice**: Dùng `CanViewTemplateAsTeacher` (không phải `CanEditTemplateAsTeacher`) vì `CanEditTemplateAsTeacher` enforce draft status — mark-ready IS the draft→ready transition; nếu dùng sẽ bị block cho Ready template và vô hiệu hoá idempotency.
- **Transaction**: Một `SaveChangesAsync` cho cả template.Status + answerKey.Status — atomic.
- **Idempotency**: Already-ready → return 200 (không phải 409); already-archived → 409.
- **No client data in mark-ready body**: POST body rỗng. Readiness validated hoàn toàn server-side từ DB state.
- **Cookie auth + XSRF**: POST cần XSRF header — đã được Angular interceptors handle globally.
- **Hidden 404**: cross-teacher access → policy + HiddenResourceResponseFactory → 404, không 403.
- **Audit**: structured log event qua `ILogger<TestTemplateService>` — không tạo DB audit table trong story này (consistent với current codebase: chưa có domain audit table).
- **Student DTO safety**: `TestTemplateDetailResponse` không expose `correctAnswer`; AnswerKey reading chỉ tại teacher routes.
- **ProblemDetails**: stable `extensions.code`; tests assert code, không assert message text.

### Previous Story Intelligence (2.4)

- **`CanViewTemplateAsTeacher` cho GET nhưng cần `CanViewTemplateAsTeacher` cho mark-ready POST**: khác 2.4 dùng `CanEditTemplateAsTeacher` cho PUT answer-key vì đó là edit; mark-ready là state transition mới.
- **`HiddenTemplateResponseAsync` helper pattern**: `AnswerKeyController` tách helper riêng — `TestTemplatesController` đã có `denialAuditor` và `templateAuthorizationService` trong constructor nhưng không có private helper method; cần thêm private `HiddenTemplateResponseAsync` tương tự pattern 2.4 controller code để tránh duplicate logic.
- **`saveInFlight` guard + `catchError`**: apply cho `onConfirmMarkReady` — pattern từ 2.2/2.3/2.4.
- **Error mapping bằng `extensions.code`**: tạo `mapMarkReadyError` riêng, không reuse functions của các component khác.
- **Speaking skip**: answer-key card và scoring check không render cho speaking — consistent với how 2.4 handled speaking.
- **`forkJoin` load**: load template + materials + answerKey song song — tránh waterfall load.

### Testing Requirements

**API Tests (`MarkReadyControllerTests.cs`):**
```
Setup: factory + AuthTestHelper.SignInUserAsync + custom helpers seed draft template with materials + answerKey

Test cases:
- MarkReady_DraftWithAllChecks_ReturnsReady (200, status="ready")
- MarkReady_AlreadyReady_ReturnsOk_Idempotent (200, không tạo duplicate)
- MarkReady_Archived_Returns409 (409 templates.archived)
- MarkReady_MissingPdf_Returns400 (400 review.missingRequiredMaterial)
- MarkReady_MissingAnswerKey_Returns400 (400 review.answerKeyIncomplete)
- MarkReady_IncompleteAnswerRows_Returns400 (400 review.answerKeyIncomplete)
- MarkReady_InvalidScoring_Returns400 (400 review.scoringInvalid)
- MarkReady_SpeakingWithMaterial_ReturnsReady (speaking template, no answerKey needed)
- MarkReady_SpeakingNoMaterial_Returns400 (400 review.missingRequiredMaterial)
- MarkReady_CrossTeacher_Returns404
- MarkReady_Anonymous_Returns401
- MarkReady_Student_Returns403
```

**TestTemplatesTestHelper thêm:**
```csharp
// Tạo draft template + active PDF material (không có AnswerKey)
internal static async Task<Guid> EnsureDraftWithMaterialsAsync(TestApiFactory factory, string skill = "reading")

// Tạo draft template + PDF material + complete AnswerKey (reading/listening)
internal static async Task<Guid> EnsureDraftWithCompleteAnswerKeyAsync(TestApiFactory factory)

// Tạo archived template
internal static async Task<Guid> EnsureArchivedTemplateAsync(TestApiFactory factory)
```

**AuthorizationMatrixTests — bổ sung rows:**
```
POST /api/test-templates/{id}/mark-ready:
  Teacher owner (draft, ready): 200 ✓ | Teacher owner (archived): 409 ✓
  Other teacher: 404 ✓ | Student: 403 ✓ | Anonymous: 401 ✓
```

**Angular Vitest specs:**
- Load và hiển thị review cards (basic info, material, answerKey cho reading)
- Checklist pass khi đủ điều kiện
- Checklist fail khi thiếu PDF (readinessChecks có 'material' = false)
- Mark-ready button disabled khi checklist fail
- Mark-ready button click → confirm modal
- Cancel modal → không gọi API
- Confirm → gọi `markReady()` → success banner hiện, homework + live-exam buttons hiện
- Speaking template: không hiện answerKey card
- `mapMarkReadyError` mapping chính xác

**Regression:** 2.1, 2.2, 2.3, 2.4 tests phải pass.

### File Structure Requirements

**API (modify — KHÔNG tạo file mới cho domain/application ngoài record types):**
- `Application/TestTemplates/ITestTemplateService.cs` — thêm `MarkReadyResult` record + `MarkReadyAsync` method
- `Infrastructure/TestTemplates/TestTemplateService.cs` — implement `MarkReadyAsync`
- `Controllers/TestTemplatesController.cs` — thêm `MarkReady` action + private helper

**Client (new):**
- `features/test-template-review/test-template-review.component.ts`
- `features/test-template-review/test-template-review.component.html`
- `features/test-template-review/test-template-review.component.css`
- `features/test-template-review/test-template-review.component.spec.ts`

**Client (modify):**
- `app.routes.ts` — thay review placeholder + thêm homework/live-exam placeholders
- `core/test-templates/test-templates-api.service.ts` — thêm `markReady(templateId)`
- `core/test-templates/test-templates.models.ts` — thêm `MarkReadyErrorCode` type, error messages, `mapMarkReadyError`

**Tests (new/extend):**
- `tests/EnglishTestWeb.Api.Tests/TestTemplates/MarkReadyControllerTests.cs` (new)
- `tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplatesTestHelper.cs` — thêm helpers
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — mark-ready matrix rows

**Docs (modify):**
- `docs/setup/development.md` — Story 2.5 smoke test (mark-ready endpoint)

### Anti-Patterns

- **Không** dùng `CanEditTemplateAsTeacher` cho mark-ready — nó enforce draft status, sẽ block idempotent ready case.
- **Không** accept class/deadline/session fields trong mark-ready request body — những thứ này không được lưu vào template.
- **Không** tạo DB audit table trong story này — structured log là đủ cho MVP, consistent với codebase hiện tại.
- **Không** validate readiness chỉ client-side — server phải kiểm tra đầy đủ.
- **Không** thay đổi AnswerKey rows khi mark-ready — chỉ thay đổi `Status`.
- **Không** cộng thêm transition nếu template đã Ready — idempotent, trả 200.
- **Không** reuse `mapMaterialApiError` hoặc `mapAnswerKeyApiError` — tạo `mapMarkReadyError` riêng.
- **Không** gọi multiple `SaveChangesAsync` trong `MarkReadyAsync` — một transaction cho cả template + answerKey status update.
- **Không** query template ownership riêng trong service — policy handler đã check.

### Latest Tech Information

- **EF Core 10 `AnyAsync`**: pattern `dbContext.TestMaterials.AnyAsync(m => m.TemplateId == templateId && m.Role == MaterialRoles.Pdf && m.IsActive, ct)` — efficient boolean check.
- **`System.Text.Json`**: consistent với `AnswerKeyService.MapResponse` — dùng `JsonSerializerDefaults.Web` options đã có sẵn.
- **Angular 22 signals**: dùng `signal<...>()` + `computed()` cho template state, readiness checks — consistent với 2.4 patterns.
- **`forkJoin`**: Angular RxJS — load parallel observables, complete khi tất cả emit; appropriate cho load-on-init.
- **`catchError(() => of(null))`**: pattern cho optional streams (answerKey có thể 404 bình thường với speaking) — kết quả là null trong forkJoin.

### Project Context Reference

- [CLAUDE.md] — stack, ProblemDetails, no JWT, quality script, cookie auth + XSRF.
- [architecture.md] — idempotency strategy, state machines, `409 Conflict`, audit, naming patterns.
- [docs/setup/development.md] — dev teacher account.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Story 2.5]
- [Source: `_bmad-output/C-UX-Scenarios/.../1.7-create-test-review-publish.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — idempotency, audit, state machines]
- [Source: `2-4-answerkey-and-scoring-configuration.md` — previous story learnings]

## Dev Agent Record

### Agent Model Used

_TBD_

### Debug Log References

_TBD_

### Completion Notes List

_TBD_

### File List

_TBD_

### Review Findings

**Patch items:**

- [x] [Review][Patch] Unknown skill (không phải reading/listening/speaking) bypass toàn bộ material + answerKey check → thêm `else return missingRequiredMaterial` [`src/.../TestTemplateService.cs:MarkReadyAsync`]
- [x] [Review][Patch] `logger.LogInformation` fire trước `SaveChangesAsync` → false audit trail nếu save fail → move log sau save thành công [`src/.../TestTemplateService.cs:MarkReadyAsync`]
- [x] [Review][Patch] `readinessChecks` computed dùng `mats.length > 0` cho reading/listening, không verify PDF role → UX misleading nếu có audio-only material [`src/.../test-template-review.component.ts:readinessChecks`]
- [x] [Review][Patch] `onBack()` luôn navigate đến `answer-key`; speaking template nên navigate về `materials` [`src/.../test-template-review.component.ts:onBack`]
- [x] [Review][Patch] Thiếu `review-publish-save-draft-button` với Object ID đúng theo UX spec 01.7 [`src/.../test-template-review.component.html`]

**Defer items:**

- [x] [Review][Defer] Race condition concurrent mark-ready → no concurrency token trên TestTemplate → double-transition without error — deferred, pre-existing
- [x] [Review][Defer] JsonException khi deserialize RowsJson → rows=[] → lỗi hiển thị `answerKeyIncomplete` thay vì `answerKeyCorrupt` — deferred, pre-existing
- [x] [Review][Defer] Speaking stale material (PDF từ lần thay đổi skill trước) pass speaking material check — deferred, pre-existing domain edge case
- [x] [Review][Defer] loadPage không reset template/materials/answerKey signals trước load mới → stale data visible trong rapid navigation — deferred, pre-existing pattern
- [x] [Review][Defer] getAnswerKey inner catch swallows 5xx transient errors, hiện thị answerKey=null — deferred, consistent with existing pattern
- [x] [Review][Defer] OperationCanceledException không bắt trong MarkReadyAsync — deferred, pre-existing pattern across codebase
- [x] [Review][Defer] Archived template khi load → viewState='loaded', hiện mark-ready button → user bị stuck với 409 error — deferred UX polish
- [x] [Review][Defer] AC2: focus/scroll tới issue đầu tiên blocking chưa implement — deferred UX polish
- [x] [Review][Defer] AC1/UX: Confirmation modal trước mark-ready chưa implement — deferred UX polish
- [x] [Review][Defer] AC5: Durable audit table chưa có (chỉ structured log) — deferred, Epic 6
- [x] [Review][Defer] Service MarkReadyAsync không verify teacherId nội bộ, ownership chỉ qua controller policy — deferred, consistent with existing architecture
- [x] [Review][Defer] AnswerKeyVersions.FirstOrDefaultAsync không có ordering → non-deterministic nếu có multiple versions — deferred, versioning chưa implement

## Change Log

- 2026-06-11: Story 2.5 created từ epics + UX 01.7 + architecture + 2.4 learnings; ready-for-dev.
