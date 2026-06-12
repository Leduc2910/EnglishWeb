---
baseline_commit: 89b76eb
---

# Story 4.1: Danh Sách Bài Thi Được Giao Cho Học Sinh

Status: done

## Story

Là học sinh,
tôi muốn xem các bài tập về nhà và kỳ thi trực tiếp có sẵn trong lớp học của mình,
để tôi có thể chọn đúng bài cần làm mà không cần hỏi giáo viên.

## Acceptance Criteria

1. **Given** học sinh đã đăng nhập và có ClassMembership active
   **When** họ mở `/student/tests`
   **Then** trang hiển thị active class context, tab Bài tập về nhà và Thi trực tiếp, bộ lọc status, bộ lọc kỹ năng, và danh sách bài được giao.

2. **Given** không có Homework hoặc Live Exam nào cho lớp hiện tại
   **When** danh sách tải xong
   **Then** empty state gắn liền với lớp hiện tại (không hiển thị như lỗi chung chung).

3. **Given** Homework đã quá deadline
   **When** học sinh xem item đó
   **Then** status hiển thị trạng thái đã đóng/hết hạn
   **And** nút "Bắt đầu" bị disabled hoặc hiển thị thông báo lỗi phù hợp.

4. **Given** LiveExamSession chưa được mở (status = "scheduled")
   **When** học sinh cố gắng bắt đầu
   **Then** hệ thống hiển thị thông báo lỗi `ERR_LIVE_EXAM_NOT_OPEN`
   **And** không thể bắt đầu làm bài.

5. **Given** học sinh lọc theo mode/status/kỹ năng
   **When** filter thay đổi
   **Then** danh sách cập nhật trong khi vẫn giữ nguyên active Class context.

6. **Given** học sinh gửi request trực tiếp đến assigned item của lớp khác
   **When** API đánh giá scope
   **Then** request bị từ chối server-side.

## Tasks / Subtasks

- [x] Task 1: Backend — AssignedTestItem contract và interface (AC1, AC6)
  - [x] 1.1 Tạo `src/EnglishTestWeb.Api/Contracts/AssignedTests/AssignedTestItem.cs` — record với các field: Id, Mode, Title, Skill, ClassId, ClassName, Status, StudentStatus, DeadlineAt, TimeLimitMinutes, ScheduledStartAt, OpenedAt, ClosedAt, CreatedAt
  - [x] 1.2 Tạo `src/EnglishTestWeb.Api/Application/AssignedTests/IAssignedTestService.cs` — interface `GetForStudentAsync(string studentId, Guid classId, CancellationToken) → Task<IReadOnlyList<AssignedTestItem>>`
  - [x] 1.3 Thêm navigation properties vào `HomeworkAssignment` entity: `TestTemplate? Template` và navigation key `public Guid TestTemplateId` giữ nguyên (chỉ thêm nav prop để `.Include()` hoạt động)
  - [x] 1.4 Thêm navigation properties vào `LiveExamSession` entity tương tự

- [x] Task 2: Backend — Service implementation (AC1, AC3, AC4, AC6)
  - [x] 2.1 Tạo `src/EnglishTestWeb.Api/Infrastructure/AssignedTests/AssignedTestService.cs`
    - Inject `AppDbContext`, `TimeProvider`
    - Query `HomeworkAssignments` WHERE `ClassId = classId` + `.Include(h => h.Template)` + project sang DTO
    - Query `LiveExamSessions` WHERE `ClassId = classId` + `.Include(s => s.Template)` + project sang DTO
    - Tính `StudentStatus`:
      - Homework: deadline >= now → `"available"`, deadline < now → `"expired"`
      - LiveExam scheduled → `"not-open"`, open → `"available"`, closed → `"closed"`
    - Trả về combined list sorted by CreatedAt descending
  - [x] 2.2 Register service trong DI (Program.cs hoặc extension method)
  - [x] 2.3 Thêm EF Core index trên `HomeworkAssignment.ClassId` và `LiveExamSession.ClassId` (trong `AppDbContext` hoặc migration)
  - [x] 2.4 `dotnet ef migrations add AddAssignedTestIndexes` nếu cần, hoặc chỉ thêm index annotation

- [x] Task 3: Backend — Controller và authorization (AC1, AC6)
  - [x] 3.1 Tạo `src/EnglishTestWeb.Api/Controllers/AssignedTestsController.cs`
    - Route: `GET /api/assigned-tests`
    - `[Authorize(Roles = IdentityRoleNames.Student)]`
    - Extract `activeClassId` từ `ICurrentUserContext.ActiveClassId`
    - Nếu `activeClassId == null` → return `[]` (empty list, không phải error — học sinh chưa có lớp)
    - Gọi `IAssignedTestService.GetForStudentAsync`
    - Verify student có membership trong class đó qua `IClassAuthorizationService.RequireStudentClassAccessAsync` trước khi query
    - Return `200 OK` với `{ items: [...] }`
  - [x] 3.2 `dotnet test` — xác nhận tất cả tests hiện có vẫn pass

- [x] Task 4: Backend — API tests (AC1, AC3, AC4, AC6)
  - [x] 4.1 Tạo `tests/EnglishTestWeb.Api.Tests/AssignedTests/AssignedTestsTestHelper.cs` — helper tạo test data: homework + live exam cho class
  - [x] 4.2 Tạo `tests/EnglishTestWeb.Api.Tests/AssignedTests/AssignedTestsControllerTests.cs` với cases:
    - `GetAssignedTests_AsAnonymous_Returns401`
    - `GetAssignedTests_AsTeacher_Returns403`
    - `GetAssignedTests_AsStudent_WithHomework_ReturnsHomeworkItem` (mode="homework", studentStatus="available")
    - `GetAssignedTests_AsStudent_WithExpiredHomework_ReturnsExpiredStatus` (deadline in past, studentStatus="expired")
    - `GetAssignedTests_AsStudent_WithScheduledLiveExam_ReturnsNotOpenStatus` (studentStatus="not-open")
    - `GetAssignedTests_AsStudent_WithOpenLiveExam_ReturnsAvailableStatus` (studentStatus="available")
    - `GetAssignedTests_AsStudent_WithClosedLiveExam_ReturnsClosedStatus` (studentStatus="closed")
    - `GetAssignedTests_AsStudent_EmptyClass_ReturnsEmptyList`
    - `GetAssignedTests_AsStudentFromDifferentClass_ReturnsEmpty` (student has active class A, items are in class B → items from B not visible)
  - [x] 4.3 Thêm `GET /api/assigned-tests` vào `AuthorizationMatrixTests.cs` — unauthenticated → 401, teacher → 403
  - [x] 4.4 `dotnet test` — xác nhận tất cả tests pass

- [x] Task 5: Angular — core service và models (AC1, AC5)
  - [x] 5.1 Tạo `src/EnglishTestWeb.Client/src/app/core/assigned-tests/assigned-tests.models.ts`:
    - Interface `AssignedTestItem` với tất cả fields từ backend DTO
    - Constants `STUDENT_STATUS_LABELS` mapping: `available` → "Đang mở", `not-open` → "Chưa mở", `expired` → "Đã hết hạn", `closed` → "Đã đóng"
    - Constants `ASSIGNED_TEST_ERROR_MESSAGES` với `'ERR_LIVE_EXAM_NOT_OPEN'`
  - [x] 5.2 Tạo `src/EnglishTestWeb.Client/src/app/core/assigned-tests/assigned-tests-api.service.ts`:
    - Method `getForActiveClass(): Promise<AssignedTestItem[]>` gọi `GET /api/assigned-tests`
    - Return `response.items` (array)

- [x] Task 6: Angular — component update (AC1, AC2, AC3, AC4, AC5)
  - [x] 6.1 Cập nhật `features/student-assigned-tests/student-assigned-tests.component.ts`:
    - Inject `AssignedTestsApiService`, `ClassContextService`, `AuthSessionService`, `Router`
    - Signals: `viewState: 'loading' | 'loaded' | 'error'`, `homeworkItems`, `liveExamItems`, `activeTab: 'homework' | 'live-exam'`, `skillFilter`, `statusFilter`, `blockedItemMessage`
    - `ngOnInit()`: load items từ API, phân loại vào `homeworkItems` / `liveExamItems`
    - `filteredHomework` và `filteredLiveExams` computed signals áp dụng filter
    - `onTabChange(tab)`, `onSkillFilter(skill)`, `onStatusFilter(status)`: update filter signals
    - `onStartItem(item: AssignedTestItem)`: kiểm tra availability; nếu item.studentStatus = 'not-open' → set `blockedItemMessage = ASSIGNED_TEST_ERROR_MESSAGES['ERR_LIVE_EXAM_NOT_OPEN']`; nếu status = 'expired' hoặc 'closed' → set message tương ứng; nếu available → navigate (placeholder cho story 4.2: `router.navigate(['/student/workspace', item.id])`)
    - `logout()`: giữ nguyên như hiện tại
  - [x] 6.2 Cập nhật `features/student-assigned-tests/student-assigned-tests.component.html`:
    - Header: class name + user info + logout (giữ nguyên, clean up placeholder text)
    - Loading state
    - Error state với retry
    - Tab bar: "Bài tập về nhà" / "Thi trực tiếp" với active indicator
    - Filter row: skill filter (all/reading/listening/speaking) + status filter (all/available/not-open/expired/closed)
    - Danh sách items (dùng `@for` loop):
      - Card mỗi item: title, skill badge, status badge (dùng `studentStatus` + `STUDENT_STATUS_LABELS`), deadline/schedule info, nút "Bắt đầu"
      - Nút "Bắt đầu" disabled nếu `item.studentStatus !== 'available'`
      - `blockedItemMessage` hiển thị inline khi có
    - Empty state: message gắn với class name hiện tại
  - [x] 6.3 Cập nhật CSS cho tabs, cards, filter row, status badges

- [x] Task 7: Angular — component spec (AC1, AC2, AC3, AC4, AC5)
  - [x] 7.1 Tạo `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.spec.ts`:
    - Mock `AssignedTestsApiService` với `vi.fn().mockResolvedValue()`
    - Test `tải được danh sách và phân loại vào hai tab`
    - Test `empty state hiển thị đúng khi không có bài`
    - Test `homework expired → nút bị disabled`
    - Test `live exam scheduled → click Bắt đầu → hiển thị ERR_LIVE_EXAM_NOT_OPEN message`
    - Test `filter theo skill → chỉ hiển thị items khớp skill`
    - Test `filter theo status → chỉ hiển thị items khớp studentStatus`
  - [x] 7.2 `npm test` — xác nhận tất cả tests pass (tăng từ số hiện tại)

## Dev Notes

### Backend: Endpoint Design

**Route:** `GET /api/assigned-tests`
**Auth:** `[Authorize(Roles = "Student")]` — không dùng Policy vì không cần resource-specific scope check upfront

**Lấy classId từ claim** (không từ query param — tránh IDOR):
```csharp
var activeClassId = _currentUser.ActiveClassId;
if (activeClassId == null) return Ok(new { items = Array.Empty<AssignedTestItem>() });
```

**Authorization flow:** Gọi `IClassAuthorizationService.RequireStudentClassAccessAsync(activeClassId, studentId)` để confirm membership còn active. Nếu denied → return 404 ẩn (theo pattern của project). Trong thực tế, active_class_id claim đã được verify lúc login, nhưng cần re-check để bảo vệ khỏi stale claim (xem deferred note từ story 1.4).

**AssignedTestItem record:**
```csharp
namespace EnglishTestWeb.Api.Contracts.AssignedTests;

public sealed record AssignedTestItem(
    Guid Id,
    string Mode,               // "homework" | "live-exam"
    string Title,              // template title
    string Skill,              // "reading" | "listening" | "speaking"
    Guid ClassId,
    string ClassName,
    string Status,             // raw assignment/session status
    string StudentStatus,      // computed: "available" | "not-open" | "expired" | "closed"
    DateTimeOffset? DeadlineAt,
    int? TimeLimitMinutes,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt);
```

**StudentStatus computation:**
```csharp
// Homework:
string studentStatus = assignment.DeadlineAt >= now ? "available" : "expired";

// LiveExam:
string studentStatus = session.Status switch {
    LiveExamSessionStatuses.Scheduled => "not-open",
    LiveExamSessionStatuses.Open => "available",
    LiveExamSessionStatuses.Closed => "closed",
    _ => "closed"
};
```

**Navigation properties cần thêm** (không thay đổi schema, chỉ EF mapping):
```csharp
// HomeworkAssignment.cs — thêm:
public TestTemplate? Template { get; set; }

// LiveExamSession.cs — thêm:
public TestTemplate? Template { get; set; }
```

Phải configure trong `AppDbContext.OnModelCreating()` nếu chưa có FK config. Kiểm tra `AppDbContext.cs` xem ForeignKey đã được configure chưa trước khi thêm.

**DI registration** — theo pattern của project (scoped service):
```csharp
services.AddScoped<IAssignedTestService, AssignedTestService>();
```
Tìm nơi register các services khác (HomeworkAssignmentService, LiveExamSessionService) để thêm vào cùng chỗ.

**EF Index** — thêm vào `AppDbContext.OnModelCreating` hoặc dùng `[Index]` attribute:
```csharp
modelBuilder.Entity<HomeworkAssignment>().HasIndex(h => h.ClassId);
modelBuilder.Entity<LiveExamSession>().HasIndex(s => s.ClassId);
```
Sau đó chạy `dotnet ef migrations add AddAssignedTestClassIdIndexes`.

### Backend: Test Patterns

Dùng pattern từ `CreateHomeworkAssignmentControllerTests.cs`:

```csharp
// Setup: tạo homework và live exam trong class của student
var factory = new TestApiFactory();
var client = factory.CreateClient();
await AuthTestHelper.SeedRolesAndUsersAsync(factory);

// Sign in as student với active class
var studentClient = await AuthTestHelper.SignInStudentAsync(client, factory, activeClassId: classId);

// Call GET /api/assigned-tests
var response = await studentClient.GetAsync("/api/assigned-tests");
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
var body = await response.Content.ReadFromJsonAsync<AssignedTestsResponse>();
Assert.NotEmpty(body!.Items);
var item = body.Items.Single(i => i.Mode == "homework");
Assert.Equal("available", item.StudentStatus);
```

**`AssignedTestsResponse` wrapper:**
```csharp
public sealed record AssignedTestsResponse(IReadOnlyList<AssignedTestItem> Items);
```

**Kiểm tra `AuthTestHelper.SignInStudentAsync`** để xem signature hiện tại. Nếu chưa có overload nhận `activeClassId`, có thể cần dùng `SignInUserAsync` trực tiếp. Xem `AuthTestHelper.cs` và `StudentLoginTests.cs` để hiểu pattern đầy đủ.

**Teacher → 403**: Teacher gọi `GET /api/assigned-tests` phải trả 403 (không phải 401) vì họ đã authenticated nhưng sai role.

**Student từ class khác**: Setup student A trong class A, tạo homework trong class B. Student A gọi `GET /api/assigned-tests` → class A active → chỉ thấy items của class A. Items của class B không visible.

### Angular: Models

```typescript
// assigned-tests.models.ts
export interface AssignedTestItem {
  id: string;
  mode: 'homework' | 'live-exam';
  title: string;
  skill: string;
  classId: string;
  className: string;
  status: string;
  studentStatus: 'available' | 'not-open' | 'expired' | 'closed';
  deadlineAt: string | null;
  timeLimitMinutes: number | null;
  scheduledStartAt: string | null;
  openedAt: string | null;
  closedAt: string | null;
  createdAt: string;
}

export const STUDENT_STATUS_LABELS: Record<string, string> = {
  available: 'Đang mở',
  'not-open': 'Chưa mở',
  expired: 'Đã hết hạn',
  closed: 'Đã đóng',
};

export const ASSIGNED_TEST_ERROR_MESSAGES: Record<string, string> = {
  ERR_LIVE_EXAM_NOT_OPEN: 'Kỳ thi chưa được mở. Vui lòng chờ giáo viên mở phiên thi.',
  ERR_HOMEWORK_EXPIRED: 'Bài tập đã hết hạn nộp.',
  ERR_ITEM_CLOSED: 'Bài thi đã đóng.',
};
```

### Angular: Component Pattern

Dùng **signal-based pattern** nhất quán với các components khác (xem `homework-create.component.ts`):

```typescript
// Core signals
protected readonly viewState = signal<'loading' | 'loaded' | 'error'>('loading');
protected readonly allItems = signal<AssignedTestItem[]>([]);
protected readonly activeTab = signal<'homework' | 'live-exam'>('homework');
protected readonly skillFilter = signal<string>('all');
protected readonly statusFilter = signal<string>('all');
protected readonly blockedItemMessage = signal<string | null>(null);

// Computed — phân loại
protected readonly homeworkItems = computed(() =>
  this.allItems().filter(i => i.mode === 'homework')
);
protected readonly liveExamItems = computed(() =>
  this.allItems().filter(i => i.mode === 'live-exam')
);

// Computed — filtered display list
protected readonly filteredHomework = computed(() => {
  let items = this.homeworkItems();
  if (this.skillFilter() !== 'all') items = items.filter(i => i.skill === this.skillFilter());
  if (this.statusFilter() !== 'all') items = items.filter(i => i.studentStatus === this.statusFilter());
  return items;
});
// tương tự filteredLiveExams
```

**`onStartItem` — placeholder cho story 4.2:**
```typescript
protected onStartItem(item: AssignedTestItem): void {
  if (item.studentStatus !== 'available') {
    if (item.studentStatus === 'not-open') {
      this.blockedItemMessage.set(ASSIGNED_TEST_ERROR_MESSAGES['ERR_LIVE_EXAM_NOT_OPEN']);
    } else if (item.studentStatus === 'expired') {
      this.blockedItemMessage.set(ASSIGNED_TEST_ERROR_MESSAGES['ERR_HOMEWORK_EXPIRED']);
    } else {
      this.blockedItemMessage.set(ASSIGNED_TEST_ERROR_MESSAGES['ERR_ITEM_CLOSED']);
    }
    return;
  }
  this.blockedItemMessage.set(null);
  // Story 4.2 sẽ thêm navigate to workspace
  // void this.router.navigate(['/student/workspace', item.id]);
}
```

**KHÔNG** navigate đến workspace trong story này — placeholder comment là đủ.

### Angular: Template HTML Pattern

Tham khảo `homework-create.component.html` cho loading/error state pattern. Dùng `@if` / `@for` (Angular 17+ control flow syntax). KHÔNG dùng `*ngIf` / `*ngFor`.

Stable element IDs cho testing:
- `id="assigned-tests-tab-homework"` — tab Bài tập về nhà
- `id="assigned-tests-tab-live-exam"` — tab Thi trực tiếp
- `id="assigned-tests-skill-filter"` — skill filter select
- `id="assigned-tests-status-filter"` — status filter select
- `id="assigned-tests-blocked-message"` — error message khi item bị blocked
- Mỗi item card: `data-testid="assigned-test-item-{{item.id}}"` hoặc dùng index

### Angular: Test Pattern

Dùng **Vitest** (KHÔNG dùng Karma). Tham khảo `test-template-review.component.spec.ts` và `homework-create.component.spec.ts` cho patterns:

```typescript
// Mock service
const mockApiService = {
  getForActiveClass: vi.fn().mockResolvedValue([
    { id: 'hw-1', mode: 'homework', title: 'Test Bài Reading', skill: 'reading',
      studentStatus: 'available', deadlineAt: '2030-01-01T00:00:00Z', ... }
  ])
};

// Setup
await TestBed.configureTestingModule({
  imports: [StudentAssignedTestsComponent],
  providers: [
    { provide: AssignedTestsApiService, useValue: mockApiService },
    { provide: ClassContextService, useValue: { activeClass: signal({ className: 'Lớp 7A', classId: 'cls-1' }) } },
    { provide: AuthSessionService, useValue: { currentUser: signal({ userName: 'student1' }) } },
    { provide: Router, useValue: { navigate: vi.fn().mockResolvedValue(true) } },
  ]
}).compileComponents();

// flushPromises() pattern (không dùng fixture.whenStable())
import { flushPromises } from '@vue/test-utils'; // hoặc custom implementation
```

**`flushPromises` import**: Kiểm tra cách import trong test files hiện có (e.g. `3-1` spec). Đây là pattern quan trọng — dùng `await flushPromises()` sau `fixture.detectChanges()` để flush async operations thay vì `fixture.whenStable()`.

### Files Being Created/Modified

**API (new):**
- `src/EnglishTestWeb.Api/Contracts/AssignedTests/AssignedTestItem.cs`
- `src/EnglishTestWeb.Api/Application/AssignedTests/IAssignedTestService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/AssignedTests/AssignedTestService.cs`
- `src/EnglishTestWeb.Api/Controllers/AssignedTestsController.cs`

**API (update):**
- `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignment.cs` — thêm nav prop
- `src/EnglishTestWeb.Api/Domain/LiveExams/LiveExamSession.cs` — thêm nav prop
- `src/EnglishTestWeb.Api/Infrastructure/Data/AppDbContext.cs` — thêm index config (kiểm tra tên file chính xác)
- Program.cs hoặc service registration file — thêm DI registration
- `src/EnglishTestWeb.Api/Migrations/` — migration mới nếu cần

**Tests (new):**
- `tests/EnglishTestWeb.Api.Tests/AssignedTests/AssignedTestsTestHelper.cs`
- `tests/EnglishTestWeb.Api.Tests/AssignedTests/AssignedTestsControllerTests.cs`

**Tests (update):**
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — thêm `GET /api/assigned-tests`

**Angular (new):**
- `src/EnglishTestWeb.Client/src/app/core/assigned-tests/assigned-tests.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/assigned-tests/assigned-tests-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.spec.ts`

**Angular (update):**
- `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.html`
- `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.css`

### Architecture Compliance

- **Controller không access DbContext** — delegate hoàn toàn sang `IAssignedTestService`
- **ClassId từ claim** — không từ query param/body (IDOR prevention)
- **Student membership re-check** — gọi `IClassAuthorizationService` trước khi query (xem pattern trong `ClassesController`)
- **`mode` là string constant** — không serialize enum int, nhất quán với `HomeworkAssignmentService` ("homework") và `LiveExamSessionService` ("live-exam")
- **Response wrapper** `{ items: [...] }` — nhất quán với pagination pattern trong project (có `items` field)
- **Angular: không có localStorage** — class context từ `ClassContextService.activeClass()` signal
- **Angular: signal-based state** — không dùng BehaviorSubject/Observable trực tiếp trong component

### Anti-Patterns

- **KHÔNG** nhận `classId` từ query param hoặc request body — phải lấy từ `ICurrentUserContext.ActiveClassId`
- **KHÔNG** query homework/live exams không có ClassId filter — mỗi query phải có `WHERE ClassId = @classId`
- **KHÔNG** tạo combined "AssignedWork" entity trong database — chỉ JOIN/query hai bảng riêng biệt trong service
- **KHÔNG** dùng `fixture.whenStable()` trong Vitest — dùng `flushPromises()` pattern
- **KHÔNG** implement navigate-to-workspace trong story này — đó là story 4.2
- **KHÔNG** thêm submission status (graded/needs-grading) — submissions chưa tồn tại (story 4.2-4.4); chỉ dùng assignment/session status
- **KHÔNG** hardcode mock list trong component — phải gọi API thực

### Context từ Previous Stories

1. **`flushPromises()` pattern** — dùng trong tất cả Angular tests async (từ story 3.1)
2. **Signal set trong test**: `(component as any).viewState.set('loaded')` — không `Object.assign`
3. **`[Authorize(Roles = ...)]` trực tiếp** — không dùng Policy cho role-only check (teacher endpoints dùng Policy vì cần resource scope)
4. **Navigation properties chưa có** — `HomeworkAssignment` và `LiveExamSession` chưa có nav props; cần thêm TRƯỚC KHI viết Service (xem deferred từ story 3.1: "HomeworkAssignment không có navigation properties — thêm khi có list endpoint")
5. **`IClassAuthorizationService.RequireStudentClassAccessAsync`** — check membership active, trả `AuthorizationDecision`; convert sang ActionResult bằng `IHiddenResourceResponseFactory` (xem `ClassesController.cs` pattern)
6. **Test seed helper pattern** — `AssignedTestsTestHelper` nên follow `HomeworkAssignmentTestHelper.cs` pattern (static methods, `EnsureXxx`)
7. **`TimeProvider`** — dùng `TimeProvider` (không `DateTime.UtcNow`) để tính deadline comparison (injectable, testable)

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 4, Story 4.1]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — Student endpoint patterns, Authorization, DTO naming]
- [Source: `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignment.cs` — entity hiện tại (không có nav props)]
- [Source: `src/EnglishTestWeb.Api/Domain/LiveExams/LiveExamSession.cs` — entity hiện tại (không có nav props)]
- [Source: `src/EnglishTestWeb.Api/Domain/LiveExams/LiveExamSessionStatuses.cs` — "scheduled"/"open"/"closed"]
- [Source: `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignmentStatuses.cs` — "published"]
- [Source: `src/EnglishTestWeb.Api/Controllers/HomeworkAssignmentsController.cs` — teacher controller pattern]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Security/ClassAuthorizationService.cs` — `RequireStudentClassAccessAsync` pattern]
- [Source: `src/EnglishTestWeb.Api/Application/Security/ICurrentUserContext.cs` — `ActiveClassId` claim]
- [Source: `tests/EnglishTestWeb.Api.Tests/HomeworkAssignments/CreateHomeworkAssignmentControllerTests.cs` — test patterns]
- [Source: `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — auth matrix pattern]
- [Source: `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.ts` — current placeholder]
- [Source: `src/EnglishTestWeb.Client/src/app/core/homework/homework.models.ts` — HomeworkAssignment model với mode/allowedActions]
- [Source: `src/EnglishTestWeb.Client/src/app/core/live-exam/live-exam.models.ts` — LiveExamSession model]
- [Source: `_bmad-output/implementation-artifacts/3-3-usage-mode-contract-across-delivery-surfaces.md` — mode constants, STUDENT_STATUS_LABELS pattern]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md` — nav props defer (3-1), no index defer (3-1), no GET endpoints defer (3-3)]

## Review Findings

- [x] [Review][Dismiss] Empty migration Up()/Down() — false positive; ClassId indexes were already created by FK auto-index in AddHomeworkAssignments/AddLiveExamSessions migrations; new migration correctly empty (only updates nav prop name in snapshot)
- [x] [Review][Patch] Missing stable IDs in HTML template — FIXED: added id="assigned-tests-tab-homework", id="assigned-tests-tab-live-exam", id="assigned-tests-skill-filter", id="assigned-tests-status-filter", id="assigned-tests-blocked-message" [student-assigned-tests.component.html]
- [x] [Review][Patch] Missing error message constants ERR_HOMEWORK_EXPIRED and ERR_ITEM_CLOSED; hardcoded string in onStartItem() — FIXED: added constants to assigned-tests.models.ts; updated onStartItem() to use them [assigned-tests.models.ts + student-assigned-tests.component.ts]
- [x] [Review][Patch] onStartItem() navigates to workspace — FIXED: navigation commented out as placeholder per spec [student-assigned-tests.component.ts]
- [x] [Review][Patch] No test for null activeClassId — FIXED: added GetAssignedTests_AsStudent_WithNoActiveClass_ReturnsEmptyList [AssignedTestsControllerTests.cs]
- [x] [Review][Defer] HomeworkAssignment with default(DateTimeOffset) deadline evaluates to "expired" permanently [AssignedTestService.cs – homeworkItems query] — deferred, pre-existing
- [x] [Review][Defer] studentId parameter accepted by service but never used in queries (all filtering is by classId only) — deferred, by spec design; document when per-student filtering is added
- [x] [Review][Defer] Unknown LiveExamSession status silently collapses to "closed" — deferred, acceptable most-restrictive fallback
- [x] [Review][Defer] onStartItem: unknown studentStatus falls through to router.navigate [student-assigned-tests.component.ts:85] — deferred, values are backend-controlled
- [x] [Review][Defer] Two separate sequential DB round-trips instead of UNION — deferred, performance optimization
- [x] [Review][Defer] AssignedTestItem.Status exposes raw internal domain status strings without stable contract — deferred, by spec design
- [x] [Review][Defer] Non-deterministic sort order when multiple items share identical CreatedAt timestamp — deferred, low-impact edge case
- [x] [Review][Defer] Orphaned FK rows (deleted TestTemplate or Class) silently dropped from results by INNER JOIN — deferred, FK constraints prevent this in practice
- [x] [Review][Defer] Invalid studentId (non-existent user) passes whitespace guard; service ignores studentId anyway — deferred, Identity rejects invalid tokens upstream
- [x] [Review][Defer] Angular: concurrent rapid API requests can overwrite list with stale data — deferred, no pagination/cancellation scope

### Review Round 2 Findings (2026-06-12)

- [x] [Review][Dismiss] Empty-state missing confirmedClass() fallback — false positive; both paragraphs already use `activeClass()?.className ?? confirmedClass()?.className`
- [x] [Review][Dismiss] null activeClassId returns 200 OK before auth check — by spec design; spec explicitly states return empty list when no active class
- [x] [Review][Dismiss] $any($event.target).value in template — accepted Angular workaround; type assertion syntax not valid in Angular templates
- [x] [Review][Dismiss] Error message keys (ERR_*) not keyed by studentStatus — by design; homework/live-exam need different wording for same terminal status
- [x] [Review][Dismiss] Stale claim + revoked membership — handled; RequireStudentClassAccessAsync live-revalidates membership
- [x] [Review][Dismiss] WithNoActiveClass test doesn't actually exercise null branch — false positive; SeedRolesAndUsersAsync creates users only, no class memberships
- [x] [Review][Defer] Fragile positional 14-arg record constructor in LINQ projection [AssignedTestService.cs] — deferred, established pattern; refactor when adding fields
- [x] [Review][Defer] onStartItem('available') clears blockedItemMessage but navigates nowhere — deferred, intentional Story 4.2 placeholder
- [x] [Review][Defer] logout() async errors swallowed by template event binding — deferred, low-risk UX
- [x] [Review][Defer] CSS class interpolation `status-{{ item.studentStatus }}` — deferred, backend-controlled TypeScript union
- [x] [Review][Defer] SeedRolesAndUsersAsync called redundantly in multiple test helpers — deferred, idempotent; refactor when adding IAsyncLifetime

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

### File List
