---
baseline_commit: 241e9d553074db22e2ccb6e33863e1e82ad5bed8
---

# Story 1.4: Base Authorization Pattern And Class Scope Guards

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

Là system owner,
tôi muốn một pattern authorization server-side tái sử dụng được cùng class/membership scope checks,
để các resource hiện tại được bảo vệ ngay và các resource tương lai có thể thêm policy riêng khi được tạo.

## Acceptance Criteria

1. **Given** Identity, roles, Classes, và ClassMembership đã tồn tại
   **When** authorization framework được triển khai
   **Then** cung cấp current-user access, role checks, resource-scope policy handlers, hidden-resource response helpers, và test fixtures cho Teacher, Student, và unauthenticated users.

2. **Given** giáo viên request một Class ngoài ownership
   **When** class-scope policy đánh giá request
   **Then** API trả `403` hoặc hidden `404` theo architecture rule đã lock
   **And** không serialize class hay roster data.

3. **Given** học sinh request một Class ngoài active ClassMembership
   **When** class-membership policy đánh giá request
   **Then** API reject server-side
   **And** direct Angular route access không bypass được quyết định đó.

3b. **Given** học sinh có cookie `etw:active_class_id` **stale** (claim trỏ tới Class không còn membership active, hoặc Class khác với membership thực tế)
   **When** gọi `GET /api/auth/me` hoặc `GET /api/classes/current`
   **Then** server revalidate membership live — không tin claim/client context đơn thuần
   **And** `/me` omit `activeClass`; `/classes/current` → 404 `classes.notFound`; Angular redirect `/class`.

4. **Given** protected class hoặc roster request bị deny
   **When** denial được log
   **Then** không expose sensitive identifiers ngoài audit metadata được phép
   **And** audit capture actor, resource id khi an toàn, và reason category.

5. **Given** authorization tests chạy
   **When** Teacher, Student, và unauthenticated cases được execute cho Class và membership resources hiện tại
   **Then** authorization matrix cover allowed, forbidden, và hidden-resource cases.

**Implementation Note:** Stories giới thiệu TestTemplate, TestMaterial, HomeworkAssignment, LiveExamSession, Submission, SpeakingSubmission, hoặc grading resources phải thêm resource-specific authorization policies trong cùng story đó.

## Tasks / Subtasks

- [x] Application security abstractions (AC: 1)
  - [x] `Application/Security/ICurrentUserContext.cs` — resolve `UserId`, roles, `IsAuthenticated` từ `ClaimsPrincipal` (không leak `HttpContext` ra ngoài Infrastructure ngoài implementation).
  - [x] `Application/Security/IClassAuthorizationService.cs` — `CanTeacherViewClassAsync`, `CanStudentAccessClassAsync`, `RequireTeacherClassAccess`, `RequireStudentClassAccess` trả structured result (allowed / hidden-not-found / forbidden).
  - [x] `Application/Security/AuthorizationDecision.cs` + `AuthorizationDenialReason.cs` — reason categories: `unauthenticated`, `wrongRole`, `class.ownership`, `class.membership`, `class.notFound`.
  - [x] `Application/Security/IHiddenResourceResponseFactory.cs` — map decision → `ProblemDetails` + HTTP status (`404` hidden vs `403` visible-denied).

- [x] Infrastructure authorization + audit (AC: 1, 2, 3, 4)
  - [x] `Infrastructure/Authorization/CurrentUserContext.cs` implement `ICurrentUserContext`.
  - [x] `Infrastructure/Authorization/ClassAuthorizationService.cs` implement class ownership + membership checks — inject `EnglishTestWebDbContext` trực tiếp (**không** inject `IClassService` → tránh circular DI với `ClassService` cũng gọi `IClassAuthorizationService`).
  - [x] `Infrastructure/Authorization/Policies/AuthorizationPolicies.cs` — policy names: `CanViewClassAsTeacher`, `CanViewClassAsStudent` (foundation cho future `CanViewHomework`, etc.).
  - [x] `Infrastructure/Authorization/Handlers/*AuthorizationHandler.cs` — **bắt buộc** (AC1 yêu cầu policy handlers): `IAuthorizationHandler` wired qua `AddAuthorization()`; handler gọi `IClassAuthorizationService` — không duplicate logic trong handler.
  - [x] `Infrastructure/Authorization/HiddenResourceResponseFactory.cs` — centralized ProblemDetails builder; stable codes `classes.notFound`, `classes.forbidden`, `auth.unauthorized`, `auth.forbidden`.
  - [x] `Infrastructure/Audit/IAuthorizationAuditLogger.cs` + `AuthorizationAuditLogger.cs` — structured log (MVP: `ILogger`, không cần DB audit table); event name `authorization.denied`; payload: actorId, role, resourceType=`class`, resourceId (khi safe), reasonCategory, `correlationId` đọc từ request header `X-Correlation-Id` khi có (client đã gửi qua `correlation-id.interceptor.ts`); **không** log passwords/tokens/roster.

- [x] Wire DI + Program.cs (AC: 1)
  - [x] `builder.Services.AddAuthorization(...)` register policies + handlers.
  - [x] Register `ICurrentUserContext`, `IClassAuthorizationService`, `IHiddenResourceResponseFactory`, `IAuthorizationAuditLogger`.
  - [x] `HttpContext`-based `ICurrentUserContext` scoped per request.

- [x] Refactor Classes API sang policy framework (AC: 2, 4, 5)
  - [x] `ClassService.GetClassDetailForTeacherAsync` — delegate ownership check sang `IClassAuthorizationService`; service chỉ materialize data khi allowed.
  - [x] `ClassesController.GetClassDetail` — dùng hidden-resource helper thay inline `ClassProblem`; **lock rule** (xem Technical Requirements).
  - [x] `ClassesController.GetTeacherClasses` — giữ scope filter (chỉ owned classes); không regression.
  - [x] Lookup `by-code` vẫn `[AllowAnonymous]` — không đổi contract public preview.

- [x] Student server-side class binding + membership revalidation (AC: 3, 5)
  - [x] Tại `LoginStudentAsync`: gắn `etw:active_class_id` vào **session principal/cookie** khi membership valid — dùng `ClaimsPrincipal` + `SignInAsync`, **không** `UserManager.AddClaimAsync` (tránh persist claim vào `AspNetUserClaims` vĩnh viễn).
  - [x] Mở rộng `CurrentUserResponse` + `GET /api/auth/me`: trả `activeClass` summary cho Student khi claim tồn tại **và** `HasActiveMembershipAsync(claimClassId, studentId)` pass (live DB — address DF1/DF4 từ story 1.3).
  - [x] **Stale claim rule:** Nếu claim trỏ Class mà student không còn active membership (revoked, inactive class, hoặc claim Class B trong khi chỉ member Class A) → omit `activeClass` trên `/me`; không fallback sang client/sessionStorage class code.
  - [x] Nếu membership revoked/inactive: `/me` không trả `activeClass`; `GET /api/classes/current` → 404 `classes.notFound` + audit `class.membership`.
  - [x] Thêm `GET /api/classes/current` — `[Authorize(Roles = Student)]`; đọc `etw:active_class_id` từ principal, revalidate membership, rồi mới trả class summary; hidden `404` nếu claim missing/invalid/stale.
  - [x] Student không được gọi `GET /api/classes/{id}` teacher roster — `[Authorize(Roles = Teacher)]` giữ nguyên; test assert student → `403 auth.forbidden` (role gate) hoặc policy deny trước khi serialize roster.

- [x] Angular alignment (AC: 3)
  - [x] `AuthSessionService.loadSession()` — hydrate `activeClass` từ `/api/auth/me` (server authority) thay vì chỉ public lookup restore.
  - [x] `studentGuard` — nếu authenticated student nhưng `/me` không có `activeClass`, redirect `/class` (server decision wins over stale client context).
  - [x] `ClassContextService` — server `activeClass` overrides client persistence khi conflict; nếu server omit → `clearClassContext()` (xóa stale `sessionStorage`).
  - [x] Không thêm localStorage token; cookie session unchanged.

- [x] Stale claim hardening (AC: 3, 3b)
  - [x] `IClassAuthorizationService.CanStudentAccessClassAsync(studentId, classId)` — single source cho `/me`, `/classes/current`, future student endpoints.
  - [x] Mở rộng `POST /api/auth/testing/sign-in` (Testing only): optional body `activeClassId` để seed stale-claim scenarios trong matrix tests.
  - [x] `ClassesTestHelper.SeedSecondClassWithoutMembershipAsync` — Class B owned by same/other teacher, student **không** có membership — dùng cho stale-claim test.

- [x] Test fixtures + authorization matrix (AC: 1, 5)
  - [x] `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` — matrix cho Class resources hiện tại.
  - [x] Mở rộng `AuthTestHelper` / `ClassesTestHelper` — helpers: `SignInTeacherAsync`, `SignInStudentAsync`, `SignInStudentWithClassClaimAsync` (nếu cần), unauthenticated client factory.
  - [x] Cases bắt buộc (assert `extensions.code` only):

| Actor | Endpoint | Expect |
|-------|----------|--------|
| Unauthenticated | `GET /api/classes` | 401 `auth.unauthorized` |
| Unauthenticated | `GET /api/classes/{ownedId}` | 401 |
| Teacher (owner) | `GET /api/classes/{ownedId}` | 200 + roster |
| Teacher (non-owner) | `GET /api/classes/{otherId}` | **404** `classes.notFound` (hidden) |
| Teacher | `GET /api/classes/current` | 403 `auth.forbidden` |
| Student (member) | `GET /api/classes/current` | 200 + class summary |
| Student (member) | `GET /api/classes/{ownedId}` | 403 `auth.forbidden` (role) — no roster leak |
| Student (testing sign-in, no class claim) | `GET /api/classes/current` | 404 `classes.notFound` |
| Student (member, membership revoked sau login) | `GET /api/classes/current` | 404 `classes.notFound`; `/me` omit `activeClass` |
| Student (stale claim: cookie `active_class_id` = Class B, chỉ member Class A) | `GET /api/classes/current` | 404 `classes.notFound`; `/me` omit `activeClass`; audit `class.membership` |
| Student (stale claim như trên) | `GET /api/classes/{classBId}` | 403 `auth.forbidden` (role) — **không** trả roster Class B |
| Student | `GET /api/classes` (teacher list) | 403 `auth.forbidden` |
| Student (member) | `GET /api/auth/teacher/ping` | 403 `auth.forbidden` (role gate — FR-1) |
| Teacher | `GET /api/classes/{nonExistentGuid}` | 404 `classes.notFound` (cùng shape hidden như non-owner) |
| Unauthenticated | `GET /api/classes/by-code/{code}` | 200 preview (unchanged) |

  - [x] Audit: deny case emits log với reason category; khi request gửi `X-Correlation-Id`, assert id xuất hiện trong audit payload (FakeAuthorizationAuditLogger hoặc `ILogger` test double).
  - [x] Regression: existing `ClassesControllerTests` + `StudentLoginTests` updated cho hidden-404 rule nếu đổi từ 403.

- [x] Docs (AC: 4)
  - [x] `docs/setup/development.md` — note server-side `activeClass` trên `/me`, smoke student revalidation.
  - [x] Comment ngắn trong story-dev notes: future resources add policies in-own-story (per Implementation Note).

## Dev Notes

### Discovery Results

- `epics_content`: `_bmad-output/planning-artifacts/epics.md` — Story 1.4, FR-1, FR-3, NFR-4.
- `architecture_content`: `_bmad-output/planning-artifacts/architecture.md` — Authorization patterns, hidden 404, policy handlers, folder layout `Infrastructure/Authorization/`, `Security/AuthorizationMatrixTests.cs`, audit event naming.
- `prd_content`: `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md` — NFR-4 Security And Scope.
- `ux_content`: không có file UX planning riêng; guards là UX helper only.
- Persistent fact `project-context.md`: không tìm thấy file.

### Story Foundation

Story 1.4 hoàn thiện **authorization foundation** cho Epic 1: extract inline checks từ Story 1.3 thành reusable framework, lock hidden-resource rules, thêm membership revalidation server-side, audit deny events, và authorization matrix tests.

**Phụ thuộc Story 1.3 (done):** `SchoolClass`, `ClassMembership`, `ClassesController`, `ClassService`, student login + membership tại login, Angular student/teacher guards, `ClassesTestHelper`, `AuthTestHelper`.

**Không kéo scope Epic 2+:** TestTemplate/Homework/LiveExam/Submission/File policies, full DB audit table, rate limiting, Admin role workflows, assigned tests list (Epic 4).

### Epic 1 Cross-Story Context

| Story | Phạm vi liên quan |
|-------|-------------------|
| 1.1 done | Identity, cookie auth, ProblemDetails baseline |
| 1.2 done | Teacher auth, route guards, XSRF |
| 1.3 done | Class domain, inline ownership/membership checks, student flow |
| **1.4 (this)** | Reusable authz framework, hidden resource policy, matrix tests, server-side student class binding |
| Epic 2+ | Resource-specific policies per Implementation Note |

### Files Being Modified — Current State & Required Changes

**`src/EnglishTestWeb.Api/Infrastructure/Classes/ClassService.cs`**
- *Hiện tại:* `GetClassDetailForTeacherAsync` inline check `schoolClass.TeacherId != teacherId` → `classes.forbidden`.
- *Thay đổi:* Gọi `IClassAuthorizationService`; chỉ query roster khi allowed; trả not-found decision cho non-owner.
- *Giữ nguyên:* Lookup, teacher list scoped query, `HasActiveMembershipAsync`.

**`src/EnglishTestWeb.Api/Controllers/ClassesController.cs`**
- *Hiện tại:* Inline `ClassProblem` helper; detail returns 403 `classes.forbidden` for non-owner.
- *Thay đổi:* Inject authorization helpers; map decisions to hidden 404; add `GET current` for students; audit on deny.
- *Giữ nguyên:* Anonymous lookup contract; teacher list endpoint shape.

**`src/EnglishTestWeb.Api/Infrastructure/Identity/AuthService.cs`**
- *Hiện tại:* `LoginStudentAsync` sets cookie without class claim; `GetCurrentUserAsync` returns roles only.
- *Thay đổi:* Add `active_class_id` claim at student login; `GetCurrentUserAsync` revalidate membership + attach `activeClass` to response.
- *Giữ nguyên:* Teacher login; unified `auth.loginInvalid` for non-member (product decision D1 from 1.3).

**`src/EnglishTestWeb.Api/Contracts/Auth/CurrentUserResponse.cs`**
- *Hiện tại:* `userId, email, userName, roles`.
- *Thay đổi:* Optional `activeClass?: { classId, className, classCode }` for students.

**`src/EnglishTestWeb.Api/Program.cs`**
- *Hiện tại:* `UseAuthentication` + `UseAuthorization` without custom policies.
- *Thay đổi:* `AddAuthorization` with class policies; register new security services.

**`src/EnglishTestWeb.Client/src/app/core/auth/auth-session.service.ts`**
- *Hiện tại:* `restoreStudentClassContext()` dùng public lookup API (DF5 — không check membership).
- *Thay đổi:* Prefer `/api/auth/me` `activeClass`; fallback `/class` redirect khi server không cấp context.
- *Giữ nguyên:* Cookie session; no browser token storage.

**`src/EnglishTestWeb.Client/src/app/core/route-access/student.guard.ts`**
- *Hiện tại:* Checks `classContext.activeClass()` client signal.
- *Thay đổi:* Align với server `/me` activeClass after `ensureSessionLoaded`.
- *Giữ nguyên:* Teacher → access-denied; unauthenticated redirects.

**`tests/EnglishTestWeb.Api.Tests/Classes/ClassesControllerTests.cs`**
- *Hiện tại:* `GetClassDetail_WithOtherTeacher_ReturnsForbidden` expects 403.
- *Thay đổi:* Update to 404 `classes.notFound` per locked architecture rule.

**`src/EnglishTestWeb.Api/Controllers/AuthController.cs`**
- *Hiện tại:* Private `AuthProblem` duplicate `ClassesController.ClassProblem`.
- *Thay đổi:* Dùng `IHiddenResourceResponseFactory` (hoặc shared `ProblemDetails` helper) cho deny responses có audit; giữ contract codes hiện có.

**`src/EnglishTestWeb.Client/src/app/core/auth/auth-api.service.ts` + `auth.models.ts`**
- *Hiện tại:* `CurrentUser` không có `activeClass`; `getCurrentUser()` typed không khớp `/me` mới.
- *Thay đổi:* Optional `activeClass` trên `CurrentUser`; map từ `/me`.

**`src/EnglishTestWeb.Client/src/app/core/classes/classes-api.service.ts`**
- *Hiện tại:* Chỉ teacher endpoints.
- *Thay đổi:* Thêm `getCurrentClass()` → `GET /api/classes/current`.

**`tests/EnglishTestWeb.Api.Tests/Auth/AuthTestHelper.cs`**
- *Hiện tại:* `SignInUserAsync` qua testing sign-in — không set class claim.
- *Thay đổi:* Thêm `SignInStudentWithClassAsync(client, classId?)` hoặc mở rộng testing sign-in để matrix cover student class paths.

**NEW API areas:** `Application/Security/*`, `Infrastructure/Authorization/**`, `Infrastructure/Audit/IAuthorizationAuditLogger.cs`, `tests/.../Security/AuthorizationMatrixTests.cs`.

### Architecture Compliance

- Authorization policies/handlers trong `Infrastructure/Authorization/`; Application chỉ abstractions. [Source: `architecture.md#Project Structure`]
- Role checks = broad capability; resource handlers = actual access. [Source: `architecture.md#Authorization Patterns`]
- **Hidden resource rule (LOCK cho story này):**
  - Read access tới Class teacher không sở hữu → **404** + `classes.notFound` (không leak existence).
  - Student không có membership cho class context → **404** hidden trên student class endpoints.
  - User đã auth, đúng role, nhưng action không được phép trên resource **visible** → **403** + `auth.forbidden` hoặc `classes.forbidden`.
- Angular guards = UX only; mọi quyết định enforce API-side. [Source: `architecture.md#Authorization Patterns`]
- Audit deny: event `authorization.denied` với actor, resource id when safe, reason category; không log sensitive data. [Source: `architecture.md#Audit Event Patterns`]
- ProblemDetails + stable `extensions.code`; tests assert codes only. [Source: `AGENTS.md`]
- Cookie-only auth; `active_class_id` claim trong Identity cookie — không JWT/localStorage. [Source: `AGENTS.md`]

### Technical Requirements

**Authorization decision flow:**

```text
1. Resolve ICurrentUserContext from ClaimsPrincipal
2. If !IsAuthenticated → 401 auth.unauthorized
3. If wrong role for endpoint → 403 auth.forbidden (role gate, before resource lookup)
4. Evaluate IClassAuthorizationService for resource scope
5. If resource outside scope (read) → 404 classes.notFound + audit authorization.denied
6. If resource visible but action disallowed → 403 + audit
7. On success → execute query (prefer scope filter before materialization)
```

**Proposed stable codes:**

| Code | HTTP | Khi |
|------|------|-----|
| `auth.unauthorized` | 401 | Chưa đăng nhập |
| `auth.forbidden` | 403 | Sai role hoặc action bị cấm trên resource visible |
| `classes.notFound` | 404 | Class ngoài scope (hidden — kể cả khi ID tồn tại) |
| `classes.forbidden` | 403 | Teacher action bị cấm khi ownership context đã visible (ít dùng ở MVP read paths) |
| `classes.codeNotFound` | 404 | Public lookup only (giữ từ 1.3) |

**Student active class claim:**

```text
Claim type: etw:active_class_id
Value: Class GUID string
Set: LoginStudentAsync after membership verified (session principal only)
Cleared: Logout; also treated invalid when live membership check fails
Validated: EVERY student class endpoint — HasActiveMembershipAsync(claimClassId, studentId)
Stale claim: claim present but membership fail → same as no claim (omit /me.activeClass, 404 /classes/current)
```

**Stale claim test setup (Testing env):**

```text
1. Seed Class A (student member) + Class B (student NOT member)
2. testing/sign-in student with { email, password, activeClassId: classBId }
3. GET /me → no activeClass
4. GET /classes/current → 404 classes.notFound
5. Optional: Angular sessionStorage has Class B preview → loadSession clears via server authority
```

**API contract changes:**

```text
GET /api/auth/me
  Authorize
  200: { userId, email, userName, roles, activeClass?: { classId, className, classCode } }
  // activeClass only when Student + valid membership

GET /api/classes/current  [NEW]
  Authorize Student
  200: { classId, className, classCode, status }
  404: classes.notFound (no valid membership context)
  401/403: standard auth codes

GET /api/classes/{id}  [Teacher only — behavior change]
  Non-owner teacher: 404 classes.notFound (was 403 classes.forbidden in 1.3)
```

**Extension pattern cho future stories:**

```text
// Khi thêm HomeworkAssignment trong Epic 3:
Application/Security/IHomeworkAuthorizationService.cs
Infrastructure/Authorization/Handlers/HomeworkAuthorizationHandler.cs
Policy: CanViewHomework, CanManageHomework
// Register trong story 3.1, không trong 1.4
```

### Library & Framework Requirements

- **ASP.NET Core Authorization** (`Microsoft.AspNetCore.Authorization`): `AddAuthorization`, `IAuthorizationHandler`, `[Authorize(Policy = "...")]`.
- **.NET 10 / Identity claims**: `UserManager.AddClaimsAsync` hoặc `SignInManager` custom principal với `ClaimsIdentity`.
- **EF Core 10**: Scope-filter queries `Where(c => c.TeacherId == userId)` trước materialize.
- **Angular 22**: Cập nhật `auth.models.ts` cho optional `activeClass` trên `CurrentUser`; functional guards unchanged pattern.
- **xUnit + Vitest**: Matrix tests API; optional guard spec update nếu `/me` hydration thay đổi redirect.

### File Structure Requirements

```text
src/EnglishTestWeb.Api/
  Application/Security/
    ICurrentUserContext.cs
    IClassAuthorizationService.cs
    IHiddenResourceResponseFactory.cs
    AuthorizationDecision.cs
    AuthorizationDenialReason.cs
  Infrastructure/Authorization/
    CurrentUserContext.cs
    ClassAuthorizationService.cs
    HiddenResourceResponseFactory.cs
    Policies/AuthorizationPolicies.cs
    Handlers/ClassTeacherAuthorizationHandler.cs
    Handlers/ClassStudentAuthorizationHandler.cs
  Infrastructure/Audit/
    IAuthorizationAuditLogger.cs
    AuthorizationAuditLogger.cs
  Contracts/Auth/
    CurrentUserResponse.cs          # add optional activeClass

tests/EnglishTestWeb.Api.Tests/
  Security/
    AuthorizationMatrixTests.cs
  TestKit/                          # optional
    Fakes/FakeAuthorizationAuditLogger.cs
```

### Testing Requirements

**API matrix (`AuthorizationMatrixTests.cs`):** Cover table trong Tasks; assert status + `extensions.code` only.

**Regression updates:**
- `ClassesControllerTests.GetClassDetail_WithOtherTeacher_*` → 404 `classes.notFound`.
- `StudentLoginTests` — không regression login paths.
- New: `/me` returns `activeClass` for member student; null after logout; revoked membership clears `activeClass`.

**Angular (Vitest):**
- `auth-session.service.spec.ts`: loadSession sets activeClass from /me mock.
- `auth-session.service.spec.ts`: loadSession với /me không có `activeClass` nhưng sessionStorage có stale preview → `clearClassContext()`.
- `student.guard.spec.ts`: authenticated student without server activeClass → `/class`.
- `student.guard.spec.ts`: stale client context + server omit activeClass → redirect `/class` (không vào `/student/tests`).

**Quality gate:** `.\scripts\quality.ps1` must pass before marking done.

### UX / Visual Notes

- Không đổi layout pages; chỉ redirect/guard behavior khi server không cấp class context.
- Access-denied copy giữ nguyên; không expose tên lớp/roster trong error responses.

### Anti-Patterns To Avoid

- Không dùng `UserManager.AddClaimAsync` cho `active_class_id` — session-only claim.
- Không tạo circular DI (`IClassService` ↔ `IClassAuthorizationService`).
- Không implement Homework/Template/File policies trong story này.
- Không dùng Angular-only checks thay server enforcement.
- Không trả 403 kèm roster/class name cho cross-scope read (enumeration leak).
- Không duplicate ownership logic trong Controller và Service — single `IClassAuthorizationService`.
- Không tạo full audit DB table — MVP logger đủ cho AC4.
- Không đổi public lookup contract (`by-code`).
- Không JWT / localStorage.
- Không assert ProblemDetails message text trong tests.

### Previous Story Intelligence (1.3)

**Patterns reuse:**
- `ClassService`, `ClassesController`, `AuthService` boundaries.
- `AuthTestHelper`, `ClassesTestHelper` seed shapes.
- `ProblemDetails` + `extensions.code` pattern từ `AuthController`/`ClassesController`.
- Student guards + `ClassContextService` — extend, không rewrite.
- `SignInForTestingAsync` cho tests — có thể cần mở rộng để set class claim trong tests.

**Explicit deferrals từ 1.3 — NOW IN SCOPE:**
- DF1: Post-login membership revalidation mỗi student API call → `/me` + `/classes/current`.
- DF4: Server-side class binding sau login → `active_class_id` claim + `/me.activeClass`.

**Still deferred:**
- DF2/DF3: Rate limiting; multi-class teacher UX.
- DF5: Client restore via public lookup — thay bằng server `/me` (fix trong 1.4).
- DF6: Class code in URL — UX refactor later.
- Epic 2+ resource policies.

**1.3 behavior change alert:**
- Teacher non-owner class detail: **403 → 404 hidden** — intentional alignment với architecture; update tests + document trong dev notes.

### Git Intelligence

Recent commits:

- `241e9d5` — feat: implement class roster lookup and student login (story 1.3)
- `e309adb` — feat: implement teacher login and app shell (story 1.2)
- `b413bac` — fix: harden story 1.1 baseline after code review

**Insights:** Vertical slices với API tests trước; review cycles harden edge cases (guards, XSRF, session rehydrate). Story 1.4 là refactor + security hardening — giữ diff focused, update existing tests khi đổi 403→404.

### Latest Technical Information (2026-06-10)

- **ASP.NET Core Authorization handlers:** Register with `services.AddSingleton<IAuthorizationHandler, THandler>()` + `AddAuthorization(o => o.AddPolicy(..., p => p.Requirements.Add(...)))`. Handlers remain stateless; use scoped services via `IHttpContextAccessor` carefully or evaluate in controller via `IClassAuthorizationService` (simpler, testable).
- **Identity claims at sign-in:** Build `ClaimsPrincipal` with `etw:active_class_id` **chỉ trong cookie session** (`SignInAsync` với principal tùy chỉnh). **Không** dùng `UserManager.AddClaimAsync` — claim sẽ persist DB và leak sang teacher login / lớp khác.
- **Hidden 404 pattern:** Same response body for not-found ID and out-of-scope ID prevents enumeration — critical for `GET /api/classes/{id}`.

### Project Context Reference

- `AGENTS.md` — stack, boundaries, cookie auth, ProblemDetails.
- `_bmad-output/implementation-artifacts/1-3-class-roster-class-code-lookup-and-student-login.md` — inline checks to extract, DF1/DF4/DF5, file list.
- `_bmad-output/implementation-artifacts/deferred-work.md` — update khi close DF1/DF4.
- `_bmad-output/planning-artifacts/architecture.md#Authorization Patterns`
- `_bmad-output/planning-artifacts/architecture.md#FR-1 to FR-3 Accounts, Roles, Classes, Access`

## References

- `_bmad-output/planning-artifacts/epics.md#Story 1.4`
- `_bmad-output/planning-artifacts/architecture.md#Authorization And Scope Matrix Context`
- `_bmad-output/planning-artifacts/architecture.md#Authorization Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Audit Event Patterns`
- `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md#NFR-4`
- `_bmad-output/implementation-artifacts/1-3-class-roster-class-code-lookup-and-student-login.md`

## Dev Agent Record

### Agent Model Used

Auto (Cursor)

### Debug Log References

- Teacher class detail non-owner: 403 `classes.forbidden` → 404 `classes.notFound` (hidden resource rule).
- Student `activeClass` on `/me` dùng `JsonIgnore(Condition = WhenWritingNull)` để omit khi stale/revoked.
- `ClassAuthorizationService` inject `DbContext` trực tiếp — tránh circular DI với `ClassService`.

### Completion Notes List

- Application security abstractions: `ICurrentUserContext`, `IClassAuthorizationService`, `AuthorizationDecision`, `IHiddenResourceResponseFactory`.
- Infrastructure: authorization handlers/policies, `HiddenResourceResponseFactory`, `AuthorizationAuditLogger`, `AuthorizationDenialAuditor`, `ApiAuthChallengeWriter`.
- API: `GET /api/classes/current`, `/me` + `activeClass`, session claim `etw:active_class_id`, testing sign-in `activeClassId`.
- Angular: hydrate/clear class context từ server `/me`.
- Tests: `AuthorizationMatrixTests` (13 cases), regression updates; quality gate 45 API + 38 Angular.

### File List

- `src/EnglishTestWeb.Api/Application/Security/**`
- `src/EnglishTestWeb.Api/Application/Classes/IClassService.cs`
- `src/EnglishTestWeb.Api/Contracts/Auth/CurrentUserResponse.cs`
- `src/EnglishTestWeb.Api/Contracts/Auth/TestingSignInRequest.cs`
- `src/EnglishTestWeb.Api/Contracts/Classes/ClassCurrentResponse.cs`
- `src/EnglishTestWeb.Api/Controllers/AuthController.cs`
- `src/EnglishTestWeb.Api/Controllers/ClassesController.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Authorization/**`
- `src/EnglishTestWeb.Api/Infrastructure/Audit/**`
- `src/EnglishTestWeb.Api/Infrastructure/Classes/ClassService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Identity/AuthService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Security/ApiAuthChallengeWriter.cs`
- `src/EnglishTestWeb.Api/Program.cs`
- `src/EnglishTestWeb.Client/src/app/core/auth/auth.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/auth/auth-session.service.ts`
- `src/EnglishTestWeb.Client/src/app/core/auth/auth-session.service.spec.ts`
- `src/EnglishTestWeb.Client/src/app/core/classes/classes-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/core/classes/classes.models.ts`
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`
- `tests/EnglishTestWeb.Api.Tests/Auth/AuthTestHelper.cs`
- `tests/EnglishTestWeb.Api.Tests/Classes/ClassesControllerTests.cs`
- `tests/EnglishTestWeb.Api.Tests/Classes/ClassesTestHelper.cs`
- `docs/setup/development.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`

### Change Log

- 2026-06-10: Story 1.4 — authorization framework, class scope guards, server-side student class binding, matrix tests, docs.

## Story Completion Status

Status: `done`

Completion note: All ACs satisfied; quality gate passed (45 API tests, 38 Angular tests).

### Self-Review (2026-06-10)

_Review sau khi tạo story; critical fixes đã merge vào file._

#### patch (6) — applied to story

| ID | Issue | Fix |
|----|-------|-----|
| P1 | `AddClaimAsync` gợi ý sai — persist claim vĩnh viễn | Lock session-only principal claim |
| P2 | Circular DI `IClassService` ↔ `IClassAuthorizationService` | Authz service dùng `DbContext` trực tiếp |
| P3 | Policy handlers marked optional — AC1 requires handlers | Bắt buộc `IAuthorizationHandler` |
| P4 | Matrix thiếu revoked membership, non-existent ID, student→teacher/ping | Bổ sung rows + FR-1 case |
| P5 | Thiếu file list Angular/API helpers | Thêm auth-api, auth.models, classes-api, AuthTestHelper |
| P6 | Duplicate ProblemDetails builders | AuthController dùng shared factory |

#### patch (2) — enhancement round 2 (user requested)

| ID | Enhancement | Applied |
|----|-------------|---------|
| E1 | Stale `active_class_id` claim + AC 3b + matrix rows + testing sign-in helper | ✅ |
| E2 | Audit correlation id từ `X-Correlation-Id` header | ✅ |

#### defer (1)

| ID | Gap | Rationale |
|----|-----|-----------|
| D1 | Student `GET /api/classes/{id}` dùng membership policy thay role-only 403 | Role gate đủ chặn roster; membership policy áp dụng trên `/me` + `/classes/current`; stale claim covered AC 3b |

#### dismiss (2)

| ID | Concern | Rationale |
|----|---------|-----------|
| X1 | AC2 epic cho phép 403 hoặc 404 | Story lock 404 cho cross-scope read — đúng architecture |
| X2 | `/me` omit vs 403 khi membership revoked | Omit `activeClass` + 404 trên `/classes/current` — consistent hidden pattern |

#### Verdict

**Pass — ready-for-dev** sau self-review + enhancement E1/E2. Dev agent nên đọc Anti-Patterns (claim persistence, circular DI, stale claim revalidation) trước khi code.

### Review Findings

#### patch

- [x] [Review][Patch] Đăng ký `IAuthorizationHandler` là Singleton trong khi handler inject scoped `IClassAuthorizationService` — vi phạm DI lifetime; đổi `AddScoped` [`Program.cs:132-133`]
- [x] [Review][Patch] `GetClassDetailForTeacherAsync` dùng `FirstAsync` sau auth — class xóa giữa hai query → 500; dùng `FirstOrDefaultAsync` + trả `classes.notFound` [`ClassService.cs:100`]
- [x] [Review][Patch] `loadSessionInternal` catch không gọi `clearClassContext()` — stale client context khi `/me` fail [`auth-session.service.ts:137-139`]
- [x] [Review][Patch] Matrix thiếu: unauthenticated `GET /api/classes/{ownedId}` → 401 (AC 5) [`AuthorizationMatrixTests.cs`]
- [x] [Review][Patch] Matrix thiếu: stale claim student `GET /api/classes/{classBId}` → 403 `auth.forbidden` (AC 3b / AC 5) [`AuthorizationMatrixTests.cs`]
- [x] [Review][Patch] Không có test assert audit denial (`authorization.denied`, reason category) (AC 4 / AC 5) [`AuthorizationMatrixTests.cs`]
- [x] [Review][Patch] Không có test assert `X-Correlation-Id` trong audit payload (AC 4 / AC 5) [`AuthorizationMatrixTests.cs`]
- [x] [Review][Patch] `RevokedMembership_GetCurrentClass_ReturnsNotFound` không assert `extensions.code` = `classes.notFound` (AC 5) [`AuthorizationMatrixTests.cs`]
- [x] [Review][Patch] `SignInForTestingAsync` gọi `MapUserAsync(user, principal: null)` — response sign-in bỏ qua claim `activeClassId` vừa set [`AuthService.cs:197`]
- [x] [Review][Patch] `GetCurrentClass` không re-check `ClassStatuses.Active` sau auth — TOCTOU có thể trả class inactive [`ClassesController.cs:103-107`]

#### defer

- [x] [Review][Defer] `GetClassContextByIdAsync` không có auth riêng — caller phải guard trước [`ClassService.cs`] — deferred, pre-existing pattern; document khi thêm caller mới
- [x] [Review][Defer] Triple authorization trên `GET /api/classes/{id}` (policy + service + service lần 2) — redundant DB round-trips [`ClassesController.cs:124-140`] — deferred, defense-in-depth acceptable MVP
- [x] [Review][Defer] Stale `active_class_id` cookie không bị xóa khi deny — chỉ omit response [`AuthService.cs`] — deferred, live revalidation on read là design lock
- [x] [Review][Defer] `CanViewClassAsStudent` policy chưa gắn endpoint — inline `RequireStudentClassAccessAsync` tương đương [`Program.cs`, `ClassesController.cs`] — deferred, handler foundation cho Epic 2+
- [x] [Review][Defer] Audit MVP chỉ `ILogger` — không DB persistence (AC 4 explicit MVP) [`AuthorizationAuditLogger.cs`] — deferred, per spec scope
- [x] [Review][Defer] Correlation ID chỉ đọc client header — không server fallback [`AuthorizationDenialAuditor.cs`] — deferred, per spec E2
- [x] [Review][Defer] `/me` `activeClass` không có `status`; `/classes/current` có — contract asymmetry [`CurrentUserResponse.cs`, `ClassCurrentResponse.cs`] — deferred, summary vs detail intentional

### Re-Review Findings (2026-06-10, post-patch)

#### patch

- [x] [Review][Patch] `MapUserAsync` thiếu re-check `ClassStatuses.Active` sau `GetClassContextByIdAsync` — lệch với `GetCurrentClass` [`AuthService.cs:230-237`]
- [x] [Review][Patch] `loadSessionInternal` không `clearClassContext()` khi user authenticated nhưng không phải Student — stale sessionStorage [`auth-session.service.ts:133-135`]
- [x] [Review][Patch] Matrix thiếu assert audit `class.membership` cho stale-claim `GET /classes/current` (AC 5) [`AuthorizationMatrixTests.cs`]
- [x] [Review][Patch] Matrix thiếu row `Unauthenticated | GET /by-code/{code} | 200` (AC 5) [`AuthorizationMatrixTests.cs`]
- [x] [Review][Patch] Matrix thiếu test class inactive + membership active → 404 (AC 5) [`AuthorizationMatrixTests.cs`]

#### defer

- [x] [Review][Defer] `GetClassDetail` policy fail + inline allow → deny không audit (TOCTOU cực hiếm) [`ClassesController.cs:134-137`] — deferred, acceptable MVP

### Re-Review Round 3 (2026-06-10)

**Verdict: ✅ Clean review — all layers passed.**

- Acceptance Auditor: AC 1–5, 3b, full matrix table — satisfied
- Blind Hunter: no HIGH/MEDIUM; 1 LOW deferred (`loginStudent` không set `activeClass` trên `CurrentUser` signal — UI dùng `classContext`, không blocking)
- Edge Case Hunter: TOCTOU membership/class-status races — deferred (live revalidation on read là design lock)
