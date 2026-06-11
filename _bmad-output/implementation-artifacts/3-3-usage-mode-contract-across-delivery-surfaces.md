---
baseline_commit: 3f9a829
---

# Story 3.3: Usage Mode Contract Across Delivery Surfaces

Status: done

## Story

Là giáo viên và học sinh,
tôi muốn Homework và Thi trực tiếp luôn hiển thị và có cấu trúc phân biệt rõ ràng,
để không ai nhầm lẫn giữa đề gốc (TestTemplate) với một assignment hoặc live session cụ thể.

## Acceptance Criteria

1. **Given** HomeworkAssignment và LiveExamSession APIs trả về list/detail DTOs
   **When** các DTOs đó được serialize
   **Then** chúng bao gồm `mode`, source template id/title, class id/name, instance id, status, và allowed actions.

2. **Given** một template được hiển thị trong Thư viện đề
   **When** usage actions được hiển thị
   **Then** labels là "Giao homework" và "Tạo thi trực tiếp"
   **And** template bản thân không bị gán nhãn là "assigned bài thi".

3. **Given** student-facing item là Homework
   **When** item được hiển thị (hiện tại: constants/models chuẩn bị cho Epic 4)
   **Then** labels và status copy dùng semantics Homework/Bài tập về nhà.

4. **Given** student-facing item là Live Exam
   **When** item được hiển thị (hiện tại: constants/models chuẩn bị cho Epic 4)
   **Then** labels và status copy dùng semantics Thi trực tiếp, bao gồm: chưa mở / đang mở / đã đóng.

## Tasks / Subtasks

- [x] Task 1: Thêm `Mode` và `AllowedActions` vào backend DTOs (AC1)
  - [x] 1.1 Cập nhật `Contracts/HomeworkAssignments/HomeworkAssignmentResponse.cs` — thêm `string Mode` và `IReadOnlyList<string> AllowedActions`
  - [x] 1.2 Cập nhật `Infrastructure/HomeworkAssignments/HomeworkAssignmentService.cs` — set `Mode = "homework"`, `AllowedActions = []` (không có action sau create trong MVP)
  - [x] 1.3 Cập nhật `Contracts/LiveExamSessions/LiveExamSessionResponse.cs` — thêm `string Mode` và `IReadOnlyList<string> AllowedActions`
  - [x] 1.4 Cập nhật `Infrastructure/LiveExamSessions/LiveExamSessionService.cs` — set `Mode = "live-exam"`, `AllowedActions` = computed từ Status: `scheduled` → `["open"]`, `open` → `["close"]`, `closed` → `[]`
  - [x] 1.5 `dotnet test` — xác nhận tất cả 194 tests vẫn pass

- [x] Task 2: Cập nhật API tests kiểm tra `mode` và `allowedActions` (AC1)
  - [x] 2.1 `HomeworkAssignments/CreateHomeworkAssignmentControllerTests.cs` — assert `response.mode == "homework"` và `response.allowedActions` trong test `Create_WithValidData_Returns201`
  - [x] 2.2 `LiveExamSessions/CreateLiveExamSessionControllerTests.cs` — assert `response.mode == "live-exam"` và `response.allowedActions == ["open"]` (scheduled) trong `Create_WithValidData_Returns201WithScheduledStatus`
  - [x] 2.3 `LiveExamSessions/OpenCloseControllerTests.cs` — assert `response.allowedActions == ["close"]` sau Open; `response.allowedActions == []` sau Close
  - [x] 2.4 `dotnet test` — 194 tests pass

- [x] Task 3: Cập nhật Angular models cho `mode` field (AC3, AC4)
  - [x] 3.1 Cập nhật `core/homework/homework.models.ts` — thêm `mode: string` và `allowedActions: string[]` vào interface `HomeworkAssignment`
  - [x] 3.2 Cập nhật `core/live-exam/live-exam.models.ts` — thêm `mode: string` và `allowedActions: string[]` vào interface `LiveExamSession`
  - [x] 3.3 Thêm `MODE_LABELS`, `HOMEWORK_STATUS_LABELS`, `LIVE_EXAM_STATUS_LABELS` vào `core/test-templates/test-templates.models.ts`

- [x] Task 4: Enable navigation buttons trong template library và review page (AC2)
  - [x] 4.1 Cập nhật `features/test-template-library/test-template-library.component.ts` — `onHomeworkAction` và `onLiveExamAction` navigate khi template ready
  - [x] 4.2 Cập nhật `features/test-template-review/test-template-review.component.html` — bỏ disabled, thêm click handlers
  - [x] 4.3 Cập nhật `features/test-template-review/test-template-review.component.ts` — thêm `onGoToHomework()` và `onGoToLiveExam()`
  - [x] 4.4 `npm test` — 104 Angular tests pass (tăng từ 102)

- [x] Task 5: Cập nhật Angular tests (AC2)
  - [x] 5.1 `features/test-template-review/test-template-review.component.spec.ts` — thêm 2 tests navigation cho homework và live-exam buttons
  - [x] 5.2 `features/test-template-library/test-template-library.component.spec.ts` — không tồn tại; library không có spec file riêng
  - [x] 5.3 `npm test` — 104 tests pass

### Review Findings

- [x] [Review][Decision] Button label "Tạo phiên thi trực tiếp" vs spec AC2 "Tạo thi trực tiếp" — resolved: keep "Tạo phiên thi trực tiếp" (adds specificity, Dev Notes consistent); dismissed.

- [x] [Review][Patch] Missing tests: `test-template-library.component.spec.ts` has no happy-path coverage for `onHomeworkAction(readyTemplate)` → navigate to `/teacher/homework/new` and `onLiveExamAction(readyTemplate)` → navigate to `/teacher/live-exams/new` — **fixed: added 2 tests, 106 total pass** [`src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.spec.ts`]

- [x] [Review][Defer] `HomeworkAssignment.AllowedActions` hardcoded `Array.Empty<string>()` — no `AllowedActionsFor` helper unlike live exam; correct for MVP (published is only status with no transitions) but asymmetric design [`src/EnglishTestWeb.Api/Infrastructure/HomeworkAssignments/HomeworkAssignmentService.cs:128`] — deferred, pre-existing design

- [x] [Review][Defer] `mode` values ("homework", "live-exam") are magic string literals with no shared constant — consistent with project convention (all status values are strings) [`HomeworkAssignmentService.cs:125`, `LiveExamSessionService.cs:240`] — deferred, pre-existing project convention

- [x] [Review][Defer] Concurrent `OpenAsync` race condition on `LiveExamSession.Status` — two simultaneous requests can both pass the scheduled check and double-transition; pre-existing gap noted in story 3-2 defer [`LiveExamSessionService.cs` OpenAsync] — deferred, pre-existing

- [x] [Review][Defer] No GET list/detail endpoints for HomeworkAssignment or LiveExamSession — when built, their mapping code must include Mode/AllowedActions; no structural enforcement prevents omission — deferred, endpoints not yet built

## Dev Notes

### Backend DTO Changes

**HomeworkAssignmentResponse.cs** — sau khi cập nhật:
```csharp
namespace EnglishTestWeb.Api.Contracts.HomeworkAssignments;

public sealed record HomeworkAssignmentResponse(
    Guid Id,
    Guid TemplateId,
    string TemplateTitle,
    string TemplateSkill,
    Guid ClassId,
    string ClassName,
    DateTimeOffset DeadlineAt,
    int? TimeLimitMinutes,
    string Status,
    string Mode,
    IReadOnlyList<string> AllowedActions,
    DateTimeOffset CreatedAt);
```

**LiveExamSessionResponse.cs** — sau khi cập nhật:
```csharp
namespace EnglishTestWeb.Api.Contracts.LiveExamSessions;

public sealed record LiveExamSessionResponse(
    Guid Id,
    Guid TemplateId,
    string TemplateTitle,
    string TemplateSkill,
    Guid ClassId,
    string ClassName,
    string Status,
    string Mode,
    IReadOnlyList<string> AllowedActions,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? ScheduledEndAt,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt);
```

**HomeworkAssignmentService.cs** mapping (Task 1.2):
```csharp
var response = new HomeworkAssignmentResponse(
    assignment.Id,
    template.Id,
    template.Title,
    template.Skill,
    schoolClass.Id,
    schoolClass.Name,
    assignment.DeadlineAt,
    assignment.TimeLimitMinutes,
    assignment.Status,
    Mode: "homework",
    AllowedActions: Array.Empty<string>(),   // không có action sau create trong MVP
    assignment.CreatedAt);
```

**LiveExamSessionService.cs** — `AllowedActionsFor` helper:
```csharp
private static IReadOnlyList<string> AllowedActionsFor(string status) =>
    status switch
    {
        LiveExamSessionStatuses.Scheduled => ["open"],
        LiveExamSessionStatuses.Open => ["close"],
        _ => Array.Empty<string>()
    };
```

Trong `MapResponse`:
```csharp
Mode: "live-exam",
AllowedActions: AllowedActionsFor(session.Status),
```

Đảm bảo gọi `MapResponse` sau mỗi transition (Open, Close) để `AllowedActions` phản ánh status mới.

### Angular Models — `delivery.models.ts` hay inline?

**Approach được chọn**: Thêm constants vào các file models hiện tại, KHÔNG tạo file mới:
- `MODE_LABELS`, `LIVE_EXAM_STATUS_LABELS`, `HOMEWORK_STATUS_LABELS` → thêm vào `core/test-templates/test-templates.models.ts` (vì các constants khác như `SKILL_LABELS`, `STATUS_LABELS` đã ở đây, và story 3.3 không yêu cầu tạo shared delivery layer)

Lý do: Tránh tạo abstraction mới cho scope MVP. Epic 4 có thể refactor nếu cần chia tách.

**homework.models.ts** sau update:
```typescript
export interface HomeworkAssignment {
  id: string;
  templateId: string;
  templateTitle: string;
  templateSkill: string;
  classId: string;
  className: string;
  deadlineAt: string;
  timeLimitMinutes: number | null;
  status: string;
  mode: string;          // "homework"
  allowedActions: string[]; // [] hiện tại
  createdAt: string;
}
```

**live-exam.models.ts** sau update:
```typescript
export interface LiveExamSession {
  id: string;
  templateId: string;
  templateTitle: string;
  templateSkill: string;
  classId: string;
  className: string;
  status: string;
  mode: string;          // "live-exam"
  allowedActions: string[]; // ["open"] | ["close"] | []
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
  openedAt: string | null;
  closedAt: string | null;
  createdAt: string;
}
```

### Library Component Navigation Fix (Task 4.1)

**Trước** (onHomeworkAction — không navigate):
```typescript
protected onHomeworkAction(template: TestTemplateListItem, event: Event): void {
    event.preventDefault();
    if (!this.isReady(template)) {
      this.blockedActionMessage.set(TEMPLATE_ERROR_MESSAGES['ERR_TEMPLATE_NOT_READY']);
      return;
    }
    this.blockedActionMessage.set(null);  // ← THIẾU navigate!
}
```

**Sau** (onHomeworkAction — navigate khi ready):
```typescript
protected onHomeworkAction(template: TestTemplateListItem, event: Event): void {
    event.preventDefault();
    if (!this.isReady(template)) {
      this.blockedActionMessage.set(TEMPLATE_ERROR_MESSAGES['ERR_TEMPLATE_NOT_READY']);
      return;
    }
    this.blockedActionMessage.set(null);
    void this.router.navigate(['/teacher/homework/new'], {
      queryParams: { templateId: template.templateId },
    });
}

protected onLiveExamAction(template: TestTemplateListItem, event: Event): void {
    event.preventDefault();
    if (!this.isReady(template)) {
      this.blockedActionMessage.set(TEMPLATE_ERROR_MESSAGES['ERR_TEMPLATE_NOT_READY']);
      return;
    }
    this.blockedActionMessage.set(null);
    void this.router.navigate(['/teacher/live-exams/new'], {
      queryParams: { templateId: template.templateId },
    });
}
```

### Review Page Success Banner Fix (Task 4.2 — 4.3)

**HTML** — thay từ disabled sang enabled với click handlers:
```html
<button
  id="review-create-homework-button"
  type="button"
  class="btn-primary"
  (click)="onGoToHomework()"
>
  Giao homework
</button>
<button
  id="review-create-live-exam-button"
  type="button"
  class="btn-secondary"
  (click)="onGoToLiveExam()"
>
  Tạo phiên thi trực tiếp
</button>
```

**TypeScript** — thêm 2 methods:
```typescript
protected onGoToHomework(): void {
    const tid = this.template()?.templateId;
    if (tid) {
        void this.router.navigate(['/teacher/homework/new'], {
            queryParams: { templateId: tid },
        });
    }
}

protected onGoToLiveExam(): void {
    const tid = this.template()?.templateId;
    if (tid) {
        void this.router.navigate(['/teacher/live-exams/new'], {
            queryParams: { templateId: tid },
        });
    }
}
```

### Stable Object IDs (AC2 — already in place)

Các IDs đã có trong library component và review page:
- `id="review-create-homework-button"` — review success banner
- `id="review-create-live-exam-button"` — review success banner
- Library component dùng `data-testid` hoặc element selectors (không cần thêm)

### Testing Requirements

**API Tests (`AnswerKeyControllerTests` pattern — thêm assertions):**

Trong `CreateHomeworkAssignmentControllerTests.cs`:
```csharp
// Assert mode và allowedActions trong Create_WithValidData_Returns201
var body = await response.Content.ReadFromJsonAsync<HomeworkAssignmentResponse>();
Assert.Equal("homework", body!.Mode);
Assert.Empty(body.AllowedActions);
```

Trong `CreateLiveExamSessionControllerTests.cs`:
```csharp
// Create trả scheduled → allowedActions = ["open"]
var body = await response.Content.ReadFromJsonAsync<LiveExamSessionResponse>();
Assert.Equal("live-exam", body!.Mode);
Assert.Equal(["open"], body.AllowedActions);
```

Trong `OpenCloseControllerTests.cs`:
```csharp
// Open → allowedActions = ["close"]
var openBody = await openResponse.Content.ReadFromJsonAsync<LiveExamSessionResponse>();
Assert.Equal(["close"], openBody!.AllowedActions);

// Close → allowedActions = []
var closeBody = await closeResponse.Content.ReadFromJsonAsync<LiveExamSessionResponse>();
Assert.Empty(closeBody!.AllowedActions);
```

**Angular Tests (`test-template-review.component.spec.ts`):**

```typescript
// Dùng flushPromises() pattern từ homework/live-exam tests
it('sau khi mark ready, click Giao homework navigates to homework/new', async () => {
    // Set viewState = 'success' với mock template
    (component as any).viewState.set('success');
    (component as any).template.set({ templateId: 'tid-123', ...mockTemplate });
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('#review-create-homework-button'));
    btn.triggerEventHandler('click', null);

    expect(mockRouter.navigate).toHaveBeenCalledWith(
        ['/teacher/homework/new'],
        { queryParams: { templateId: 'tid-123' } }
    );
});

it('sau khi mark ready, click Tạo phiên thi trực tiếp navigates to live-exams/new', async () => {
    (component as any).viewState.set('success');
    (component as any).template.set({ templateId: 'tid-123', ...mockTemplate });
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('#review-create-live-exam-button'));
    btn.triggerEventHandler('click', null);

    expect(mockRouter.navigate).toHaveBeenCalledWith(
        ['/teacher/live-exams/new'],
        { queryParams: { templateId: 'tid-123' } }
    );
});
```

### Architecture Compliance

- **DTO không nên chứa business logic** — `AllowedActionsFor` là static helper trong service, không trong entity
- **`mode` là constant string** — không phải enum, nhất quán với pattern `status = "homework"` / `status = "ready"` trong project
- **Không tạo thêm endpoint mới** — chỉ cập nhật existing DTOs và service mapping
- **Navigation từ Angular** — dùng `Router.navigate()` (không dùng `routerLink` trong template với dynamic params, đã có pattern trong homework-create/live-exam-create)
- **KHÔNG** tạo file `delivery.models.ts` mới — thêm vào file hiện tại để tránh thêm abstraction không cần thiết

### Anti-Patterns

- **KHÔNG** serialize `AllowedActions` thành `null` khi empty — luôn trả `[]` (empty array)
- **KHÔNG** tính `AllowedActions` ở controller layer — service owns state logic
- **KHÔNG** hard-code trong HTML template `disabled` placeholder với `title="Story X.X"` — những button này cần navigation thực sự
- **KHÔNG** tạo interface `IDeliveryMode` hay abstraction pattern mới trong scope story này

### Files Being Modified

**API (update):**
- `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/HomeworkAssignmentResponse.cs` — thêm Mode, AllowedActions
- `src/EnglishTestWeb.Api/Contracts/LiveExamSessions/LiveExamSessionResponse.cs` — thêm Mode, AllowedActions
- `src/EnglishTestWeb.Api/Infrastructure/HomeworkAssignments/HomeworkAssignmentService.cs` — populate Mode + AllowedActions
- `src/EnglishTestWeb.Api/Infrastructure/LiveExamSessions/LiveExamSessionService.cs` — populate Mode + AllowedActionsFor helper

**Angular (update):**
- `src/EnglishTestWeb.Client/src/app/core/homework/homework.models.ts` — thêm mode, allowedActions vào interface
- `src/EnglishTestWeb.Client/src/app/core/live-exam/live-exam.models.ts` — thêm mode, allowedActions vào interface
- `src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates.models.ts` — thêm MODE_LABELS, HOMEWORK_STATUS_LABELS, LIVE_EXAM_STATUS_LABELS
- `src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.ts` — enable navigation trong onHomeworkAction/onLiveExamAction
- `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.html` — bỏ disabled, thêm click handlers
- `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.ts` — thêm onGoToHomework(), onGoToLiveExam()

**Tests (update):**
- `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/CreateHomeworkAssignmentControllerTests.cs` — thêm mode/allowedActions assertions
- `tests/EnglishTestWeb.Api.Tests/LiveExamSessions/CreateLiveExamSessionControllerTests.cs` — thêm mode/allowedActions assertions
- `tests/EnglishTestWeb.Api.Tests/LiveExamSessions/OpenCloseControllerTests.cs` — thêm allowedActions transition assertions
- `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.spec.ts` — thêm navigation tests

### Context từ Story 3.1 và 3.2

1. **Không dùng `fixture.whenStable()`** — dùng `flushPromises()` pattern
2. **Signal set trong test**: `(component as any).viewState.set('success')`, không `Object.assign`
3. **Router mock**: `const mockRouter = { navigate: vi.fn().mockResolvedValue(true) }` — pattern đã dùng trong homework/live-exam specs
4. **`AllowedActionsFor` helper** phải được gọi trong cả `CreateAsync`, `OpenAsync`, `CloseAsync` của `LiveExamSessionService`
5. **Deserialization C# test**: `ReadFromJsonAsync<HomeworkAssignmentResponse>()` — C# records serialize/deserialize correctly với System.Text.Json

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 3, Story 3.3]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — DTO naming conventions, state machine, allowed actions]
- [Source: `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/HomeworkAssignmentResponse.cs` — current DTO]
- [Source: `src/EnglishTestWeb.Api/Contracts/LiveExamSessions/LiveExamSessionResponse.cs` — current DTO]
- [Source: `src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.ts` — onHomeworkAction/onLiveExamAction thiếu navigation]
- [Source: `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.html` — disabled buttons cần enable]
- [Source: `3-1-create-homeworkassignment-from-a-ready-template.md` — flushPromises, signal test pattern]
- [Source: `3-2-create-and-control-liveexamsession.md` — Angular spec anti-patterns, router mock pattern]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- Thêm `Mode` ("homework"/"live-exam") và `AllowedActions` (computed từ status) vào cả hai backend DTOs
- `AllowedActionsFor` helper trong `LiveExamSessionService` tính đúng actions cho scheduled/open/closed
- 194 API tests pass bao gồm assertions mode + allowedActions cho create/open/close transitions
- Angular interfaces `HomeworkAssignment` và `LiveExamSession` cập nhật với `mode` và `allowedActions`
- Thêm 3 constants: `MODE_LABELS`, `HOMEWORK_STATUS_LABELS`, `LIVE_EXAM_STATUS_LABELS` vào test-templates.models.ts
- Enable navigation trong `onHomeworkAction`/`onLiveExamAction` (trước đây chỉ set blockedActionMessage, không navigate)
- Review page success banner buttons đã enabled với `onGoToHomework()` và `onGoToLiveExam()` handlers
- 104 Angular tests pass (tăng 2 tests: navigation cho homework + live-exam buttons)

### File List

- `src/EnglishTestWeb.Api/Contracts/HomeworkAssignments/HomeworkAssignmentResponse.cs`
- `src/EnglishTestWeb.Api/Contracts/LiveExamSessions/LiveExamSessionResponse.cs`
- `src/EnglishTestWeb.Api/Infrastructure/HomeworkAssignments/HomeworkAssignmentService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/LiveExamSessions/LiveExamSessionService.cs`
- `src/EnglishTestWeb.Client/src/app/core/homework/homework.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/live-exam/live-exam.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates.models.ts`
- `src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.html`
- `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.spec.ts`
- `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/CreateHomeworkAssignmentControllerTests.cs`
- `tests/EnglishTestWeb.Api.Tests/LiveExamSessions/CreateLiveExamSessionControllerTests.cs`
- `tests/EnglishTestWeb.Api.Tests/LiveExamSessions/OpenCloseControllerTests.cs`
- `_bmad-output/implementation-artifacts/3-3-usage-mode-contract-across-delivery-surfaces.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
