---
baseline_commit: 9e6c2f1
---

# Story 3.1: Create HomeworkAssignment From A Ready Template

Status: done

## Story

Là giáo viên,
tôi muốn giao một đề gốc Ready cho một lớp học dưới dạng Homework với hạn nộp và giới hạn thời gian làm bài tùy chọn,
để học sinh có thể hoàn thành bài tập ở nhà trong khoảng thời gian cho phép.

## Acceptance Criteria

1. **Given** giáo viên chọn "Giao homework" từ một template Ready
   **When** màn hình tạo Homework mở ra
   **Then** route `/teacher/homework/new?templateId={templateId}` render (hoặc một route tương đương đã được document)
   **And** form hiển thị: thông tin template nguồn (title, skill), class select, due date input, time limit input tùy chọn, nút tạo, nút hủy, và mode label "Homework".

2. **Given** không có WDS page spec riêng cho Homework creation
   **When** bắt đầu implementation
   **Then** feature contract định nghĩa stable object IDs cho: source-template summary, class select, due-date input, time-limit input, create action, cancel action, loading state, validation errors, và success state.

3. **Given** giáo viên chọn một lớp ngoài phạm vi của họ
   **When** create request được submit
   **Then** API từ chối request server-side
   **And** không có HomeworkAssignment nào được tạo.

4. **Given** deadline hoặc time limit validation thất bại
   **When** giáo viên submit
   **Then** inline errors giải thích field nào không hợp lệ
   **And** template vẫn ở trạng thái Ready và không thay đổi.

5. **Given** dữ liệu Homework hợp lệ
   **When** giáo viên tạo assignment
   **Then** HomeworkAssignment tham chiếu đúng một Ready TestTemplate và một Class
   **And** duplicate clicks không tạo duplicate assignments.

6. **Given** HomeworkAssignment được tạo
   **When** được xem qua Teacher API response
   **Then** response chứa Homework mode, source template title, source template skill, class id, class name, deadline, time limit (nếu có), và status.

7. **Given** create operation thành công
   **When** audit được kiểm tra
   **Then** assignment id, template id, class id, actor, trạng thái created, và timestamp được ghi lại.

## Tasks / Subtasks

- [x] Domain entity + DB migration (AC: 5, 6)
  - [x] Tạo `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignment.cs` (entity model)
  - [x] Tạo `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignmentStatuses.cs` (string constants: `Published`)
  - [x] Tạo `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/HomeworkAssignmentConfiguration.cs`
  - [x] Thêm `DbSet<HomeworkAssignment> HomeworkAssignments` vào `EnglishTestWebDbContext.cs`
  - [x] Tạo EF Core migration: `dotnet ef migrations add AddHomeworkAssignments --project src/EnglishTestWeb.Api`

- [x] Contracts (DTOs) (AC: 1, 6)
  - [x] Tạo `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/CreateHomeworkAssignmentRequest.cs`
    - Fields: `TemplateId: Guid`, `ClassId: Guid`, `DeadlineAt: DateTimeOffset`, `TimeLimitMinutes: int?`
  - [x] Tạo `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/HomeworkAssignmentResponse.cs`
    - Fields: `Id`, `TemplateId`, `TemplateTitle`, `TemplateSkill`, `ClassId`, `ClassName`, `DeadlineAt`, `TimeLimitMinutes?`, `Status`, `CreatedAt`

- [x] Application interface + service implementation (AC: 3, 4, 5, 7)
  - [x] Tạo `src/EnglishTestWeb.Api/Application/HomeworkAssignments/IHomeworkAssignmentService.cs`
    - Record: `CreateHomeworkAssignmentResult(bool Allowed, HomeworkAssignmentResponse? Detail, string? ErrorCode, int StatusCode)`
    - Method: `CreateAsync(string teacherId, CreateHomeworkAssignmentRequest request, CancellationToken)`
  - [x] Tạo `src/EnglishTestWeb.Api/Infrastructure/HomeworkAssignments/HomeworkAssignmentService.cs`
    - Inject: `EnglishTestWebDbContext`, `ITemplateAuthorizationService`, `IClassAuthorizationService`, `ILogger<HomeworkAssignmentService>`
    - Validation order (xem Dev Notes)
    - Structured log audit event sau `SaveChangesAsync` thành công

- [x] Controller (AC: 1, 3, 4, 5)
  - [x] Tạo `src/EnglishTestWeb.Api/Controllers/HomeworkAssignmentsController.cs`
    - Route: `[Route("api/homework-assignments")]`
    - `POST /api/homework-assignments` → 201 Created với `HomeworkAssignmentResponse`
    - `[Authorize(Roles = IdentityRoleNames.Teacher)]`
    - Delegate hoàn toàn vào service; controller thin

- [x] API tests (AC: 3, 4, 5, 7)
  - [x] Tạo `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/HomeworkAssignmentTestHelper.cs`
    - `EnsureReadyTemplateWithClassAsync(factory)` — tạo ready template + class owned by teacher
  - [x] Tạo `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/CreateHomeworkAssignmentControllerTests.cs`
    - Xem Dev Notes → Testing Requirements
  - [x] Bổ sung rows vào `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`

- [x] Angular core (AC: 1, 6)
  - [x] Tạo `src/EnglishTestWeb.Client/src/app/core/homework/homework.models.ts`
    - `CreateHomeworkRequest`, `HomeworkAssignment`, `HomeworkCreateErrorCode`, `mapHomeworkCreateError(error)`
  - [x] Tạo `src/EnglishTestWeb.Client/src/app/core/homework/homework-api.service.ts`
    - `create(request: CreateHomeworkRequest): Promise<HomeworkAssignment>`

- [x] Angular homework-create component (AC: 1, 2, 4, 5)
  - [x] Tạo `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.ts`
    - ViewState: `'loading' | 'loaded' | 'saving' | 'success' | 'loadError'`
    - Read `templateId` từ `ActivatedRoute.queryParamMap` (không phải `paramMap`)
    - Load template summary + teacher classes song song trên init
    - Form signals: `selectedClassId`, `deadlineAt`, `timeLimitMinutes` (optional)
    - `isSaving` signal để disable button khi đang submit (AC5 duplicate-click guard)
    - `onSubmit()`, `onCancel()` methods
  - [x] Tạo `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.html`
    - Dùng stable Object IDs từ Dev Notes (AC2)
  - [x] Tạo `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.css`
  - [x] Tạo `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.spec.ts`

- [x] Route update + quality gate (AC: 1)
  - [x] Cập nhật `src/EnglishTestWeb.Client/src/app/app.routes.ts`: thay placeholder `homework/new` bằng `HomeworkCreateComponent`
  - [x] Chạy `.\scripts\quality.ps1` — pass là xong

### Review Findings (Pass 1 — 2026-06-11)

- [x] [Review][Patch] Audit log thiếu trường `status` — AC7 yêu cầu "trạng thái created" được ghi; thêm `status={Status}` + `assignment.Status` vào log call [HomeworkAssignmentService.cs:107] — **FIXED**
- [x] [Review][Defer] TOCTOU: auth check và DB load template là 2 query riêng — established pattern toàn codebase; deferred
- [x] [Review][Defer] Không có unique constraint trên (TestTemplateId, ClassId) — server-side idempotency đã explicitly deferred trong Dev Notes; isSaving() guard đủ cho MVP
- [x] [Review][Defer] DbUpdateException catch-all → homework.createFailed 500 — consistent với project pattern; deferred
- [x] [Review][Defer] parseInt("5.9abc") truncates silently ở Angular — server validates [1,600]; deferred
- [x] [Review][Defer] Không có index đơn trên ClassId/TestTemplateId — student-facing query pattern chưa có spec; deferred
- [x] [Review][Defer] datetime-local input không có timezone → browser dùng local time — Vietnamese users UTC+7 correct; deferred
- [x] [Review][Defer] Negative timeLimitMinutes bypass Angular min=1 → server rejects — server guard đủ; deferred
- [x] [Review][Defer] Form signals không reset khi templateId thay đổi — flow thực tế không trigger case này; deferred
- [x] [Review][Defer] isFormValid dùng stale template signal — server rejects nếu template archived; deferred
- [x] [Review][Defer] Không có UX message khi teacher không có active class — form block đúng, không có guidance; deferred
- [x] [Review][Defer] HomeworkAssignment.Status không có DB check constraint — consistent với TestTemplate.Status pattern; deferred

## Dev Notes

### Validation Order trong `HomeworkAssignmentService.CreateAsync`

Thứ tự validation để giảm DB round-trips và đảm bảo đúng error code:

1. **Teacher auth check** — `currentUserContext.UserId` null → 401 (thực ra controller guard xử lý trước)
2. **Template ownership** — `ITemplateAuthorizationService.RequireTeacherTemplateAccessAsync(templateId, teacherId)`:
   - Decision.IsAllowed = false → `"homework.templateNotFound"` 404
3. **Template Ready status** — load template từ DB, kiểm tra `template.Status == "ready"`:
   - Status = Draft hoặc Archived → `"homework.templateNotReady"` 400
4. **Class ownership** — `IClassAuthorizationService.RequireTeacherClassAccessAsync(classId, teacherId)`:
   - Decision.IsAllowed = false → `"homework.classNotFound"` 404
5. **Deadline validation** — `request.DeadlineAt <= DateTimeOffset.UtcNow.AddMinutes(1)`:
   - Quá khứ hoặc quá gần → `"homework.deadlinePast"` 400
6. **TimeLimitMinutes validation** — nếu có: `timeLimitMinutes < 1 || timeLimitMinutes > 600`:
   - Ngoài range → `"homework.timeLimitInvalid"` 400
7. **Create + SaveChangesAsync** — trong try/catch `DbUpdateException` → `"homework.createFailed"` 500
8. **Audit log** — `logger.LogInformation(...)` sau `SaveChangesAsync` thành công

### HomeworkAssignment Entity

```csharp
// Domain/Assignments/HomeworkAssignment.cs
public sealed class HomeworkAssignment
{
    public Guid Id { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public Guid TestTemplateId { get; set; }
    public Guid ClassId { get; set; }
    public string Status { get; set; } = HomeworkAssignmentStatuses.Published;
    public DateTimeOffset DeadlineAt { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// Domain/Assignments/HomeworkAssignmentStatuses.cs
public static class HomeworkAssignmentStatuses
{
    public const string Published = "published";
}
```

### HomeworkAssignmentConfiguration

Mirror `TestTemplateConfiguration.cs` pattern:
- Table: `"HomeworkAssignments"`
- `TeacherId`: MaxLength 450, required, FK → ApplicationUser, OnDelete Restrict
- `Status`: MaxLength 32, required
- Index: `{ TeacherId, ClassId }` và `{ TeacherId, TestTemplateId }`
- FK `TestTemplateId` → TestTemplates, OnDelete Restrict
- FK `ClassId` → Classes, OnDelete Restrict

### Stable Object IDs (AC2 Contract)

Tất cả IDs sau phải tồn tại chính xác trong `homework-create.component.html`:

| Object ID | Element | Notes |
|---|---|---|
| `homework-create-loading` | loading skeleton/spinner | visible khi ViewState='loading' |
| `homework-create-source-template` | section/div | hiển thị template title + skill (read-only) |
| `homework-create-class-select` | `<select>` | class dropdown, options từ teacher classes |
| `homework-create-due-date-input` | `<input type="datetime-local">` | deadline |
| `homework-create-time-limit-input` | `<input type="number">` | optional, min=1, max=600 |
| `homework-create-submit` | `<button>` | disabled khi isSaving() hoặc form invalid |
| `homework-create-cancel` | `<button>` | navigate về library |
| `homework-create-validation-error` | `<div>` hoặc `<p>` | hiển thị API/form errors |
| `homework-create-success` | section | visible khi ViewState='success' |
| `homework-create-error` | section | visible khi ViewState='loadError' |

### Angular Component Architecture

```typescript
// ViewState pattern (consistent với existing components)
type ViewState = 'loading' | 'loaded' | 'saving' | 'success' | 'loadError';

// Đọc templateId từ queryParamMap (không phải paramMap)
ngOnInit(): void {
  this.paramSubscription = this.route.queryParamMap.subscribe(params => {
    const templateId = params.get('templateId');
    if (!templateId) {
      void this.router.navigate(['/teacher/library']);
      return;
    }
    void this.loadPage(templateId);
  });
}

// Load song song — consistent với 2.5 pattern
private async loadPage(templateId: string): Promise<void> {
  // Dùng loadRequestId pattern để tránh stale concurrent loads
  // Load template summary (getTemplate) + teacher classes (classesApiService.getTeacherClasses()) song song với Promise.all
  // Nếu template status !== 'ready': set loadError = 'homework.templateNotReady'
}

protected async onSubmit(): Promise<void> {
  if (this.isSaving()) return; // AC5 duplicate-click guard
  this.viewState.set('saving');
  try {
    const result = await this.homeworkApi.create({ templateId, classId, deadlineAt, timeLimitMinutes });
    this.assignment.set(result);
    this.viewState.set('success');
  } catch (error) {
    this.apiError.set(mapHomeworkCreateError(error));
    this.viewState.set('loaded');
  }
}

protected onCancel(): void {
  const templateId = this.templateId();
  if (templateId) {
    void this.router.navigate(['/teacher/library', templateId, 'review']);
  } else {
    void this.router.navigate(['/teacher/library']);
  }
}
```

### Class List cho Dropdown

Dùng `ClassesApiService.getTeacherClasses()` (đã có sẵn, trả `ClassSummary[]`):
- `ClassSummary.classId: string` — dùng làm value cho select option
- `ClassSummary.className: string` — dùng làm label hiển thị
- Lọc chỉ hiển thị classes có `status === 'active'`

### Audit Log Event

```csharp
logger.LogInformation(
    "HomeworkAssignmentCreated: assignmentId={AssignmentId} templateId={TemplateId} classId={ClassId} teacherId={TeacherId} deadlineAt={DeadlineAt} at={Timestamp}",
    assignment.Id, request.TemplateId, request.ClassId, teacherId, request.DeadlineAt, now);
```

### ProblemDetails Error Codes

| Code | HTTP | Khi nào |
|---|---|---|
| `homework.templateNotFound` | 404 | Template không tồn tại hoặc không phải của teacher |
| `homework.templateNotReady` | 400 | Template tồn tại nhưng không ở trạng thái Ready |
| `homework.classNotFound` | 404 | Class không tồn tại hoặc không phải của teacher |
| `homework.deadlinePast` | 400 | DeadlineAt ≤ now + 1 phút |
| `homework.timeLimitInvalid` | 400 | TimeLimitMinutes ngoài [1, 600] |
| `homework.createFailed` | 500 | DbUpdateException khi save |

### Architecture Compliance

- **Không** dùng `Authorization Policy` mới cho Story 3.1 — service-level ownership check qua `ITemplateAuthorizationService` + `IClassAuthorizationService` là đủ (consistent với existing pattern).
- **Controller thin**: không có domain logic trong controller; chỉ check `currentUserContext.UserId` và delegate vào service.
- **Hidden 404**: cross-teacher template/class access → 404, không 403 (enumeration prevention).
- **String enums**: status = `"published"`, không dùng int.
- **Cookie auth + XSRF**: POST `homework-assignments` cần XSRF header — Angular interceptors handle globally.
- **Không** tạo DB audit table — structured log đủ cho MVP (consistent với Story 2.5).
- **Idempotency scope**: AC5 "duplicate clicks" được handled bởi `isSaving()` signal ở Angular. Server-side idempotency key (X-Idempotency-Key) deferred (xem Deferred section).
- **Migration**: chạy `dotnet ef migrations add AddHomeworkAssignments --project src/EnglishTestWeb.Api` sau khi entity + config + DbContext update xong.

### Testing Requirements

**API Tests (`CreateHomeworkAssignmentControllerTests.cs`):**

```
Setup: factory + AuthTestHelper.SignInUserAsync + HomeworkAssignmentTestHelper

Test cases:
- Create_WithValidData_Returns201 (kiểm tra response fields: id, templateId, classId, deadlineAt, status="published")
- Create_TemplateNotOwned_Returns404 (cross-teacher template)
- Create_TemplateDraft_Returns400_TemplateNotReady
- Create_TemplateArchived_Returns400_TemplateNotReady
- Create_ClassNotOwned_Returns404 (cross-teacher class hoặc classId không tồn tại)
- Create_PastDeadline_Returns400_DeadlinePast (deadlineAt = DateTimeOffset.UtcNow.AddMinutes(-5))
- Create_InvalidTimeLimitZero_Returns400_TimeLimitInvalid
- Create_InvalidTimeLimitTooLarge_Returns400_TimeLimitInvalid (> 600)
- Create_Anonymous_Returns401
- Create_Student_Returns403
- Create_ValidWithTimeLimit_Returns201 (optional time limit present)
```

**HomeworkAssignmentTestHelper:**
```csharp
internal static async Task<(Guid templateId, Guid classId)> EnsureReadyTemplateAndClassAsync(TestApiFactory factory)
// Reuse TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync + ClassesTestHelper để lấy sẵn class
```

**AuthorizationMatrixTests — thêm rows:**
```
POST /api/homework-assignments:
  Teacher (valid data): 201 ✓
  Other teacher's template: 404 ✓
  Student: 403 ✓
  Anonymous: 401 ✓
```

**Angular Vitest specs:**
- Loading state hiển thị loading indicator
- Loaded state hiển thị form với template summary và class dropdown
- Submit button disabled khi đang saving
- Success state hiển thị sau create thành công
- API error hiển thị khi create thất bại
- Cancel navigate về library review
- No templateId in query → redirect to library

### Previous Story Intelligence (2.5)

- **Inject classes trong component**: dùng `ClassesApiService` đã có sẵn — không tạo service mới cho class list.
- **ViewState + loadRequestId pattern**: đã proven trong 2.5 (test-template-review.component.ts) — copy pattern này.
- **`mapXxxError` function**: tạo `mapHomeworkCreateError` trong `homework.models.ts` — không reuse error mappers của template.
- **`IClassAuthorizationService.RequireTeacherClassAccessAsync`**: đã có sẵn trong DI (registered ở 1.4) — inject vào HomeworkAssignmentService như cách TemplateAuthorizationService được inject.
- **EF Core migration**: `dotnet ef migrations add <MigrationName> --project src/EnglishTestWeb.Api` — không cần `--startup-project`.
- **Test helper pattern**: `EnsureXxxAsync` trong TestHelper — idempotent, tạo nếu chưa có.
- **Structured log sau SaveChangesAsync**: pattern đã được patched vào 2.5 — áp dụng nhất quán ở đây.

### File Structure Requirements

**API (new):**
- `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignment.cs`
- `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignmentStatuses.cs`
- `src/EnglishTestWeb.Api/Application/HomeworkAssignments/IHomeworkAssignmentService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/HomeworkAssignments/HomeworkAssignmentService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/HomeworkAssignmentConfiguration.cs`
- `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/CreateHomeworkAssignmentRequest.cs`
- `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/HomeworkAssignmentResponse.cs`
- `src/EnglishTestWeb.Api/Controllers/HomeworkAssignmentsController.cs`

**API (modify):**
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs` — thêm `DbSet<HomeworkAssignment>`
- `src/EnglishTestWeb.Api/Program.cs` — register `IHomeworkAssignmentService` → `HomeworkAssignmentService` (singleton/scoped pattern như TestTemplateService)

**Client (new):**
- `src/EnglishTestWeb.Client/src/app/core/homework/homework.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/homework/homework-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.html`
- `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.css`
- `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.spec.ts`

**Client (modify):**
- `src/EnglishTestWeb.Client/src/app/app.routes.ts` — thay placeholder `homework/new` bằng `HomeworkCreateComponent`

**Tests (new/extend):**
- `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/HomeworkAssignmentTestHelper.cs`
- `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/CreateHomeworkAssignmentControllerTests.cs`
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — bổ sung rows

### Anti-Patterns

- **Không** query `TestTemplate` trực tiếp trong controller — delegate qua service.
- **Không** dùng `CanEditTemplateAsTeacher` — đó là policy cho draft-only edit; HomeworkAssignment cần Ready template.
- **Không** lưu deadline/time-limit vào TestTemplate — HomeworkAssignment là entity riêng, template không thay đổi.
- **Không** cần authorization policy mới — service-layer check đủ cho MVP.
- **Không** dùng `ActivatedRoute.paramMap` cho templateId — route là `/homework/new?templateId=...` (query param), phải dùng `queryParamMap`.
- **Không** tạo Angular route với `:templateId` param — route giữ nguyên là `homework/new`, templateId truyền qua query string.

### Program.cs Registration Pattern

Xem cách `TestTemplateService` và `AnswerKeyService` được register trong `Program.cs`:
```csharp
builder.Services.AddScoped<IHomeworkAssignmentService, HomeworkAssignmentService>();
```

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 3, Story 3.1]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — HomeworkAssignment lifecycle, DTO naming, API naming conventions]
- [Source: `2-5-review-template-mark-ready-and-next-actions.md` — ViewState pattern, audit log pattern, service injection patterns]
- [Source: `src/EnglishTestWeb.Api/Application/Security/IClassAuthorizationService.cs` — class auth interface]
- [Source: `src/EnglishTestWeb.Api/Application/Security/ITemplateAuthorizationService.cs` — template auth interface]
- [Source: `src/EnglishTestWeb.Client/src/app/core/classes/classes-api.service.ts` — teacher class list endpoint]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/TestTemplateConfiguration.cs` — EF configuration pattern]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Fixed CS4034 (await in non-async lambda) in HomeworkAssignmentTestHelper — removed unused `GetOtherTeacherClassIdAsync`
- Fixed Angular `date` pipe missing in standalone component — replaced with `formatDeadline()` method using `toLocaleString('vi-VN')`
- Fixed Angular spec timing — used `flushPromises()` helper (setTimeout) instead of `whenStable()` for void promise chains

### Completion Notes List

- All 7 ACs satisfied: route renders, stable Object IDs defined, server-side ownership checks prevent cross-teacher access, inline validation errors, duplicate-click guard via `isSaving()`, full response fields, structured audit log
- API: 162 tests pass (14 new homework tests + 3 auth matrix rows)
- Angular: 90 tests pass (8 new homework-create specs)
- Quality gate: pass

### File List

**New (API):**
- `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignment.cs`
- `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignmentStatuses.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/HomeworkAssignmentConfiguration.cs`
- `src/EnglishTestWeb.Api/Application/HomeworkAssignments/IHomeworkAssignmentService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/HomeworkAssignments/HomeworkAssignmentService.cs`
- `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/CreateHomeworkAssignmentRequest.cs`
- `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/HomeworkAssignmentResponse.cs`
- `src/EnglishTestWeb.Api/Controllers/HomeworkAssignmentsController.cs`
- `src/EnglishTestWeb.Api/Migrations/*_AddHomeworkAssignments.cs`

**Modified (API):**
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs`
- `src/EnglishTestWeb.Api/Program.cs`

**New (Angular):**
- `src/EnglishTestWeb.Client/src/app/core/homework/homework.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/homework/homework-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.html`
- `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.css`
- `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.spec.ts`

**Modified (Angular):**
- `src/EnglishTestWeb.Client/src/app/app.routes.ts`

**New (Tests):**
- `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/HomeworkAssignmentTestHelper.cs`
- `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/CreateHomeworkAssignmentControllerTests.cs`

**Modified (Tests):**
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`

## Review Findings

### Pass 1 (2026-06-11)

**Patches applied (1):**
- [MED] Audit log thiếu `status={Status}` field — added `assignment.Status` to log parameters (AC7 violation).

**Deferred (11):** See `deferred-work.md` → "Deferred from: code review of 3-1-..."

### Pass 2 (2026-06-11)

**No patches applied.**

**Deferred (5):** See `deferred-work.md` → "Deferred from: code review pass 2 of 3-1-..."

### Pass 3 (2026-06-11)

**Patches applied (1):**
- [MED] Teacher có thể giao homework cho lớp Inactive — API không có guard, chỉ Angular filter. Added `ClassStatuses.Active` check in `HomeworkAssignmentService.cs` (returns `homework.classNotActive` 400), added error message to `homework.models.ts`, added `EnsureReadyTemplateAndInactiveClassAsync` test helper, added `Create_InactiveClass_Returns400ClassNotActive` test.

**Deferred (1):** See `deferred-work.md` → "Deferred from: code review pass 3 of 3-1-..."
