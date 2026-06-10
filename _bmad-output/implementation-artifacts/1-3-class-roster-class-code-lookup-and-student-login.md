---
baseline_commit: e309adb4da27e748428218711a96795089d002de
---

# Story 1.3: Class Roster, Class Code Lookup, And Student Login

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

Là học sinh,
tôi muốn nhập mã lớp, xác nhận đúng lớp, và đăng nhập với context lớp đó,
để vào đúng workspace lớp trước khi thấy bài được giao.

## Acceptance Criteria

1. **Given** môi trường MVP cần dữ liệu test trước khi có UX quản lý lớp đầy đủ
   **When** lệnh seed/admin provisioning chạy
   **Then** tạo hoặc verify một Teacher, một Student, một Class active, và một ClassMembership active
   **And** thao tác idempotent cho local/dev và test environments.

2. **Given** giáo viên có Class active (seed hoặc admin-created) với class code và student memberships
   **When** giáo viên mở surface Lớp học
   **Then** giáo viên thấy tên lớp, mã lớp, trạng thái active, và danh sách học sinh đã ghi danh trong phạm vi lớp của mình.

3. **Given** học sinh mở `/class`
   **When** nhập mã lớp có khoảng trắng, dấu gạch, hoặc chữ thường
   **Then** hệ thống normalize mã để lookup
   **And** vẫn xử lý input an toàn (trim, loại ký tự không hợp lệ, không crash).

4. **Given** mã lớp hợp lệ và Class đang active
   **When** lookup thành công
   **Then** confirmation card hiển thị tên lớp và context giáo viên
   **And** học sinh phải xác nhận trước khi navigate tới `/student/login`.

5. **Given** mã không hợp lệ hoặc Class không còn active
   **When** lookup thất bại
   **Then** học sinh thấy lỗi tiếng Việt có thể thử lại
   **And** không expose roster lớp hay assigned tests.

6. **Given** đã chọn class context và credential học sinh hợp lệ
   **When** học sinh đăng nhập
   **Then** API verify ClassMembership server-side
   **And** route trực tiếp tới Assigned Tests (`/student/tests`) cho Class active đó.

7. **Given** tài khoản học sinh không thuộc Class đã chọn
   **When** login hoàn tất credential validation
   **Then** access bị chặn với bước tiếp theo rõ (đổi mã lớp / liên hệ giáo viên)
   **And** không trả assignments, sessions, submissions, hay chi tiết roster.

## Tasks / Subtasks

- [x] Domain & persistence — Class + ClassMembership (AC: 1, 2, 6, 7)
  - [x] Tạo `Domain/Classes/Class.cs`, `ClassMembership.cs`, enums/status strings: `active`, `inactive`.
  - [x] EF configurations: bảng `Classes`, `ClassMemberships`; unique index trên `Classes.ClassCode` (normalized); FK `TeacherId` → `AspNetUsers`, `StudentId` → `AspNetUsers`.
  - [x] Migration mới (intent-based name, ví dụ `AddClassesAndMemberships`).
  - [x] Register `DbSet` trong `EnglishTestWebDbContext`.

- [x] MVP seed idempotent (AC: 1)
  - [x] Mở rộng hoặc tạo seeder (ví dụ `MvpDemoDataSeeder` / mở rộng `IdentityDevUserSeeder`) tạo/verify: dev Teacher (reuse 1.2), dev Student, 1 Class active với class code cố định, 1 ClassMembership active.
  - [x] Config trong `appsettings.Development.json`: email/password student, class name, class code (document trong `docs/setup/development.md`).
  - [x] CLI flag `--seed-mvp-demo` và/hoặc `Identity:SeedMvpDemoOnStartup`; idempotent khi chạy lại.
  - [x] Test factory helper seed cùng shape cho API tests.

- [x] Application + API — Classes (AC: 2, 3, 4, 5)
  - [x] `Application/Classes/IClassService.cs` + implementation.
  - [x] `Contracts/Classes/*`: lookup preview DTO (public-safe), teacher roster DTOs.
  - [x] `ClassesController`:
    - `GET /api/classes/by-code/{code}` — `[AllowAnonymous]`; normalize code server-side; trả preview tối thiểu (classId, className, teacherDisplayName, status) hoặc `404` + `classes.codeNotFound` / `classes.codeInactive`.
    - `GET /api/classes` — `[Authorize(Roles = Teacher)]`; list classes của teacher hiện tại.
    - `GET /api/classes/{id}` — `[Authorize(Roles = Teacher)]`; detail + enrolled students; reject nếu không phải owner (403 hoặc hidden 404 — chọn một rule và document; inline filter OK cho story này, full policy handlers thuộc 1.4).
  - [x] Lookup response không bao gồm roster, assignments, hay student PII ngoài teacher display name.

- [x] Application + API — Student auth (AC: 6, 7)
  - [x] Mở rộng `IAuthService` / `AuthService`: `LoginStudentAsync(StudentLoginRequest)` — verify credential, role `Student`, Class active, ClassMembership active cho classId/classCode đã chọn.
  - [x] `POST /api/auth/student/login` — body: `{ identifier, password, classCode, rememberMe? }`; XSRF required.
  - [x] Stable codes: `auth.loginInvalid` (sai credential, không leak enumeration); `auth.notInClass` (credential OK nhưng không có membership); `classes.codeNotFound` / `classes.codeInactive` khi class context invalid.
  - [x] Response success: `CurrentUserResponse` + optional `activeClass` summary `{ classId, className, classCode }` (camelCase JSON).
  - [x] Teacher login endpoint (`POST /api/auth/login`) vẫn chỉ cho Teacher — không regression.

- [x] Angular — class context & student auth core (AC: 3–7)
  - [x] `core/classes/class-context.service.ts` — signal lưu confirmed class preview; persist `classCode` qua query param `/student/login?classCode=` (refresh-safe); **không** dùng localStorage cho auth token; sessionStorage cho classCode optional nếu cần reload `/class` flow.
  - [x] `core/classes/classes-api.service.ts` — lookup by code, teacher roster calls.
  - [x] Mở rộng `AuthSessionService`: `loginStudent()`, `isStudent`, map `auth.notInClass` → copy UX.
  - [x] `core/route-access/student.guard.ts` — unauthenticated student routes → `/student/login` hoặc `/class` nếu thiếu class context; authenticated teacher vào student route → blocked; authenticated student without membership context → redirect `/class`.

- [x] Angular — student surfaces (AC: 3–7)
  - [x] `features/student-class-entry/` — route `/class`; UX 02.1 object IDs; states: default, loading, confirm, error; normalize input client-side (uppercase visual, strip spaces/dashes).
  - [x] `features/student-login/` — route `/student/login`; UX 02.2; hiển thị class context card; link đổi lớp → `/class`; missing context → prompt về `/class`.
  - [x] `features/student-assigned-tests/` — route `/student/tests` **placeholder**: header class context + empty state "Chưa có bài được giao" (Epic 4 implement list thật); đủ để AC6 redirect sau login.
  - [x] Optional minimal `shared/layouts/student-shell/` top bar (class name + logout) — hoặc inline trong assigned-tests placeholder.

- [x] Angular — teacher Lớp học surface (AC: 2)
  - [x] Thay placeholder `/teacher/classes` bằng `features/teacher-classes/` — hiển thị class(es) seeded: tên, mã, badge active, bảng/list học sinh enrolled (display name/email).
  - [x] Teacher chỉ thấy classes của mình (API scoped); empty state nếu chưa seed.

- [x] Routes & root redirect (AC: 3, 6)
  - [x] Cập nhật `app.routes.ts`: `/class`, `/student/login`, `/student/tests`, giữ `/login` cho teacher.
  - [x] `rootRedirectGuard`: authenticated teacher → dashboard; authenticated student với active class context → `/student/tests`; unauthenticated → `/class` (student entry) hoặc document choice — **ưu tiên `/class` làm public entry cho student flow**, `/login` vẫn teacher-only.

- [x] Tests (AC: 1–7)
  - [x] API: seed idempotent; lookup valid/invalid/inactive code; normalization cases; teacher roster scoped; student login success with membership; student login wrong password; student not in class; teacher cannot use student login; lookup không leak roster.
  - [x] API tests assert `extensions.code` only, không assert message text.
  - [x] Angular: class code validation errors; confirmation flow; student login not-in-class error; student guard redirects; teacher classes renders seeded roster fields.

- [x] Docs (AC: 1)
  - [x] `docs/setup/development.md`: dev student credentials, class code, seed command, smoke flow class → login → tests placeholder.

## Dev Notes

### Discovery Results

- `epics_content`: `_bmad-output/planning-artifacts/epics.md` — Story 1.3, FR-2, FR-3, UX-DR3, UX-DR4, MVP provisioning decision.
- `architecture_content`: `_bmad-output/planning-artifacts/architecture.md` — Domain/Classes structure, `/api/classes`, string state enums, authorization boundaries, FR-1–3 mapping.
- `prd_content`: `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md` — FR-2, FR-3, glossary Class/ClassMembership.
- Data model hint: `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml` — Class fields: id, name, class_code, teacher_id, status; ClassMembership: class_id, student_id, status.
- UX behavior authority:
  - `_bmad-output/C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.1-student-class-code-entry/2.1-student-class-code-entry.md`
  - `_bmad-output/C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.2-student-login-account-access/2.2-student-login-account-access.md`
  - `_bmad-output/C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.3-student-assigned-tests/2.3-student-assigned-tests.md` (placeholder scope only)
- Persistent fact glob `**/project-context.md`: không tìm thấy file.

### Story Foundation

Story 1.3 triển khai FR-2 (class code entry + confirmation + context preservation) và FR-3 (ClassMembership enforcement tại login) trên baseline Story 1.1–1.2. Phạm vi: domain Class/ClassMembership, MVP seed, public class code lookup, teacher roster read surface, student login với membership check, student routes tới assigned tests placeholder.

**Phụ thuộc Story 1.2 (done):** Cookie auth, XSRF, `AuthController`, `IAuthService`, `AuthSessionService`, teacher shell/guards, dev teacher seed, `TestApiFactory`, ProblemDetails codes.

**Không kéo scope Story 1.4+:** Reusable authorization policy handler framework, hidden-resource helper abstraction, authorization matrix test suite đầy đủ, CRUD quản lý lớp/import học sinh, HomeworkAssignment/LiveExamSession, assigned tests list thật (Epic 4), student shell đầy đủ cho attempt workspace.

**MVP Provisioning (locked decision):** Không build LMS admin module. Dùng idempotent seed cho Teacher + Student + Class + ClassMembership để FR-1–3 testable.

### Epic 1 Cross-Story Context

| Story | Phạm vi liên quan |
|-------|-------------------|
| 1.1 done | Identity schema, roles, cookie+XSRF, EF baseline |
| 1.2 done | Teacher login, session API, teacher shell, teacher guards |
| **1.3 (this)** | Class entities, MVP seed, class lookup, teacher roster read, student login + membership |
| 1.4 | Reusable authorization handlers, class-scope guards matrix, hidden 404 policy |

### Files Being Modified — Current State & Required Changes

**`src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs`**
- *Hiện tại:* Chỉ Identity tables + role seed data.
- *Thay đổi:* `DbSet<Class>`, `DbSet<ClassMembership>`, configurations, migration.
- *Giữ nguyên:* Identity role seed pattern.

**`src/EnglishTestWeb.Api/Infrastructure/Identity/AuthService.cs`**
- *Hiện tại:* `LoginTeacherAsync`, `GetCurrentUserAsync`, `LogoutAsync`, `SignInForTestingAsync`.
- *Thay đổi:* Thêm `LoginStudentAsync` với membership verification; inject class query service hoặc `DbContext` qua application layer (prefer `IClassService` / `IClassMembershipService`).
- *Giữ nguyên:* Teacher login behavior; không leak enumeration.

**`src/EnglishTestWeb.Api/Controllers/AuthController.cs`**
- *Hiện tại:* Teacher login/logout/me/teacher/ping + testing sign-in.
- *Thay đổi:* `POST student/login` endpoint.
- *Giữ nguyên:* Teacher routes; ProblemDetails helper pattern.

**`src/EnglishTestWeb.Api/Infrastructure/Identity/IdentityDevUserSeeder.cs`**
- *Hiện tại:* Chỉ seed dev Teacher.
- *Thay đổi:* Mở rộng hoặc delegate sang MVP demo seeder cho Student + Class + Membership (Story 1.2 note: "Không seed Class/Student/ClassMembership — thuộc Story 1.3").

**`src/EnglishTestWeb.Api/Program.cs`**
- *Hiện tại:* Register auth services, seed hooks cho roles + dev teacher.
- *Thay đổi:* Register class services, MVP seed hook + CLI flag, migrate on seed path.
- *Giữ nguyên:* Cookie/XSRF/Testing in-memory DB behavior.

**`src/EnglishTestWeb.Client/src/app/app.routes.ts`**
- *Hiện tại:* Teacher routes + `/login`; `/teacher/classes` placeholder; root redirect teacher → dashboard else login.
- *Thay đổi:* Student routes; replace classes placeholder; update root redirect for student entry at `/class`.

**`src/EnglishTestWeb.Client/src/app/core/auth/auth-session.service.ts`**
- *Hiện tại:* Teacher login/session; `isTeacher`; no browser token storage.
- *Thay đổi:* Student login, `isStudent`, active class signal hoặc companion service, error mapping for not-in-class.
- *Giữ nguyên:* Cookie session model; `usesBrowserTokenStorage = false`.

**`src/EnglishTestWeb.Client/src/app/core/route-access/teacher.guard.ts`**
- *Hiện tại:* `teacherGuard`, `guestGuard`, `rootRedirectGuard`.
- *Thay đổi:* `rootRedirectGuard` phân nhánh student; không break teacher returnUrl behavior.
- *Giữ nguyên:* Teacher guard logic.

**NEW API areas:** `Domain/Classes/`, `Application/Classes/`, `Contracts/Classes/`, `Controllers/ClassesController.cs`, MVP seeder, EF migration.

**NEW Angular areas:** `core/classes/`, `core/route-access/student.guard.ts`, `features/student-class-entry/`, `features/student-login/`, `features/student-assigned-tests/`, `features/teacher-classes/`.

### Architecture Compliance

- Controllers → Application → Domain; `ClassesController` delegate `IClassService`, không query `DbContext` trực tiếp. [Source: `architecture.md#Architectural Boundaries`]
- REST: `/api/classes`, `/api/classes/by-code/{code}`, `/api/auth/student/login` — lowercase, unversioned. [Source: `architecture.md#API Naming Conventions`]
- State values as strings: Class/Membership `active` | `inactive`. [Source: `architecture.md#Data Exchange Formats`]
- Errors: `ProblemDetails` + stable `extensions.code` namespace `classes.*`, `auth.*`. Tests assert codes only.
- Angular guards là UX helper; membership và teacher ownership phải enforce server-side. [Source: `architecture.md#Authorization Patterns`]
- Cookie-only auth; class context ≠ auth token — không lưu credential/token trong browser storage. [Source: `AGENTS.md`]
- Public lookup DTO tối thiểu — không trả roster hay assignments. [Source: FR-2 consequences, UX 02.1]
- Student-facing responses không include answer keys (N/A story này) — pattern giữ cho tương lai.

### Technical Requirements

**Class code normalization (client + server — phải khớp):**

```text
1. Trim whitespace
2. Remove spaces and dashes
3. ToUpperInvariant
4. Validate: ^[A-Z0-9]{4,12}$ after normalization
```

Ví dụ: `"eng-7 a"` → `"ENG7A"`.

**Proposed API contracts:**

```text
GET /api/classes/by-code/{code}
  AllowAnonymous
  200: { classId, className, classCode, teacherDisplayName, status: "active"|"inactive" }
  404: classes.codeNotFound | classes.codeInactive

GET /api/classes
  Authorize Teacher
  200: ClassSummaryDto[]  // teacher's classes only

GET /api/classes/{id}
  Authorize Teacher + owner check
  200: { classId, className, classCode, status, students: [{ studentId, displayName, email, membershipStatus }] }

POST /api/auth/student/login
  AllowAnonymous + XSRF
  Body: { identifier, password, classCode, rememberMe? }
  200: { userId, email, userName, roles, activeClass: { classId, className, classCode } }
  401: auth.loginInvalid
  403: auth.notInClass
  404: classes.codeNotFound | classes.codeInactive (if class context invalid before auth)
```

**Stable error codes (API):**

| Code | Khi |
|------|-----|
| `classes.codeNotFound` | Mã không match Class nào |
| `classes.codeInactive` | Class tồn tại nhưng status inactive |
| `classes.forbidden` | Teacher truy cập Class không thuộc ownership |
| `auth.loginInvalid` | Sai credential hoặc không phải Student (cùng shape, không leak) |
| `auth.notInClass` | Credential OK, Student role OK, nhưng không có active ClassMembership |
| `auth.unauthorized` / `auth.forbidden` | Đã có từ 1.2 |

**Client validation keys (UX):**

| Key | Copy |
|-----|------|
| `ERR_CLASS_CODE_REQUIRED` | Nhập mã lớp. |
| `ERR_CLASS_CODE_FORMAT` | Mã lớp chưa đúng định dạng. |
| `ERR_CLASS_CODE_INVALID` | Không tìm thấy lớp với mã này. |
| `ERR_CLASS_CODE_EXPIRED` | Mã lớp này đã hết hiệu lực. |
| `ERR_STUDENT_IDENTIFIER_REQUIRED` | Nhập tài khoản học sinh. |
| `ERR_STUDENT_PASSWORD_REQUIRED` | Nhập mật khẩu. |
| `ERR_STUDENT_NOT_IN_CLASS` | Tài khoản này chưa thuộc lớp đã chọn. Kiểm tra lại với giáo viên. |

**Student flow sequence:**

1. Navigate `/class` → nhập mã → `GET /api/classes/by-code/{normalized}`.
2. Confirmation card → user confirms → navigate `/student/login?classCode=ENG7A` + store preview in `ClassContextService`.
3. `GET /api/security/xsrf-token` → `POST /api/auth/student/login` with classCode.
4. API: validate class active → `PasswordSignInAsync` → verify Student role → verify ClassMembership active → sign in cookie.
5. `AuthSessionService.loadSession()` + set active class → redirect `/student/tests`.

**Teacher roster flow:**

1. Authenticated teacher → `/teacher/classes` → `GET /api/classes` (+ optional detail for student list).
2. Render class code for teacher to share with students (AC2).

**Dev MVP seed (document in development.md — ví dụ):**

| Entity | Suggested dev value |
|--------|---------------------|
| Teacher | Reuse `teacher@englishtestweb.local` / `Teacher123!` |
| Student email | `student@englishtestweb.local` |
| Student password | `Student123!` |
| Class name | `English 7A` |
| Class code | `ENG7A` |

**Scope boundary vs Story 1.4:**

- Story 1.3: Inline checks trong `LoginStudentAsync` và teacher ownership filter trong `IClassService`.
- Story 1.4: Extract thành `IAuthorizationService`, policy handlers, hidden 404 helper, matrix tests — **không duplicate full framework trong 1.3**.

### Library & Framework Requirements

- **.NET 10 / EF Core 10:** New migration; GUID primary keys for Class/Membership per architecture default.
- **ASP.NET Core Identity:** Reuse `ApplicationUser`; Class.TeacherId and ClassMembership.StudentId reference user Id string.
- **Angular 22 standalone:** Reactive forms, signals for class context, functional guards.
- **Vitest + xUnit:** Extend existing test patterns from Story 1.2.

### File Structure Requirements

```text
src/EnglishTestWeb.Api/
  Domain/Classes/
    Class.cs
    ClassMembership.cs
    ClassStatuses.cs          # constants: active, inactive
  Application/Classes/
    IClassService.cs
    ClassService.cs
  Contracts/Classes/
    ClassLookupResponse.cs
    ClassSummaryResponse.cs
    ClassDetailResponse.cs
    ClassStudentResponse.cs
  Controllers/
    ClassesController.cs
  Infrastructure/
    Persistence/Configurations/ClassConfiguration.cs
    Persistence/Configurations/ClassMembershipConfiguration.cs
    Persistence/Migrations/*AddClassesAndMemberships*
    Identity/MvpDemoDataSeeder.cs   # or extend IdentityDevUserSeeder

src/EnglishTestWeb.Client/src/app/
  core/
    classes/
      class-context.service.ts
      classes-api.service.ts
      classes.models.ts
    route-access/
      student.guard.ts
  features/
    student-class-entry/
    student-login/
    student-assigned-tests/     # placeholder
    teacher-classes/            # replaces placeholder at /teacher/classes

tests/EnglishTestWeb.Api.Tests/
  Classes/
    ClassesControllerTests.cs
    ClassLookupTests.cs
  Auth/
    StudentLoginTests.cs
```

### Testing Requirements

**API (xUnit + `TestApiFactory`):**

- MVP seed creates teacher, student, class, membership (idempotent second run).
- Lookup: valid code → 200 minimal DTO; unknown → `classes.codeNotFound`; inactive class → `classes.codeInactive`.
- Normalization: `"eng-7a"` matches `ENG7A`.
- Teacher `GET /api/classes` returns only owned classes; other teacher's class → forbidden/not found.
- Student login: valid member → 200 + activeClass; wrong password → `auth.loginInvalid`; valid student wrong class → `auth.notInClass`; teacher credentials on student endpoint → `auth.loginInvalid`.
- Lookup response body must not contain student list or assignment data.

**Angular (Vitest):**

- Class entry: required/format validation keys.
- Confirm navigates to `/student/login` with classCode query param.
- Student login: not-in-class shows `ERR_STUDENT_NOT_IN_CLASS` copy.
- Student guard: unauthenticated `/student/tests` → login or class entry.
- Teacher classes component renders class name, code, status, student names from mock API.

**Quality gate:** `.\scripts\quality.ps1` must pass before marking story done.

### UX / Visual Notes

- `/class`: single-purpose public page — UX 02.1 object IDs (`student-class-entry-*`).
- `/student/login`: class context card always visible — UX 02.2 (`student-login-*`).
- `/student/tests`: placeholder only — header + empty state; full list UX 02.3 thuộc Epic 4.
- `/teacher/classes`: read-only roster for seeded class; không cần create/edit class UI (out of scope).
- Copy tiếng Việt từ UX content tables.
- WCAG: visible focus, labels, keyboard submit on Enter.

### Anti-Patterns To Avoid

- Không build full class management CRUD/import LMS.
- Không implement reusable authorization handler framework (Story 1.4).
- Không implement assigned tests/homework list thật (Epic 4).
- Không expose full roster qua public lookup hoặc failed login responses.
- Không JWT / localStorage cho auth tokens.
- Không dùng teacher `/login` cho student — tách route `/student/login`.
- Không skip server-side membership check — client class context chỉ là UX convenience.
- Không duplicate normalization logic khác nhau giữa client và server.
- Không assert ProblemDetails message text trong tests.

### Previous Story Intelligence (1.2)

**Patterns đã establish — reuse:**

- `AuthService` + `IAuthService` boundary; `AuthController` ProblemDetails helper.
- `IdentityDevUserSeeder` idempotent pattern + config section + CLI `--seed-dev-teacher`.
- `AuthSessionService` signals, XSRF prefetch before POST, `mapApiError` with stable codes.
- `teacher.guard.ts` / `guest.guard.ts` / `sanitizeTeacherReturnUrl`.
- `POST /api/auth/testing/sign-in` for test cookie sessions.
- `TestApiFactory` in-memory DB + `AuthTestHelper` seed users.
- Teacher shell nav; placeholder component pattern for unfinished features.

**Explicit deferrals from 1.2 now in scope:**

- Class/Student/ClassMembership seed → **this story**.
- `/teacher/classes` real surface → **this story**.
- Student auth flow → **this story**.

**Still deferred:**

- Login rate limiting.
- Full authorization matrix (1.4).
- Assigned tests implementation (Epic 4).

**Review lessons from 1.2 apply:**

- Logout/session clear in `finally`; don't swallow errors without client state update.
- Test positive paths for new auth endpoints (`GET me` equivalent if adding student session fields).
- Guard specs for redirect edge cases (student missing class context).

### Git Intelligence

Recent commits:

- `e309adb` — feat: implement teacher login and app shell (story 1.2)
- `b413bac` — fix: harden story 1.1 baseline after code review
- `b9cc3b0` — chore: baseline story 1.1

**Insights:** Team ships vertical slices with Application abstractions, Infrastructure implementations, API tests on stable codes, Angular core-first then features. Story 1.3 should mirror 1.2 file layout (Contracts/Application/Controllers + core Angular services + feature folders + test helpers). Extend `AuthTestHelper` rather than duplicate seed logic.

### Latest Technical Information (2026-06-10)

- **EF Core 10 migrations:** Add entities via `DbSet` + `IEntityTypeConfiguration`, `dotnet ef migrations add AddClassesAndMemberships`. Unique index on normalized `ClassCode` prevents duplicate codes.
- **Identity user FK:** `TeacherId`/`StudentId` as `string` matching `ApplicationUser.Id` (Identity default GUID string).
- **ASP.NET Core `[AllowAnonymous]` lookup:** Rate limiting deferred but avoid enumerable responses — same 404 for not found vs unauthorized scope on teacher endpoints.
- **Angular query param preservation:** `router.navigate(['/student/login'], { queryParams: { classCode } })` survives refresh; re-fetch preview on login page init if service state empty.

### Project Context Reference

- `AGENTS.md` — stack boundaries, cookie auth, ProblemDetails codes, no JWT.
- `docs/setup/development.md` — extend with student/class seed smoke steps.
- `_bmad-output/implementation-artifacts/1-2-teacher-login-and-teacher-app-shell.md` — auth patterns, file list, review findings.
- `_bmad-output/implementation-artifacts/deferred-work.md` — rate limiting still deferred.

## References

- `_bmad-output/planning-artifacts/epics.md#Story 1.3`
- `_bmad-output/planning-artifacts/architecture.md#FR-1 to FR-3 Accounts, Roles, Classes, Access`
- `_bmad-output/planning-artifacts/architecture.md#Database Naming Conventions`
- `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md#FR-2`
- `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md#FR-3`
- `_bmad-output/C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.1-student-class-code-entry/2.1-student-class-code-entry.md`
- `_bmad-output/C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.2-student-login-account-access/2.2-student-login-account-access.md`
- `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml`
- `_bmad-output/implementation-artifacts/1-2-teacher-login-and-teacher-app-shell.md`

## Dev Agent Record

### Agent Model Used

Auto (Cursor)

### Debug Log References

- Entity đặt tên `SchoolClass` trong code để tránh nhầm với keyword C#; bảng SQL vẫn là `Classes`.
- Teacher class detail trả `403` + `classes.forbidden` khi không phải owner (inline check; Story 1.4 sẽ extract policy framework).

### Completion Notes List

- Domain: `SchoolClass`, `ClassMembership`, migration `AddClassesAndMemberships`, EF configurations.
- MVP seed: `MvpDemoDataSeeder`, `--seed-mvp-demo`, `Identity:SeedMvpDemoOnStartup`.
- API: `ClassesController` lookup/list/detail; `POST /api/auth/student/login` với membership enforcement.
- Angular: student class entry, login, tests placeholder; teacher classes roster; guards + class context services.
- Tests: 32 API + 34 Angular pass; `.\scripts\quality.ps1` pass.

### File List

- `src/EnglishTestWeb.Api/Domain/Classes/**`
- `src/EnglishTestWeb.Api/Application/Classes/**`
- `src/EnglishTestWeb.Api/Application/Auth/IAuthService.cs`
- `src/EnglishTestWeb.Api/Contracts/Classes/**`
- `src/EnglishTestWeb.Api/Contracts/Auth/ActiveClassResponse.cs`
- `src/EnglishTestWeb.Api/Contracts/Auth/StudentLoginRequest.cs`
- `src/EnglishTestWeb.Api/Contracts/Auth/StudentLoginResponse.cs`
- `src/EnglishTestWeb.Api/Controllers/ClassesController.cs`
- `src/EnglishTestWeb.Api/Controllers/AuthController.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Classes/ClassService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Identity/AuthService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Identity/MvpDemoDataSeeder.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/**`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/*AddClassesAndMemberships*`
- `src/EnglishTestWeb.Api/Program.cs`
- `src/EnglishTestWeb.Api/appsettings.Development.json`
- `src/EnglishTestWeb.Client/src/app/app.routes.ts`
- `src/EnglishTestWeb.Client/src/app/core/auth/**`
- `src/EnglishTestWeb.Client/src/app/core/classes/**`
- `src/EnglishTestWeb.Client/src/app/core/route-access/**`
- `src/EnglishTestWeb.Client/src/app/features/student-class-entry/**`
- `src/EnglishTestWeb.Client/src/app/features/student-login/**`
- `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/**`
- `src/EnglishTestWeb.Client/src/app/features/teacher-classes/**`
- `tests/EnglishTestWeb.Api.Tests/Classes/**`
- `tests/EnglishTestWeb.Api.Tests/Auth/StudentLoginTests.cs`
- `docs/setup/development.md`

### Change Log

- 2026-06-10: Story 1.3 — Class/Membership domain, MVP seed, class lookup, student login, teacher roster, student routes, tests, docs.
- 2026-06-10: Code review fixes — D1(B) unified loginInvalid for non-members; class confirm flag; session re-hydrate; XSRF fail-closed; tests expanded.

## Story Completion Status

Status: `done`

Completion note: Acceptance criteria met; review patches applied; quality gate passed (32 API tests, 34 Angular tests).

### Review Findings

_Review date: 2026-06-10 — baseline `e309adb` (story 1.2) vs story 1.3 work._

#### decision_needed (1) — resolved

| ID | Resolution |
|----|------------|
| D1 | **B applied** — non-member student login trả `401` + `auth.loginInvalid` (không còn `auth.notInClass`). |

#### patch (7) — all applied

| ID | Resolution |
|----|------------|
| P1 | `loadSession()` re-hydrate `activeClass` từ `sessionStorage` + lookup API cho student. |
| P2 | `ClassContextService.isConfirmedForClass()` + guard chặn deep-link chưa confirm; redirect về `/class`. |
| P3 | XSRF interceptor prefetch token khi store rỗng; `loadSession()` issue token sớm. |
| P4 | `studentLoginGuard` đọc `sessionStorage` khi thiếu query param. |
| P5 | Normalize `classCode` trước student login POST. |
| P6 | Tests: MVP seeder idempotency, inactive class login, guard cases, `student-login.spec.ts`, `class-code.spec.ts`. |
| P7 | Loading text trên class entry và login context card khi hydrate. |

#### defer (3)

| ID | Finding | Rationale |
|----|---------|-----------|
| DF1 | Membership chỉ kiểm tra lúc login, không revalidate mỗi request/API sau đó | Story 1.4 policy framework |
| DF2 | Rate limiting login/lookup | Đã defer từ story 1.2 |
| DF3 | Teacher roster chỉ auto-load class đầu tiên khi teacher có nhiều lớp | MVP single-class demo |

#### dismiss (4)

| ID | Finding | Rationale |
|----|---------|-----------|
| X1 | Oracle `codeNotFound` vs `codeInactive` trên lookup/login | Theo contract API trong story |
| X2 | Public lookup trả teacher display name | AC4 yêu cầu context giáo viên trên confirmation card |
| X3 | Class inactive giữa lookup và login | Hành vi mong đợi; user thấy lỗi rõ |
| X4 | Underscore/dash trong class code input | Spec: alphanumeric only |

#### Verdict

**Done** — D1(B) + P1–P7 applied; DF1–DF3 tracked in `deferred-work.md`.

### Re-Review (post-fix, 2026-06-10)

_Baseline: `e309adb` → current uncommitted work sau P1–P7._

#### patch (3) — applied

| ID | Resolution |
|----|------------|
| P8 | Block submit khi `isLoadingContext`; `contextHydrationGeneration` hủy hydration sau login; catch không clear nếu đã authenticated/activeClass. |
| P9 | `restoreStudentClassContext` clear stale persistence on lookup fail; `studentGuard` yêu cầu `activeClass()` cho student đã auth. |
| P10 | `AuthApiService.issueXsrfToken()` single-flight promise; test concurrent unsafe requests. |

#### defer (4)

| ID | Finding | Rationale |
|----|---------|-----------|
| DF4 | Không có server-side class binding sau login; `/me` không trả activeClass | Story 1.4 |
| DF5 | `restoreStudentClassContext` dùng public lookup, không check membership | Story 1.4 + DF4 |
| DF6 | Class code trong URL query (history/referrer) | UX refactor later |
| DF7 | Teacher email fallback trong anonymous lookup | MVP; harden khi có display-name policy |

#### dismiss (6)

| ID | Finding | Rationale |
|----|---------|-----------|
| X5 | AC7 UX message riêng cho not-in-class | **Accepted** — D1(B) product decision |
| X6 | sessionStorage confirm flag forgeable | UX gate only; login vẫn server-side membership |
| X7 | Class code enumeration oracle (404 vs 401) | Theo story API contract |
| X8 | Client-only confirm bypass | Same as X6 |
| X9 | sessionStorage throws in private mode | Edge browser; low MVP risk |
| X10 | Underscore/dot separators | Spec alphanumeric |

#### Re-review verdict

**Done** — P8–P10 applied; sẵn sàng commit.

### Final Review (post P8–P10, 2026-06-10)

_3 lớp review song song sau toàn bộ patch._

#### patch (1) — applied

| ID | Resolution |
|----|------------|
| P11 | `changeClass()` bump `contextHydrationGeneration` để hủy hydration in-flight. |

#### dismiss (4)

| ID | Finding | Rationale |
|----|---------|-----------|
| X11 | Guard bounce tests→class sau failed restore | Đúng kết quả, thừa 1 redirect |
| X12 | XSRF clear during in-flight logout | Hiếm; retry refresh đủ MVP |
| X13 | Restore lookup không check membership | DF4/DF5 deferred 1.4 |
| X14 | AC7 generic error copy | D1(B) accepted |

#### Final verdict

**Pass — approve commit.** AC coverage đạt; không blocker high/medium. Quality gate: 32 API + 36 Angular tests.
