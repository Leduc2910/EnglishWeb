---
baseline_commit: b413bacd28cca2a71038a1192761dc932b22d59d
---

# Story 1.2: Teacher Login And Teacher App Shell

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

Là giáo viên,
tôi muốn đăng nhập và vào teacher shell dự đoán được,
để bắt đầu từ Dashboard và điều hướng tới Thư viện đề, Lớp học, và Kết quả mà không nhầm role.

## Acceptance Criteria

1. **Given** giáo viên chưa đăng nhập truy cập `/login`
   **When** trang login load
   **Then** hiển thị branding EnglishTestWeb, copy ngữ cảnh giáo viên, input username/email, password có show/hide, remember-me nếu stack hỗ trợ, link quên mật khẩu, và label hiển thị rõ.

2. **Given** giáo viên submit thiếu hoặc sai thông tin
   **When** validation client hoặc authentication thất bại
   **Then** lỗi inline dùng mã lỗi ổn định và copy tiếng Việt
   **And** response không tiết lộ email có tồn tại hay không.

3. **Given** thông tin đăng nhập giáo viên hợp lệ
   **When** giáo viên đăng nhập thành công
   **Then** app route tới `/teacher/dashboard` hoặc teacher route được yêu cầu ban đầu
   **And** nav Dashboard, Thư viện đề, Lớp học, Kết quả hiển thị trong teacher shell.

4. **Given** tài khoản Student cố truy cập teacher route/API
   **When** route guard và authorization server chạy
   **Then** access bị từ chối server-side
   **And** Angular guard hiển thị trạng thái blocked/login phù hợp, không lộ dữ liệu giáo viên.

5. **Given** teacher shell được thao tác bằng keyboard
   **When** giáo viên tab qua nav và login controls
   **Then** focus visible và theo thứ tự visual.

## Tasks / Subtasks

- [x] API auth surface theo architecture boundary (AC: 2, 3, 4)
  - [x] Tạo `Contracts/Auth/*`, `Application/Auth/*`, `Controllers/AuthController.cs`.
  - [x] `POST /api/auth/login` — nhận email/username + password (+ rememberMe); delegate `SignInManager`; chỉ cho phép user có role `Teacher`; trả `401` + `auth.loginInvalid` khi sai credential hoặc không phải teacher (cùng message, không leak enumeration).
  - [x] `POST /api/auth/logout` — `[Authorize]`; sign out cookie; `204`.
  - [x] `GET /api/auth/me` — `[Authorize]`; trả `CurrentUserResponse` gồm `userId`, `email`, `userName`, `roles[]`.
  - [x] Teacher-only smoke endpoint tối thiểu, ví dụ `GET /api/auth/teacher/ping` hoặc route teacher placeholder có `[Authorize(Roles = "Teacher")]` để AC4 có server-side proof.
  - [x] Login endpoint vẫn qua XSRF: client gọi `GET /api/security/xsrf-token` trước `POST /api/auth/login`.
  - [x] Không expose JWT; cookie `EnglishTestWeb.Auth` HttpOnly như baseline.

- [x] Dev/test teacher user seed idempotent (AC: 3)
  - [x] Thêm seeder (ví dụ `IdentityDevUserSeeder` hoặc mở rộng startup seed) chỉ chạy Development/Testing hoặc khi flag config bật.
  - [x] Tạo/verify 1 user Teacher (email + password documented trong `docs/setup/development.md`).
  - [x] Không seed Class/Student/ClassMembership — thuộc Story 1.3.

- [x] Angular auth core (AC: 2, 3, 4)
  - [x] Mở rộng `AuthSessionService`: `loadSession()`, `login()`, `logout()`, signal/observable `currentUser`, `isTeacher`, `isAuthenticated`.
  - [x] `AuthApiService` trong `core/auth` gọi `/api/auth/*` qua HttpClient (credentials + XSRF đã có).
  - [x] Map `ProblemDetails.extensions.code` → UI errors; client validation dùng keys UX `ERR_LOGIN_*`.
  - [x] Không dùng `localStorage`/`sessionStorage` cho token.

- [x] Route access guards (AC: 3, 4)
  - [x] `core/route-access/teacher.guard.ts` — chặn unauthenticated → `/login?returnUrl=...`; chặn Student/wrong role → blocked state hoặc redirect an toàn.
  - [x] `guest.guard.ts` — authenticated teacher vào `/login` → redirect dashboard hoặc `returnUrl`.
  - [x] Preserve `returnUrl` qua login success.

- [x] Teacher login page `/login` (AC: 1, 2, 5)
  - [x] Feature `features/teacher-login/` hoặc `features/auth/teacher-login/` standalone component.
  - [x] Layout: brand bar + context panel + login card theo UX 01.1 / 03.1 (object IDs `teacher-login-*`).
  - [x] States: default, loading, error, success redirect.
  - [x] Show/hide password, Enter submit, visible focus, labels + `autocomplete`.
  - [x] `/forgot-password` route placeholder (copy/link only; full reset flow defer).

- [x] Teacher app shell + routes (AC: 3, 5)
  - [x] `shared/layouts/teacher-shell/` — persistent nav: Dashboard | Thư viện đề | Lớp học | Kết quả; account menu logout.
  - [x] Routes:
    - `/teacher/dashboard` — scan surface placeholder (header + empty/skeleton metrics OK; không implement FR-19 metrics đầy đủ).
    - `/teacher/library`, `/teacher/classes`, `/teacher/results` — placeholder pages trong shell (nav active state + title); feature logic thuộc epic sau.
  - [x] Root redirect: authenticated teacher → dashboard; unauthenticated public entry → `/login`.
  - [x] Wildcard 404 route tối thiểu (deferred từ 1.1).

- [x] Tests (AC: 2, 3, 4)
  - [x] API: login success teacher; login invalid credentials; login student rejected; `GET /api/auth/me` authenticated; teacher-only endpoint 403 for student; logout clears session.
  - [x] API tests assert `extensions.code`, không assert message text.
  - [x] Angular: login form validation errors; guard redirect với `returnUrl`; auth service không persist token storage; teacher shell renders 4 nav labels.

- [x] Docs (AC: 3)
  - [x] `docs/setup/development.md`: dev teacher credentials, login smoke steps, routes list.

## Dev Notes

### Discovery Results

- `epics_content`: `_bmad-output/planning-artifacts/epics.md` — Story 1.2, UX-DR1, UX-DR2, FR-1.
- `architecture_content`: `_bmad-output/planning-artifacts/architecture.md` — Auth, API patterns, Angular structure, FR-1 mapping.
- `prd_content`: `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md` — FR-1, FR-19 nav expectations, NFR-3, NFR-4.
- UX behavior authority: `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.1-teacher-login-account-access/1.1-teacher-login-account-access.md`, `1.2-teacher-dashboard/1.2-teacher-dashboard.md`.
- Visual reference only: `docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md`.
- Persistent fact glob `**/project-context.md`: không tìm thấy file.

### Story Foundation

Story 1.2 triển khai FR-1 (teacher authentication + teacher shell access) trên baseline Story 1.1. Phạm vi: login flow end-to-end, session read/logout, teacher shell navigation, role separation Teacher vs Student ở route/API. Không build class roster, student login, class-scope authorization handlers (Story 1.3–1.4). Dashboard chỉ cần shell + placeholder scan surface; metrics/recent work đầy đủ thuộc Epic 6 / FR-19.

**Phụ thuộc Story 1.1 (đã done):** Identity roles `Teacher`/`Student`, cookie auth, XSRF middleware, HttpClient interceptors, `TestApiFactory`, `scripts/quality.ps1`.

**Không kéo scope Story 1.3+:** Class entity, class code lookup, student `/class`, ClassMembership, resource policy handlers, Thư viện đề CRUD, results workspace.

### Epic 1 Cross-Story Context

| Story | Phạm vi liên quan |
|-------|-------------------|
| 1.1 done | Stack, Identity schema, roles seed, cookie+XSRF, protected storage foundation |
| **1.2 (this)** | Teacher login, session API, teacher shell, role guards |
| 1.3 | Class roster UI, class code, student login, full MVP seed (Teacher+Student+Class+Membership) |
| 1.4 | Resource authorization matrix, class-scope guards |

### Files Being Modified — Current State & Required Changes

**`src/EnglishTestWeb.Api/Program.cs`**
- *Hiện tại:* Identity cookie configured; API paths return 401/403 thay vì HTML redirect; antiforgery + XSRF middleware; role seed on startup optional.
- *Thay đổi:* Register auth application services, authorization policies (`TeacherOnly` minimum), dev teacher user seeder hook, optional login rate limiting nếu implement đơn giản.
- *Giữ nguyên:* Cookie name `EnglishTestWeb.Auth`, API-friendly redirect handlers, Data Protection fail-fast production.

**`src/EnglishTestWeb.Client/src/app/app.routes.ts`**
- *Hiện tại:* `routes: []` — empty.
- *Thay đổi:* Full route tree: `/login`, `/teacher/*` lazy or eager features, guards, default redirects, 404 wildcard.

**`src/EnglishTestWeb.Client/src/app/core/auth/auth-session.service.ts`**
- *Hiện tại:* Chỉ `usesBrowserTokenStorage = false` và `persistAccessToken` throws — baseline anti-pattern guard.
- *Thay đổi:* Session load/login/logout; expose auth state cho guards và shell; vẫn cấm browser token storage.

**`src/EnglishTestWeb.Client/src/app/app.html` / `app.css`**
- *Hiện tại:* Minimal baseline shell text "features arrive in later stories".
- *Thay đổi:* Root chỉ `<router-outlet>`; layout chuyển vào `TeacherShell` / login page.

**`src/EnglishTestWeb.Api/Controllers/SecurityController.cs`**
- *Hiện tại:* `GET /api/security/xsrf-token` — giữ nguyên; login flow phụ thuộc endpoint này.

**NEW API areas (chưa tồn tại):** `AuthController`, `Application/Auth`, `Contracts/Auth`, optional `Infrastructure/Identity/IdentityDevUserSeeder.cs`.

**NEW Angular areas (chưa tồn tại):** `core/route-access/`, `shared/layouts/teacher-shell/`, `features/teacher-login/`, `features/teacher-dashboard/` (placeholder), placeholder features cho library/classes/results.

### Architecture Compliance

- Controllers → Application → Domain; `AuthController` delegate sang `IAuthService` / command handler, không gọi `UserManager`/`SignInManager` trực tiếp trong controller nếu có thể wrap trong Application layer. [Source: `architecture.md#Architectural Boundaries`]
- REST: `/api/auth/login`, `/api/auth/logout`, `/api/auth/me` — lowercase, unversioned. [Source: `architecture.md#API Naming Conventions`]
- Errors: `ProblemDetails` + `extensions.code` namespace `auth.*`. Tests assert codes only. [Source: `architecture.md#Format Patterns`]
- Angular route guards là UX helper; API phải enforce role server-side. [Source: `architecture.md#Authorization Patterns`]
- Cookie-only auth; không JWT/localStorage. [Source: `AGENTS.md`, `architecture.md#Authentication & Security`]
- Feature folders theo route, không theo Stitch screen names. [Source: `architecture.md#Frontend Architecture`]

### Technical Requirements

**API auth contract (đề xuất — giữ consistent khi implement):**

```text
POST /api/auth/login
  Body: { "identifier": string, "password": string, "rememberMe": boolean }
  200: { "userId", "email", "userName", "roles" }
  401: ProblemDetails code auth.loginInvalid

POST /api/auth/logout
  204

GET /api/auth/me
  200: CurrentUserResponse | 401
```

**Stable error codes (API):**

| Code | Khi |
|------|-----|
| `auth.loginInvalid` | Sai credential hoặc user không có role Teacher (cùng response shape) |
| `auth.unauthorized` | Chưa đăng nhập |
| `auth.forbidden` | Đã đăng nhập nhưng thiếu role (Student vào teacher API) |
| `auth.xsrfRequired` / `auth.xsrfInvalid` | Đã có từ baseline |

**Client validation keys (UX — map tới copy tiếng Việt):**

| Key | Copy |
|-----|------|
| `ERR_LOGIN_IDENTIFIER_REQUIRED` | Nhập email hoặc tên đăng nhập. |
| `ERR_LOGIN_PASSWORD_REQUIRED` | Nhập mật khẩu. |
| `ERR_LOGIN_INVALID` | Thông tin đăng nhập chưa đúng. Kiểm tra lại email và mật khẩu. |

**Login flow sequence:**

1. Angular navigate `/login` → optional `GET /api/security/xsrf-token` (credentials).
2. `POST /api/auth/login` với XSRF header + credentials.
3. API `PasswordSignInAsync`; verify role `Teacher`; reject Student với cùng `auth.loginInvalid` hoặc `auth.forbidden` tùy policy (ưu tiên không leak: dùng `auth.loginInvalid` cho wrong role at login).
4. `AuthSessionService.loadSession()` via `GET /api/auth/me`.
5. Redirect `returnUrl` nếu hợp lệ và thuộc `/teacher/*`, else `/teacher/dashboard`.

**Teacher routes (Angular):**

| Path | Guard | Ghi chú |
|------|-------|---------|
| `/login` | guest | Public |
| `/forgot-password` | none | Placeholder |
| `/teacher/dashboard` | teacher | Placeholder dashboard |
| `/teacher/library` | teacher | Placeholder — Epic 2 |
| `/teacher/classes` | teacher | Placeholder — Story 1.3 |
| `/teacher/results` | teacher | Placeholder — Epic 6 |

**Dev teacher seed:** Idempotent; config-driven credentials (ví dụ `Identity:DevTeacher:Email`, `Password` trong `appsettings.Development.json`); document rõ trong setup doc. Password không commit secret production values.

**Rate limiting:** Architecture khuyến nghị rate limit login. Nếu chưa có infrastructure, có thể defer với note trong completion — ưu tiên auth correctness trước; nếu thêm, dùng ASP.NET Core rate limiting middleware scoped `POST /api/auth/login`.

### Library & Framework Requirements

- **.NET 10 / ASP.NET Core Identity 10:** `SignInManager<ApplicationUser>`, `UserManager<ApplicationUser>`, `[Authorize(Roles = IdentityRoleNames.Teacher)]`.
- **Angular 22 standalone:** `CanActivateFn` functional guards, signals hoặc RxJS cho session state, reactive forms cho login.
- **Vitest:** co-located `.spec.ts` như baseline.
- **XSRF:** Cookie `XSRF-TOKEN`, header `X-XSRF-TOKEN` — đã configured trong `http.providers.ts`; login POST phải pass positive XSRF test (closes deferred item từ Story 1.1).

### File Structure Requirements

```text
src/EnglishTestWeb.Api/
  Contracts/Auth/
    LoginRequest.cs
    CurrentUserResponse.cs
  Application/Auth/
    IAuthService.cs
    AuthService.cs (or LoginCommandHandler)
  Controllers/
    AuthController.cs
  Infrastructure/Identity/
    IdentityDevUserSeeder.cs (optional name)

src/EnglishTestWeb.Client/src/app/
  core/
    auth/
      auth-api.service.ts
      auth-session.service.ts (extend)
    route-access/
      teacher.guard.ts
      guest.guard.ts
  shared/layouts/teacher-shell/
    teacher-shell.component.ts|html|css
  features/
    teacher-login/
    teacher-dashboard/
    teacher-library-placeholder/   # or single placeholder component reused
    teacher-classes-placeholder/
    teacher-results-placeholder/

tests/EnglishTestWeb.Api.Tests/
  Auth/
    AuthControllerTests.cs (or AuthLoginTests.cs)
```

### Testing Requirements

**API (xUnit + `TestApiFactory`):**

- Seed teacher + student users in test setup (in-memory DB + `UserManager`).
- Teacher login → 200 + roles contains `Teacher`.
- Bad password → 401 + `auth.loginInvalid`.
- Student login attempt at teacher login endpoint → 401/403 per chosen policy (document in test).
- `GET /api/auth/me` without cookie → 401.
- Teacher-only endpoint with student cookie → 403 + `auth.forbidden`.
- Valid XSRF + login → 200 (positive path — addresses Story 1.1 defer).

**Angular (Vitest):**

- Login component required field validation emits correct error keys.
- `AuthSessionService` still does not write tokens to storage (extend existing spec).
- Teacher guard redirects unauthenticated to `/login?returnUrl=...`.
- Teacher shell template contains nav labels: Dashboard, Thư viện đề, Lớp học, Kết quả.

**Quality gate:** `.\scripts\quality.ps1` must pass before marking story done.

### UX / Visual Notes

- Task-focused login: không hero marketing. Brand bar + context panel + form card. [UX 01.1]
- Teacher shell nav persistent; Dashboard là scan surface, không phải workflow launcher chính. [UX 01.2, UX-DR2]
- Typography/spacing: Inter (đã trong `app.css`), tokens `space-*` / heading scale có thể dùng CSS variables đơn giản — không Bootstrap.
- Object IDs từ UX spec (`teacher-login-*`, `teacher-dashboard-nav-*`) cho testability/accessibility.
- Stitch chỉ tham khảo visual; WDS/UX scenarios là behavior authority.
- WCAG AA: visible focus rings, label associated inputs, keyboard nav AC5.

### Anti-Patterns To Avoid

- Không JWT / localStorage / sessionStorage auth.
- Không bypass XSRF cho login vì "đã same-origin".
- Không implement class/student flows trong story này.
- Không để controller gọi `DbContext`/`SignInManager` trực tiếp without application boundary.
- Không leak email existence qua khác message/status cho wrong email vs wrong password.
- Không build full dashboard metrics (FR-19) — chỉ shell + placeholder.
- Không duplicate login component cho Scenario 03 — reuse một teacher login feature.
- Không tổ chức code theo folder Stitch screen names.

### Previous Story Intelligence (1.1)

**Patterns đã establish — reuse, đừng reinvent:**

- Cookie `EnglishTestWeb.Auth`, API 401/403 thay vì redirect HTML cho `/api/*`.
- `IdentityRoleNames.Teacher` / `Student`; `IdentityRoleSeeder` idempotent.
- `GET /api/security/xsrf-token` + `XsrfProtectionMiddleware` cho unsafe verbs.
- Angular `httpProviders`: credentials + XSRF + correlation id + ProblemDetails interceptor.
- `isApiRequest()` helper — dùng cho auth API calls.
- `TestApiFactory` in-memory DB — extend cho user seed trong auth tests.
- `scripts/quality.ps1` + CI workflow — keep green.

**Deferred từ code review 1.1 — address trong story này:**

- `AuthSessionService` session read/logout → **this story**.
- Empty routes / wildcard 404 → **this story**.
- Positive XSRF path khi có auth flow → **this story**.

**Still deferred (không scope 1.2):**

- `IXsrfTokenService` HttpContext in Application layer.
- Symlink escape hardening protected storage.
- `IFileStorage` read/delete.
- Correlation ID propagation policy.

### Git Intelligence

Recent commits:

- `b413bac` — harden story 1.1 baseline after code review.
- `b9cc3b0` — chore: baseline story 1.1.

**Insights:** Team ưu tiên boundary compliance, stable ProblemDetails codes, test coverage cho security paths, và documentation/proxy alignment. Follow cùng style: Application abstractions, Infrastructure implementations, focused API tests asserting codes, Angular core layer extensions trước features.

### Latest Technical Information (2026-06-10)

- **ASP.NET Core Identity cookie auth:** `PasswordSignInAsync` sets HttpOnly cookie; `IsPersistent` maps to remember-me. API SPA pattern: disable redirect, return status codes (đã configured in `Program.cs`). [Identity docs ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- **Antiforgery with SPA:** Issue token via dedicated GET; Angular `withXsrfConfiguration` sends header on POST. Login is POST → must prefetch XSRF. [Antiforgery docs](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery)
- **Angular 22 guards:** Use `CanActivateFn` with `inject(AuthSessionService)` and `Router`. [Angular router guards](https://angular.dev/guide/routing/common-router-tasks#preventing-unauthorized-access)
- **Angular HttpClient credentials:** `withCredentials`/interceptor already sends cookies cho same-origin/proxy dev.

### Project Context Reference

- `AGENTS.md` — stack boundaries, cookie auth, ProblemDetails codes.
- `docs/setup/development.md` — local run, proxy `http://localhost:5124`, migration/seed commands.
- `_bmad-output/implementation-artifacts/deferred-work.md` — 1.1 deferrals mapped to this story.

## References

- `_bmad-output/planning-artifacts/epics.md#Story 1.2`
- `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md#FR-1`
- `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.1-teacher-login-account-access/1.1-teacher-login-account-access.md`
- `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.2-teacher-dashboard/1.2-teacher-dashboard.md`
- `_bmad-output/implementation-artifacts/1-1-setup-baseline-net-10-web-api-angular-22-sql-server-identity-protected-storage.md`
- `docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md`

## Dev Agent Record

### Agent Model Used

Auto (Cursor)

### Debug Log References

- `Program.cs` dùng EF InMemory khi `Environment=Testing` + `Testing:DatabaseName` từ `TestApiFactory` để tránh dual SQL Server/InMemory provider conflict.
- Thêm `POST /api/auth/testing/sign-in` (Testing-only) để integration test student session cho `teacher/ping` forbidden case.

### Completion Notes List

- API auth: login/logout/me/teacher/ping + `IAuthService`/`AuthService` boundary; stable `auth.*` ProblemDetails codes.
- Dev teacher seed idempotent qua `IdentityDevUserSeeder` + `Identity:SeedDevTeacherOnStartup` / `--seed-dev-teacher`.
- Angular: auth session signals, guards, teacher login page, teacher shell nav, placeholder routes, 404/access-denied.
- Tests: 19 API + 21 Angular pass; `.\scripts\quality.ps1` pass.
- Rate limiting login deferred (architecture note) — auth correctness ưu tiên trong story này.

### File List

- `src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj`
- `src/EnglishTestWeb.Api/Program.cs`
- `src/EnglishTestWeb.Api/appsettings.Development.json`
- `src/EnglishTestWeb.Api/Contracts/Auth/**`
- `src/EnglishTestWeb.Api/Application/Auth/**`
- `src/EnglishTestWeb.Api/Infrastructure/Identity/AuthService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Identity/IdentityDevUserSeeder.cs`
- `src/EnglishTestWeb.Api/Controllers/AuthController.cs`
- `src/EnglishTestWeb.Client/src/app/app.html`
- `src/EnglishTestWeb.Client/src/app/app.css`
- `src/EnglishTestWeb.Client/src/app/app.routes.ts`
- `src/EnglishTestWeb.Client/src/app/app.spec.ts`
- `src/EnglishTestWeb.Client/src/app/core/auth/**`
- `src/EnglishTestWeb.Client/src/app/core/route-access/**`
- `src/EnglishTestWeb.Client/src/app/shared/layouts/teacher-shell/**`
- `src/EnglishTestWeb.Client/src/app/features/teacher-login/**`
- `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/**`
- `src/EnglishTestWeb.Client/src/app/features/teacher-placeholder/**`
- `src/EnglishTestWeb.Client/src/app/features/access-denied/**`
- `src/EnglishTestWeb.Client/src/app/features/forgot-password/**`
- `src/EnglishTestWeb.Client/src/app/features/not-found/**`
- `tests/EnglishTestWeb.Api.Tests/TestApiFactory.cs`
- `tests/EnglishTestWeb.Api.Tests/Auth/**`
- `docs/setup/development.md`

### Change Log

- 2026-06-10: Story 1.2 — teacher login API, dev seed, Angular auth/shell/routes, tests, docs.

### Review Findings

- [x] [Review][Patch] Logout không xóa session client khi API lỗi [`auth-session.service.ts:49`]
- [x] [Review][Patch] Thiếu API test `GET /api/auth/me` khi đã đăng nhập teacher [`AuthControllerTests.cs`]
- [x] [Review][Patch] Thiếu test `guestGuard` redirect với `returnUrl` hợp lệ [`teacher.guard.spec.ts`]
- [x] [Review][Patch] Thiếu test `teacherGuard` chặn user đã auth nhưng không phải Teacher → `/access-denied` [`teacher.guard.spec.ts`]
- [x] [Review][Patch] `sanitizeTeacherReturnUrl` nên decode URI trước khi validate để chặn bypass `://` [`return-url.ts:1`]
- [x] [Review][Defer] Rate limiting cho `POST /api/auth/login` — deferred, pre-existing (architecture khuyến nghị; story completion note đã ghi nhận)
- [x] [Review][Patch] `logout()` chỉ `clearSession` trong `finally` sau API logout, không bọc prefetch XSRF [`auth-session.service.ts:49`] (re-review 2026-06-10)
- [x] [Review][Patch] `logout()` nuốt lỗi API sau XSRF để shell luôn redirect `/login` [`auth-session.service.ts:49`] (re-review lần 3, 2026-06-10)

## Story Completion Status

Status: `done`

Completion note: All acceptance criteria implemented; code review patches applied; quality gate passed.
