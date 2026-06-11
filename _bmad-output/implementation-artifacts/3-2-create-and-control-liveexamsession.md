---
baseline_commit: 9e6c2f1
---

# Story 3.2: Create And Control LiveExamSession

Status: done

## Story

Là giáo viên,
tôi muốn tạo một phiên thi trực tiếp (Live Exam) từ một đề gốc Ready và tự tay mở / đóng phiên đó,
để học sinh chỉ có thể bắt đầu làm bài khi phiên được cho phép.

## Acceptance Criteria

1. **Given** giáo viên chọn "Tạo thi trực tiếp" từ một template Ready
   **When** màn hình tạo Live Exam mở ra
   **Then** route `/teacher/live-exams/new?templateId={templateId}` render
   **And** form hiển thị: thông tin template nguồn (title, skill), class select, optional scheduled start/end display fields, nút tạo, nút hủy, và mode label "Thi trực tiếp".

2. **Given** không có WDS page spec riêng cho Live Exam creation/control
   **When** bắt đầu implementation
   **Then** feature contract định nghĩa stable object IDs cho: source-template summary, class select, schedule display fields, create action, open action, close action, status badge, validation errors, và success/conflict states.

3. **Given** dữ liệu Live Exam hợp lệ
   **When** giáo viên tạo session
   **Then** LiveExamSession tham chiếu đúng một Ready TestTemplate và một Class
   **And** trạng thái ban đầu là `scheduled` (chưa mở), không tự mở.

4. **Given** LiveExamSession tồn tại và đang ở trạng thái `scheduled`
   **When** giáo viên mở phiên (open)
   **Then** status chuyển thành `open` đúng một lần
   **And** audit ghi nhận previous state, next state, actor, timestamp.

5. **Given** LiveExamSession đang ở trạng thái `open`
   **When** giáo viên đóng phiên (close)
   **Then** status chuyển thành `closed`
   **And** audit ghi nhận previous state, next state, actor, timestamp.

6. **Given** duplicate open hoặc close request được gửi
   **When** API xử lý
   **Then** trả về 409 Conflict với error code xác định (`liveExam.alreadyOpen`, `liveExam.alreadyClosed`, `liveExam.sessionClosed`, `liveExam.sessionNotOpen`)
   **And** audit không ghi nhận transition kép.

7. **Given** scheduled fields có trong MVP
   **When** scheduled time đến
   **Then** hệ thống KHÔNG tự mở session
   **And** UI copy giải thích rõ teacher phải mở thủ công.

## Tasks / Subtasks

- [ ] Task 1: Backend domain entity + EF migration (AC3)
  - [ ] 1.1 Tạo `Domain/LiveExams/LiveExamSession.cs` — fields: Id, TeacherId, TestTemplateId, ClassId, Status, ScheduledStartAt?, ScheduledEndAt?, OpenedAt?, ClosedAt?, CreatedAt, UpdatedAt
  - [ ] 1.2 Tạo `Domain/LiveExams/LiveExamSessionStatuses.cs` — constants: `scheduled`, `open`, `closed`
  - [ ] 1.3 Tạo `Infrastructure/Persistence/Configurations/LiveExamSessionConfiguration.cs` — table "LiveExamSessions", FK Restrict, index (TeacherId, ClassId), index (TeacherId, TestTemplateId), Status MaxLength 32
  - [ ] 1.4 Thêm `DbSet<LiveExamSession> LiveExamSessions` vào `EnglishTestWebDbContext.cs`
  - [ ] 1.5 Chạy migration: `dotnet ef migrations add AddLiveExamSessions --project src/EnglishTestWeb.Api`
  - [ ] 1.6 `dotnet build` — xác nhận build pass

- [ ] Task 2: Backend Create endpoint (AC1, AC3, AC7)
  - [ ] 2.1 Tạo `Application/LiveExamSessions/ILiveExamSessionService.cs` — result records + interface với `CreateAsync`, `OpenAsync`, `CloseAsync`
  - [ ] 2.2 Tạo `Contracts/LiveExamSessions/CreateLiveExamSessionRequest.cs` — TemplateId, ClassId, ScheduledStartAt?, ScheduledEndAt?
  - [ ] 2.3 Tạo `Contracts/LiveExamSessions/LiveExamSessionResponse.cs` — Id, TemplateId, TemplateTitle, TemplateSkill, ClassId, ClassName, Status, ScheduledStartAt?, ScheduledEndAt?, OpenedAt?, ClosedAt?, CreatedAt
  - [ ] 2.4 Implement `Infrastructure/LiveExamSessions/LiveExamSessionService.CreateAsync`: template ownership → template Ready check → class ownership → class Active check → save với status=scheduled → audit log → return response
  - [ ] 2.5 Tạo `Controllers/LiveExamSessionsController.cs` với `[Authorize(Roles = Teacher)] POST /api/live-exam-sessions`
  - [ ] 2.6 Register `builder.Services.AddScoped<ILiveExamSessionService, LiveExamSessionService>()` trong `Program.cs`
  - [ ] 2.7 API tests: Create cases (xem Testing Requirements)
  - [ ] 2.8 `dotnet test` — xác nhận tất cả tests pass

- [ ] Task 3: Backend Open / Close transitions (AC4, AC5, AC6)
  - [ ] 3.1 Implement `LiveExamSessionService.OpenAsync`: load session by id + teacher check (404 nếu không tìm thấy/không phải của teacher) → check status: nếu `open` → 409 alreadyOpen; nếu `closed` → 409 sessionClosed; nếu `scheduled` → transition + audit → return updated response
  - [ ] 3.2 Implement `LiveExamSessionService.CloseAsync`: load session by id + teacher check → check status: nếu `closed` → 409 alreadyClosed; nếu `scheduled` → 409 sessionNotOpen; nếu `open` → transition + audit → return updated response
  - [ ] 3.3 Thêm `POST /api/live-exam-sessions/{id}/open` và `POST /api/live-exam-sessions/{id}/close` vào controller
  - [ ] 3.4 API tests: Open/Close transition cases (xem Testing Requirements)
  - [ ] 3.5 Thêm authorization matrix rows trong `AuthorizationMatrixTests.cs`
  - [ ] 3.6 `dotnet test` — xác nhận tất cả tests pass

- [ ] Task 4: Angular create + control UI (AC1, AC2, AC4, AC5, AC7)
  - [ ] 4.1 Tạo `core/live-exam/live-exam.models.ts` — interfaces + LIVE_EXAM_ERROR_MESSAGES + mapLiveExamError
  - [ ] 4.2 Tạo `core/live-exam/live-exam-api.service.ts` — `create()`, `open(id)`, `close(id)`
  - [ ] 4.3 Tạo `features/live-exam-create/live-exam-create.component.ts` — viewState, session control signals, loadPage (queryParamMap), onCreate, onOpen, onClose, onCancel
  - [ ] 4.4 Tạo `features/live-exam-create/live-exam-create.component.html` — stable object IDs, status badge, conditional open/close buttons
  - [ ] 4.5 Tạo `features/live-exam-create/live-exam-create.component.css`
  - [ ] 4.6 Tạo `features/live-exam-create/live-exam-create.component.spec.ts` — 10+ test cases
  - [ ] 4.7 Cập nhật `app.routes.ts` — thay placeholder `TeacherPlaceholderComponent` bằng `LiveExamCreateComponent` cho route `live-exams/new`
  - [ ] 4.8 `npm test` — xác nhận tất cả Angular tests pass

### Review Findings

- [x] [Review][Patch] OpenAsync / CloseAsync thiếu try/catch trên SaveChangesAsync — unhandled DbUpdateException không trả structured error code [Infrastructure/LiveExamSessions/LiveExamSessionService.cs]
- [x] [Review][Patch] LoadRelatedEntitiesAsync dùng FirstAsync thay vì FirstOrDefaultAsync — throws InvalidOperationException nếu FK row bị mất [Infrastructure/LiveExamSessions/LiveExamSessionService.cs]
- [x] [Review][Patch] Thiếu student 403 tests cho Open và Close endpoints — chỉ Create có student test [tests/EnglishTestWeb.Api.Tests/LiveExamSessions/OpenCloseControllerTests.cs]
- [x] [Review][Defer] TOCTOU double-lookup auth pattern (template auth → re-fetch template) — project-wide established pattern; refactor khi extract shared auth helper
- [x] [Review][Defer] Không có optimistic concurrency token cho concurrent open/close — no [ConcurrencyCheck] trên Status; race condition dưới concurrent load; add khi implement submission pipeline
- [x] [Review][Defer] datetime-local parsed không có timezone explicit — browser behavior; low practical risk cho single-timezone MVP
- [x] [Review][Defer] scheduledEndAt < scheduledStartAt không được validate — spec không yêu cầu temporal ordering; add khi có scheduling story
- [x] [Review][Defer] Session không refresh sau transition error — local status stale; UX polish; add re-fetch khi có error-telemetry story
- [x] [Review][Defer] Multiple open sessions per class không bị chặn — design decision; spec không require uniqueness constraint; clarify khi có student view story
- [x] [Review][Defer] Cross-teacher Open/Close test thiếu real second-teacher fixture — consistent với 3-1 defer; add khi có second-teacher test helper infrastructure

## Dev Notes

### Domain Entity Design

```csharp
// Domain/LiveExams/LiveExamSession.cs
public sealed class LiveExamSession
{
    public Guid Id { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public Guid TestTemplateId { get; set; }
    public Guid ClassId { get; set; }
    public string Status { get; set; } = LiveExamSessionStatuses.Scheduled;
    public DateTimeOffset? ScheduledStartAt { get; set; }
    public DateTimeOffset? ScheduledEndAt { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// Domain/LiveExams/LiveExamSessionStatuses.cs
public static class LiveExamSessionStatuses
{
    public const string Scheduled = "scheduled";
    public const string Open = "open";
    public const string Closed = "closed";
}
```

### Service Interface Design

```csharp
// Application/LiveExamSessions/ILiveExamSessionService.cs
using EnglishTestWeb.Api.Contracts.LiveExamSessions;

namespace EnglishTestWeb.Api.Application.LiveExamSessions;

public sealed record CreateLiveExamSessionResult(
    bool Allowed,
    LiveExamSessionResponse? Detail,
    string? ErrorCode,
    int StatusCode);

public sealed record LiveExamSessionTransitionResult(
    bool Allowed,
    LiveExamSessionResponse? Detail,
    string? ErrorCode,
    int StatusCode);

public interface ILiveExamSessionService
{
    Task<CreateLiveExamSessionResult> CreateAsync(
        string teacherId,
        CreateLiveExamSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<LiveExamSessionTransitionResult> OpenAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<LiveExamSessionTransitionResult> CloseAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
```

### Service Implementation Pattern

**CreateAsync** — theo đúng thứ tự validation của HomeworkAssignmentService (Story 3.1):
1. Template ownership check (ITemplateAuthorizationService → 404 `liveExam.templateNotFound`)
2. Template Ready check (status != "ready" → 400 `liveExam.templateNotReady`)
3. Class ownership check (IClassAuthorizationService → 404 `liveExam.classNotFound`)
4. Class Active check (status != "active" → 400 `liveExam.classNotActive`)
5. Create + SaveChanges
6. Audit log sau SaveChanges: `"LiveExamSessionCreated: sessionId={SessionId} templateId={TemplateId} classId={ClassId} teacherId={TeacherId} status={Status} at={Timestamp}"`
7. Return response

**OpenAsync / CloseAsync** — pattern:
```csharp
public async Task<LiveExamSessionTransitionResult> OpenAsync(
    string teacherId, Guid sessionId, CancellationToken cancellationToken = default)
{
    var session = await dbContext.LiveExamSessions
        .FirstOrDefaultAsync(s => s.Id == sessionId && s.TeacherId == teacherId, cancellationToken);

    if (session is null)
        return new LiveExamSessionTransitionResult(false, null, "liveExam.sessionNotFound", StatusCodes.Status404NotFound);

    if (string.Equals(session.Status, LiveExamSessionStatuses.Open, StringComparison.Ordinal))
        return new LiveExamSessionTransitionResult(false, null, "liveExam.alreadyOpen", StatusCodes.Status409Conflict);

    if (string.Equals(session.Status, LiveExamSessionStatuses.Closed, StringComparison.Ordinal))
        return new LiveExamSessionTransitionResult(false, null, "liveExam.sessionClosed", StatusCodes.Status409Conflict);

    // status == "scheduled" → transition
    var previousStatus = session.Status;
    var now = DateTimeOffset.UtcNow;
    session.Status = LiveExamSessionStatuses.Open;
    session.OpenedAt = now;
    session.UpdatedAt = now;

    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation(
        "LiveExamSessionOpened: sessionId={SessionId} teacherId={TeacherId} previousStatus={PreviousStatus} newStatus={NewStatus} at={Timestamp}",
        session.Id, teacherId, previousStatus, session.Status, now);

    // Load template + class for response
    var template = await dbContext.TestTemplates.AsNoTracking()
        .FirstOrDefaultAsync(t => t.Id == session.TestTemplateId, cancellationToken);
    var schoolClass = await dbContext.Classes.AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == session.ClassId, cancellationToken);

    return new LiveExamSessionTransitionResult(true, MapResponse(session, template!, schoolClass!), null, StatusCodes.Status200OK);
}
```

CloseAsync mirrors OpenAsync với checks: `open` → transition to `closed`; `closed` → 409 `alreadyClosed`; `scheduled` → 409 `sessionNotOpen`.

### ProblemDetails Error Codes

| Code | HTTP | Khi nào |
|---|---|---|
| `liveExam.templateNotFound` | 404 | Template không tồn tại hoặc không phải của teacher |
| `liveExam.templateNotReady` | 400 | Template không ở trạng thái Ready |
| `liveExam.classNotFound` | 404 | Class không tồn tại hoặc không phải của teacher |
| `liveExam.classNotActive` | 400 | Class không active |
| `liveExam.createFailed` | 500 | DbUpdateException khi save |
| `liveExam.sessionNotFound` | 404 | Session không tồn tại hoặc không phải của teacher |
| `liveExam.alreadyOpen` | 409 | Gọi /open khi session đã open |
| `liveExam.sessionClosed` | 409 | Gọi /open khi session đã closed |
| `liveExam.alreadyClosed` | 409 | Gọi /close khi session đã closed |
| `liveExam.sessionNotOpen` | 409 | Gọi /close khi session đang scheduled |
| `liveExam.transitionFailed` | 500 | DbUpdateException khi save transition |

### Controller Design

```csharp
[ApiController]
[Route("api/live-exam-sessions")]
public sealed class LiveExamSessionsController(
    ILiveExamSessionService liveExamSessionService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateLiveExamSessionRequest request,
        CancellationToken cancellationToken) { ... returns StatusCode(201, result.Detail) }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost("{id:guid}/open")]
    public async Task<ActionResult> Open(Guid id, CancellationToken cancellationToken) { ... }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult> Close(Guid id, CancellationToken cancellationToken) { ... }
}
```

Open/Close trả 200 với updated `LiveExamSessionResponse` khi thành công; dùng `hiddenResourceResponseFactory.FromCode` khi fail (404/409/500).

### EF Configuration Pattern

```csharp
// Mirror HomeworkAssignmentConfiguration:
builder.ToTable("LiveExamSessions");
builder.HasKey(e => e.Id);
builder.Property(e => e.TeacherId).HasMaxLength(450).IsRequired();
builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
builder.Property(e => e.CreatedAt).IsRequired();
builder.Property(e => e.UpdatedAt).IsRequired();
// Nullable DateTimeOffset fields: ScheduledStartAt, ScheduledEndAt, OpenedAt, ClosedAt
builder.HasIndex(e => new { e.TeacherId, e.ClassId });
builder.HasIndex(e => new { e.TeacherId, e.TestTemplateId });
// FK Restrict cho Teacher, TestTemplate, SchoolClass
```

### Angular Component Architecture

**ViewState pattern (mirror HomeworkCreateComponent — học từ Story 3.1):**

```typescript
type ViewState = 'loading' | 'loaded' | 'saving' | 'created' | 'loadError';
```

- `loading` — đang tải template + classes
- `loaded` — form sẵn sàng, chưa tạo session
- `saving` — đang POST create (duplicate-click guard)
- `created` — session đã tạo, hiển thị session detail với open/close buttons
- `loadError` — template không tìm thấy hoặc không ready

**Session action state (tách riêng):**
```typescript
type SessionAction = 'idle' | 'opening' | 'closing';
```

**Signals pattern (y hệt homework-create.component.ts):**
```typescript
protected readonly viewState = signal<ViewState>('loading');
protected readonly loadError = signal<string | null>(null);
protected readonly apiError = signal<string | null>(null);
protected readonly templateId = signal<string | null>(null);
protected readonly template = signal<TestTemplateDetail | null>(null);
protected readonly classes = signal<ClassSummary[]>([]);
protected readonly session = signal<LiveExamSession | null>(null);
protected readonly sessionAction = signal<SessionAction>('idle');

protected readonly selectedClassId = signal<string>('');
protected readonly scheduledStartAt = signal<string>('');
protected readonly scheduledEndAt = signal<string>('');

// Computed
protected readonly activeClasses = computed(() => this.classes().filter(c => c.status === 'active'));
protected readonly isFormValid = computed(() => !!this.selectedClassId() && this.template()?.status === 'ready');
protected readonly isSaving = computed(() => this.viewState() === 'saving');
protected readonly isCreated = computed(() => this.viewState() === 'created');
protected readonly canOpen = computed(() => this.session()?.status === 'scheduled');
protected readonly canClose = computed(() => this.session()?.status === 'open');
protected readonly isOpening = computed(() => this.sessionAction() === 'opening');
protected readonly isClosing = computed(() => this.sessionAction() === 'closing');
```

**QUAN TRỌNG — Đọc templateId từ `queryParamMap` không phải `paramMap`:**
Route là `/teacher/live-exams/new?templateId=...` (query param), KHÔNG PHẢI `/teacher/live-exams/new/:templateId`.

```typescript
ngOnInit(): void {
  this.paramSubscription = this.route.queryParamMap.subscribe(params => {
    const id = params.get('templateId');
    if (!id) {
      void this.router.navigate(['/teacher/library']);
      return;
    }
    this.templateId.set(id);
    void this.loadPage(id);
  });
}
```

**loadPage pattern (với loadRequestId để tránh stale loads — y hệt homework):**
- Load template + classes song song bằng `Promise.all`
- Kiểm tra `template.status !== 'ready'` → set loadError + viewState='loadError'
- Lọc activeClasses trong computed (không filter trong loadPage)

**onCreate:**
```typescript
protected async onCreate(): Promise<void> {
  const tid = this.templateId();
  const classId = this.selectedClassId();
  if (!tid || !classId || this.isSaving()) return;

  this.viewState.set('saving');
  this.apiError.set(null);

  const scheduledStartRaw = this.scheduledStartAt();
  const scheduledEndRaw = this.scheduledEndAt();

  try {
    const result = await this.liveExamApi.create({
      templateId: tid,
      classId,
      scheduledStartAt: scheduledStartRaw ? new Date(scheduledStartRaw).toISOString() : null,
      scheduledEndAt: scheduledEndRaw ? new Date(scheduledEndRaw).toISOString() : null,
    });
    this.session.set(result);
    this.viewState.set('created');
  } catch (error) {
    this.apiError.set(mapLiveExamError(error));
    this.viewState.set('loaded');
  }
}
```

**onOpen / onClose:**
```typescript
protected async onOpen(): Promise<void> {
  const s = this.session();
  if (!s || this.sessionAction() !== 'idle') return;
  this.sessionAction.set('opening');
  this.apiError.set(null);
  try {
    const updated = await this.liveExamApi.open(s.id);
    this.session.set(updated);
  } catch (error) {
    this.apiError.set(mapLiveExamError(error));
  } finally {
    this.sessionAction.set('idle');
  }
}
// onClose mirrors onOpen
```

**onCancel:**
```typescript
protected onCancel(): void {
  const tid = this.templateId();
  if (tid) {
    void this.router.navigate(['/teacher/library', tid, 'review']);
  } else {
    void this.router.navigate(['/teacher/library']);
  }
}
```

### Angular Models

```typescript
// core/live-exam/live-exam.models.ts
export interface CreateLiveExamRequest {
  templateId: string;
  classId: string;
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
}

export interface LiveExamSession {
  id: string;
  templateId: string;
  templateTitle: string;
  templateSkill: string;
  classId: string;
  className: string;
  status: string; // 'scheduled' | 'open' | 'closed'
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
  openedAt: string | null;
  closedAt: string | null;
  createdAt: string;
}

export const LIVE_EXAM_ERROR_MESSAGES: Record<string, string> = {
  'liveExam.templateNotFound': 'Không tìm thấy đề gốc hoặc đề không thuộc quyền quản lý của bạn.',
  'liveExam.templateNotReady': 'Đề gốc chưa ở trạng thái Sẵn sàng.',
  'liveExam.classNotFound': 'Không tìm thấy lớp hoặc lớp không thuộc quyền quản lý của bạn.',
  'liveExam.classNotActive': 'Lớp học đã không còn hoạt động. Vui lòng chọn lớp khác.',
  'liveExam.createFailed': 'Không thể tạo phiên thi. Vui lòng thử lại.',
  'liveExam.sessionNotFound': 'Không tìm thấy phiên thi.',
  'liveExam.alreadyOpen': 'Phiên thi đã đang mở.',
  'liveExam.sessionClosed': 'Phiên thi đã đóng, không thể mở lại.',
  'liveExam.alreadyClosed': 'Phiên thi đã đóng.',
  'liveExam.sessionNotOpen': 'Phiên thi chưa mở, không thể đóng.',
  'liveExam.transitionFailed': 'Không thể thay đổi trạng thái phiên. Vui lòng thử lại.',
};

export function mapLiveExamError(error: unknown): string {
  const extensions = (error as { error?: { extensions?: { code?: string } } })?.error?.extensions;
  const code = extensions?.code;
  if (code && Object.prototype.hasOwnProperty.call(LIVE_EXAM_ERROR_MESSAGES, code)) {
    return LIVE_EXAM_ERROR_MESSAGES[code];
  }
  return 'Có lỗi xảy ra. Vui lòng thử lại.';
}
```

### Angular API Service

```typescript
// core/live-exam/live-exam-api.service.ts
@Injectable({ providedIn: 'root' })
export class LiveExamApiService {
  private readonly http = inject(HttpClient);

  create(request: CreateLiveExamRequest): Promise<LiveExamSession> {
    return firstValueFrom(this.http.post<LiveExamSession>('/api/live-exam-sessions', request));
  }

  open(id: string): Promise<LiveExamSession> {
    return firstValueFrom(this.http.post<LiveExamSession>(`/api/live-exam-sessions/${id}/open`, {}));
  }

  close(id: string): Promise<LiveExamSession> {
    return firstValueFrom(this.http.post<LiveExamSession>(`/api/live-exam-sessions/${id}/close`, {}));
  }
}
```

### Stable Object IDs (AC2)

Template HTML phải có các IDs/attributes sau:

| Element | ID / Attribute | Khi nào hiển thị |
|---|---|---|
| Template title | `data-testid="source-template-title"` | viewState loaded/created |
| Template skill | `data-testid="source-template-skill"` | viewState loaded/created |
| Class select | `data-testid="class-select"` | viewState loaded |
| Scheduled start input | `data-testid="scheduled-start-input"` | viewState loaded |
| Scheduled end input | `data-testid="scheduled-end-input"` | viewState loaded |
| Create button | `data-testid="create-action"` | viewState loaded |
| Cancel button | `data-testid="cancel-action"` | viewState loaded |
| Status badge | `data-testid="session-status-badge"` | viewState created |
| Open button | `data-testid="open-action"` | canOpen() === true |
| Close button | `data-testid="close-action"` | canClose() === true |
| Loading indicator | `data-testid="loading-indicator"` | viewState loading |
| Load error | `data-testid="load-error"` | viewState loadError |
| API error banner | `data-testid="api-error"` | apiError() !== null |

### Testing Requirements

**API Tests — `CreateLiveExamSessionControllerTests.cs`:**
```
Setup: factory + AuthTestHelper.SignInTeacherAsync + LiveExamSessionTestHelper

Create tests:
- Create_WithValidData_Returns201WithScheduledStatus
- Create_WithScheduledTimes_Returns201 (ScheduledStartAt + ScheduledEndAt present)
- Create_TemplateNotOwned_Returns404
- Create_TemplateDraft_Returns400TemplateNotReady
- Create_TemplateArchived_Returns400TemplateNotReady
- Create_ClassNotOwned_Returns404
- Create_ClassNotActive_Returns400ClassNotActive
- Create_Anonymous_Returns401
- Create_Student_Returns403
```

**API Tests — `OpenCloseControllerTests.cs`:**
```
Open tests:
- Open_ScheduledSession_Returns200WithOpenStatus
- Open_AlreadyOpen_Returns409AlreadyOpen
- Open_ClosedSession_Returns409SessionClosed
- Open_SessionNotOwned_Returns404
- Open_Anonymous_Returns401

Close tests:
- Close_OpenSession_Returns200WithClosedStatus
- Close_AlreadyClosed_Returns409AlreadyClosed
- Close_ScheduledSession_Returns409SessionNotOpen
- Close_SessionNotOwned_Returns404
- Close_Anonymous_Returns401
```

**Test Helper — `LiveExamSessionTestHelper.cs`:**
```csharp
// Pattern giống HomeworkAssignmentTestHelper — xem story 3.1 pattern
internal static async Task<(Guid templateId, Guid classId)> EnsureReadyTemplateAndClassAsync(TestApiFactory factory)
internal static async Task<Guid> CreateScheduledSessionAsync(TestApiFactory factory, HttpClient client)
// CreateScheduledSessionAsync: tạo session via API sau khi đã sign in teacher
// Returns sessionId để dùng cho open/close tests
```

**Angular Tests — `live-exam-create.component.spec.ts`:**

```typescript
// Pattern giống homework-create.component.spec.ts (xem story 3.1 spec)
// QUAN TRỌNG: dùng flushPromises() thay vì whenStable():
async function flushPromises() {
  await new Promise<void>(r => setTimeout(r, 0));
}
async function initAndLoad() {
  fixture.detectChanges();
  await flushPromises();
  fixture.detectChanges();
}

Test cases (minimum 10):
1. hiển thị loading indicator khi khởi tạo
2. hiển thị form sau khi load (template title/skill, class select)
3. chỉ hiển thị active classes trong dropdown
4. nút Create disabled khi chưa chọn class
5. sau khi tạo thành công: hiển thị session detail với status badge "scheduled" và nút Open
6. sau khi click Open: session status thành "open", nút Close xuất hiện, nút Open ẩn
7. sau khi click Close: session status thành "closed", không còn nút Open/Close
8. hiển thị api error banner khi create thất bại
9. hiển thị api error banner khi open/close thất bại
10. click Cancel: navigate về /teacher/library/:templateId/review
11. không có templateId: redirect về /teacher/library
12. template không ready: hiển thị loadError state
```

**QUAN TRỌNG — Angular spec anti-patterns từ Story 3.1:**
- **KHÔNG** dùng `fixture.whenStable()` cho void promise chains — dùng `flushPromises()` (setTimeout 0)
- **KHÔNG** dùng `Object.assign(component, { session: mockSession })` để set signal — dùng `(component as any).session.set(mockSession)`
- **KHÔNG** `import { DatePipe }` trong standalone component nếu format date — dùng method `formatDate(iso: string): string` với `toLocaleString('vi-VN')`

### Architecture Compliance

- **KHÔNG** cần Authorization Policy mới — service-level ownership check qua `ITemplateAuthorizationService` + `IClassAuthorizationService` đủ (consistent với 3.1)
- **Controller thin** — delegate toàn bộ business logic vào service
- **Hidden 404** — cross-teacher session access trả 404, không 403
- **String enums** — status = `"scheduled"` / `"open"` / `"closed"`, không dùng int
- **Cookie auth + XSRF** — POST endpoints cần XSRF header — Angular interceptors handle globally
- **Không** tạo DB audit table — structured log đủ cho MVP
- **Audit state transitions** — log cả `previousStatus` và `newStatus` (AC4, AC5, AC6)
- **open/close body** — Angular gửi empty object `{}` trong POST body; API nhận `[FromBody]` không có request DTO (hoặc có thể dùng `[FromRoute] Guid id` chỉ từ path param)
- **Migration** — `dotnet ef migrations add AddLiveExamSessions --project src/EnglishTestWeb.Api` không cần `--startup-project`

### Patterns Established in Previous Stories (Must Reuse)

1. **HomeworkAssignmentService validation order** (Story 3.1) → áp dụng y chang cho LiveExamSessionService CreateAsync
2. **loadRequestId pattern** (Story 3.1) → chống stale concurrent loads trong Angular component
3. **flushPromises() trong Angular spec** (Story 3.1) → PHẢI dùng, không dùng whenStable()
4. **ActiveClass check** (Story 3.1 patch) — PHẢI có trong CreateAsync (service bên server-side)
5. **Audit log sau SaveChangesAsync** (Story 3.1 review pass 1) — PHẢI log sau save, không trước
6. **mapXxxError function** trong models.ts — dùng `extensions.code`, không `body.code`
7. **SKILL_LABELS** import trong component — `import { SKILL_LABELS } from '../../core/test-templates/test-templates.models'`
8. **ClassesApiService.getTeacherClasses()** — đã có, inject như homework-create.component.ts
9. **TestTemplatesApiService.getTemplate(id)** — đã có, dùng để load template detail

### File Structure Requirements

**API (new):**
- `src/EnglishTestWeb.Api/Domain/LiveExams/LiveExamSession.cs`
- `src/EnglishTestWeb.Api/Domain/LiveExams/LiveExamSessionStatuses.cs`
- `src/EnglishTestWeb.Api/Application/LiveExamSessions/ILiveExamSessionService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/LiveExamSessions/LiveExamSessionService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/LiveExamSessionConfiguration.cs`
- `src/EnglishTestWeb.Api/Contracts/LiveExamSessions/CreateLiveExamSessionRequest.cs`
- `src/EnglishTestWeb.Api/Contracts/LiveExamSessions/LiveExamSessionResponse.cs`
- `src/EnglishTestWeb.Api/Controllers/LiveExamSessionsController.cs`
- `src/EnglishTestWeb.Api/Migrations/*_AddLiveExamSessions.cs`

**API (modify):**
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs` — thêm `DbSet<LiveExamSession>`
- `src/EnglishTestWeb.Api/Program.cs` — register `ILiveExamSessionService`

**Angular (new):**
- `src/EnglishTestWeb.Client/src/app/core/live-exam/live-exam.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/live-exam/live-exam-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/features/live-exam-create/live-exam-create.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/live-exam-create/live-exam-create.component.html`
- `src/EnglishTestWeb.Client/src/app/features/live-exam-create/live-exam-create.component.css`
- `src/EnglishTestWeb.Client/src/app/features/live-exam-create/live-exam-create.component.spec.ts`

**Angular (modify):**
- `src/EnglishTestWeb.Client/src/app/app.routes.ts` — thay `TeacherPlaceholderComponent` bằng `LiveExamCreateComponent` cho route `live-exams/new`

**Tests (new):**
- `tests/EnglishTestWeb.Api.Tests/LiveExamSessions/LiveExamSessionTestHelper.cs`
- `tests/EnglishTestWeb.Api.Tests/LiveExamSessions/CreateLiveExamSessionControllerTests.cs`
- `tests/EnglishTestWeb.Api.Tests/LiveExamSessions/OpenCloseControllerTests.cs`

**Tests (modify):**
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`

### Anti-Patterns

- **KHÔNG** query `TestTemplate` trực tiếp trong controller — delegate qua service
- **KHÔNG** dùng `ActivatedRoute.paramMap` — route dùng query param `?templateId=...`, phải dùng `queryParamMap`
- **KHÔNG** dùng Angular `DatePipe` trong standalone component imports — dùng method format thay thế
- **KHÔNG** bỏ qua class Active check trong CreateAsync — đây là lỗi đã phát hiện trong story 3.1 review
- **KHÔNG** log audit trước SaveChangesAsync — nếu save thất bại, audit log sẽ sai
- **KHÔNG** tạo thêm Angular route mới (e.g., `/teacher/live-exams/:id`) — tất cả create + control trong một component `/teacher/live-exams/new`
- **KHÔNG** auto-open session khi scheduled time đến (AC7) — chỉ manual open/close

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 3, Story 3.2]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — API naming conventions, state transitions, transaction boundaries]
- [Source: `3-1-create-homeworkassignment-from-a-ready-template.md` — validation order, Angular patterns, spec patterns, flushPromises, audit log]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/HomeworkAssignments/HomeworkAssignmentService.cs` — CreateAsync validation pattern, ClassStatuses.Active check]
- [Source: `src/EnglishTestWeb.Api/Controllers/HomeworkAssignmentsController.cs` — thin controller pattern]
- [Source: `src/EnglishTestWeb.Client/src/app/features/homework-create/homework-create.component.ts` — viewState, signals, loadRequestId, loadPage, onSubmit, onCancel patterns]
- [Source: `src/EnglishTestWeb.Client/src/app/core/homework/homework-api.service.ts` — API service pattern với firstValueFrom]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/HomeworkAssignmentConfiguration.cs` — EF configuration pattern]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

### File List
