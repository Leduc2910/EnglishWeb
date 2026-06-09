---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/addendum.md"
workflowType: "architecture"
project_name: "EnglishTestWeb"
user_name: "Duc"
date: "2026-06-09"
lastStep: 8
status: "complete"
completedAt: "2026-06-09"
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
PRD co 20 FR, gom thanh 7 nhom kien truc chinh: auth/role/class access; Thu vien de va De goc; HomeworkAssignment/LiveExamSession; student assigned work va attempt; Speaking upload/grading; Results/Dashboard; visual reference tu Stitch. Truc domain quan trong nhat la tach De goc khoi lan su dung de, va bat buoc Submission tham chieu dung mot HomeworkAssignment hoac mot LiveExamSession.

**Non-Functional Requirements:**
8 NFR tac dong truc tiep toi kien truc: initial load duoi 2s cho dashboard/list/results; autosave feedback duoi 1s; WCAG AA va keyboard accessibility; server-side role/scope guard; chong duplicate state transition; secure file access cho PDF/audio/Speaking; audit trail cho trang thai quan trong; desktop-first nhung responsive-safe.

**Scale & Complexity:**
- Primary domain: full-stack education workflow web app.
- Complexity level: medium-high.
- Estimated architectural components: 12.
- No epics/stories loaded yet; architecture se dua tren PRD/DD-001 lam source chinh.
- UX scope gom 3 scenarios, 15 pages, wizard, split workspace, file upload, PDF/audio playback, autosave, master-detail grading.

### Technical Constraints & Dependencies

- Web app desktop/laptop-first; mobile app native ngoai MVP.
- MVP dung PDF/audio upload + answer form rieng; khong parse PDF thanh cau hoi.
- Speaking upload-first; browser recording chua chac thuoc MVP.
- File storage la dependency bat buoc cho PDF, Listening audio va Speaking submissions.
- Authentication va role-based access la dependency bat buoc.
- PDF rendering va audio playback la dependency bat buoc.
- Homework deadline, Live Exam open/close, AnswerKey edit/versioning la cac quyet dinh can khoa trong architecture.
- Stitch la visual/layout reference; khong duoc override DD-001/WDS behavior/domain.

### Architectural Decomposition Insight

Architecture nen tach theo domain boundary, khong theo tung screen:

1. Identity & Class Access
2. Template Authoring
3. Assignment/Session Delivery
4. Attempt & Submission
5. Scoring & Grading
6. File/Media Access
7. Teacher Reporting
8. Shared UI Shell

This avoids coupling the implementation too tightly to the 15 WDS pages while still preserving the user flows.

### Highest-Risk Architectural Decisions

- AnswerKey versioning after submissions exist.
- Submission source integrity: exactly one HomeworkAssignment or LiveExamSession.
- LiveExamSession state model: scheduled/open/closed/manual/scheduled behavior.
- Homework deadline and whether extension/reopen is supported.
- File access authorization for PDF/audio/Speaking media.
- Autosave conflict behavior versus final submission locking.
- Reporting score stability: aggregates must read from the correct score snapshot/version.

### Business Invariants And Ownership Boundaries

- Submission belongs to exactly one HomeworkAssignment or one LiveExamSession; it must never be double-linked or source-less.
- AnswerKey used for grading must be immutable or versioned once attempts/submissions exist.
- Score records must preserve the AnswerKey version or scoring snapshot used at submission time.
- Final submission wins over autosave; autosave after final submit must be rejected or ignored.
- Student can only see Class, Assignment, Session, Submission and File resources allowed by ClassMembership and session state.
- Teacher can only manage resources they own or are explicitly assigned to manage.
- TestTemplate ownership and sharing policy must be decided before implementation; MVP assumes teacher-owned templates unless architecture changes this.

### Lifecycle And State Model Context

Architecture must explicitly define valid states, actors and transitions for:

- TestTemplate / De goc: draft, ready, archived.
- AnswerKey: draft, ready, locked/versioned.
- HomeworkAssignment: draft, published/assigned, closed, reopened, archived.
- LiveExamSession: draft/scheduled, open, locked/closed, grading, published/archived.
- Submission/Attempt: draft/autosaved, submitted, auto-graded, grading, graded, returned.
- SpeakingSubmission: draft uploaded, submitted, grading, graded.

Invalid transitions are as important as valid transitions: submitted work must not return to draft without an explicit reopen event, closed Live Exam must not accept new attempts, and AnswerKey edits must not silently change historical scores.

### Authorization And Scope Matrix Context

The architecture needs a Role x Resource x Action matrix covering at least Admin, Teacher and Student. Scope checks must include:

- Class scope via ClassMembership.
- Template ownership.
- Assignment/session ownership and participant access.
- Submission ownership and teacher review scope.
- File/media access scope for PDF, Listening audio and Speaking files.

Route/UI guards are not enough; every read/write involving Class, TestTemplate, HomeworkAssignment, LiveExamSession, Submission, SubmissionAnswer, SpeakingSubmission and file metadata must be guarded server-side.

### Concurrency, Idempotency And Data Integrity

The architecture should define transaction boundaries and idempotency strategy for:

- Creating HomeworkAssignment and LiveExamSession.
- Marking TestTemplate ready.
- Opening/closing/reopening sessions or assignments.
- Autosaving answers.
- Final submit.
- Saving Speaking grade/feedback.

Data integrity acceptance checks should include:

- AC-DI-01: AnswerKey edit does not alter historical scores.
- AC-DI-02: final submit locks attempt; autosave after submit is rejected.
- AC-DI-03: duplicate submit/create/save does not create duplicate records.
- AC-DI-04: homework reopen creates an audit event and preserves history.
- AC-DI-05: result views preserve source mode: Homework vs Live Exam.

### File, Media And Audit Context

File/media architecture must cover upload, attach, replace, view/play, revoke/delete/archive and recoverable missing-file errors. PDF/audio/Speaking files should not rely on long-lived public URLs; access must be scoped and revocable.

Audit/observability should capture at least: login, template ready, AnswerKey version change, assignment publish, live session open/close, autosave, final submit, homework reopen, grading save/change, file access denied and file missing/recovery events. Audit events should include actor, timestamp, resource id, previous state, next state and reason when relevant.

### Reporting And Fixture Implications

Reporting must be defined by stable aggregates: by class, by assignment/session, by student, by skill, by template and by mode. Dashboard/reporting should avoid recalculating historical scores from mutable AnswerKey state.

Minimum test fixtures should include: multiple roles, multiple classes, student in multiple classes, homework expired, homework reopened, live exam scheduled/open/closed, submitted attempt, autosave draft, AnswerKey v1/v2, valid/invalid files, missing media, and results across Homework and Live Exam modes.

### UX Architecture Context

Screen complexity is not uniform:

- Lower complexity: dashboard summary, login/account access, simple class code entry.
- Medium complexity: template library, assigned tests list, speaking upload, template review.
- High complexity: template wizard upload/answer key, Reading/Listening attempt workspace, Live Exam session controls, Results & Grading master-detail workspace.

State visibility is a product requirement, not styling detail:

- Live Exam must visibly show scheduled/open/closed/grading/published states.
- Homework must show deadline, closed/reopened status, submitted/late/missing state.
- Student attempt must distinguish autosaved, submitted, locked and reopened.
- Results must preserve mode context so teacher does not grade or report the wrong source.

Layout contracts should remain operational and scan-friendly: stable sidebar/header/content/action regions, split workspace for exam/grading, clear dangerous actions, and no marketing-style card-heavy surfaces for dense workflows. WDS page specs remain the source of truth for IA, flow, interaction, accessibility and layout contract. Stitch remains visual/component inspiration only; any deviation from WDS/domain constraints should be recorded.

### Cross-Cutting Concerns Identified

- Server-side authorization scope for Teacher ownership and Student ClassMembership.
- Submission mode integrity.
- State machines for De goc, HomeworkAssignment, LiveExamSession, Submission and SpeakingSubmission.
- Secure file lifecycle: upload, attach, replace, view/play, delete/archive.
- AnswerKey lifecycle: draft, ready, locked/versioned.
- Attempt lifecycle: draft/autosaved, submitted, auto-graded, manually graded where applicable.
- Auditability and observability around submission, grading, file access and state transitions.
- Measurable NFRs and quality gates for authorization, state transition/idempotency, score stability, autosave/final submit, secure file access, reporting mode preservation and accessibility.
- UI consistency: status badges, wizard, split workspace, table/master-detail patterns.

## Starter Template Evaluation

### Primary Technology Domain

Full-stack education workflow web app: ASP.NET Core Web API + Angular SPA, SQL Server, protected file/media delivery, IIS/Windows deployment.

### Starter Options Considered

- Legacy `dotnet new angular`: rejected. Although present locally, it is discontinued since .NET 8 and pushes the project toward older SPA-template behavior.
- Visual Studio Angular and ASP.NET Core template: viable for manual Visual Studio setup, but not selected as canonical because it depends on local VS/template behavior.
- Custom two-project CLI starter: selected. It keeps backend/frontend explicit for AI agents and leaves room for Identity, SQL Server, protected storage, autosave/submission integrity, Homework/Live Exam lifecycle and AnswerKey versioning.

### Selected Starter: Custom .NET 10 Web API + Angular 22 SPA

**Rationale for Selection:**
This gives EnglishTestWeb a deterministic current foundation while avoiding legacy SPA-template coupling. The API owns domain state, Identity, authorization, SQL Server, audit, file access, autosave/final-submit rules, Homework/Live Exam lifecycle and AnswerKey versioning. Angular owns the WDS/Stitch-informed operational UI.

**Initialization Command:**

```bash
dotnet new sln -n EnglishTestWeb
dotnet new webapi -n EnglishTestWeb.Api -o src/EnglishTestWeb.Api -f net10.0 --use-controllers
dotnet sln EnglishTestWeb.sln add src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj
npx @angular/cli@22 new english-test-web-client --directory src/EnglishTestWeb.Client --routing --style css --standalone --strict --test-runner vitest --package-manager npm --skip-git
```

**Preconditions:**
Use .NET 10 LTS and stay current on patch updates. Upgrade local Node before Angular 22 scaffolding because local Node `22.17.0` is below Angular 22's active-support requirement. Add `global.json` for .NET SDK pinning and frontend engine notes for Node pinning.

**Architectural Decisions Provided by Starter:**

**Language & Runtime:**
C# / ASP.NET Core Web API on .NET 10, TypeScript / Angular 22, Node only for frontend build/development.

**Authentication:**
ASP.NET Core Identity will be added manually to the Web API with EF Core SQL Server stores. Same-origin deployment should be preferred for MVP to reduce SPA auth and protected-file complexity.

**Database:**
SQL Server through EF Core migrations. Identity schema and EnglishTestWeb domain schema share one database unless later split by deployment need.

**File Storage:**
Add an `IFileStorage` abstraction with a local protected-disk implementation for development. Uploaded PDF/audio/Speaking files stay outside public `wwwroot` and are served through authorized API endpoints.

**Styling Solution:**
Angular component CSS plus shared design tokens. No Bootstrap/default template UI. Stitch is visual/layout reference; WDS/DD-001 remain behavior/domain authority.

**Build Tooling:**
ASP.NET Core provides API routing, OpenAPI and IIS publish support. Angular CLI provides the SPA workspace. Development uses an Angular proxy to the API; production can publish Angular build output into API `wwwroot` for same-origin IIS hosting.

**Testing Framework:**
Angular uses Vitest from the CLI starter. API tests and Playwright E2E should be added as explicit implementation stories for auth, file access, autosave, submission locking and AnswerKey versioning.

**Code Organization:**
Keep `/src/EnglishTestWeb.Api` and `/src/EnglishTestWeb.Client` separate. API code should be organized by domain/application/infrastructure boundaries rather than by screen. Angular code should map to routes/workspaces while sharing status, guard, upload, media and autosave components.

**Development Experience:**
Run API and Angular separately in development with CORS/proxy configured explicitly. Production should publish a single IIS-hosted ASP.NET Core site serving both API and SPA assets when possible.

**Note:** Project initialization using this selected starter should be the first implementation story.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
- Codebase is separated into `EnglishTestWeb.Api` and `EnglishTestWeb.Client`, but production deploy is same-origin through ASP.NET Core on IIS.
- Backend uses ASP.NET Core 10 Web API, ASP.NET Core Identity, EF Core 10 and SQL Server.
- Browser authentication uses cookie-only ASP.NET Core Identity flow; Angular must not store access tokens in `localStorage` or `sessionStorage`.
- Authorization is server-side role plus resource/scope based.
- Protected PDF/audio/Speaking files are stored outside public `wwwroot` and streamed only through authorized API endpoints.
- Submission must reference exactly one source mode: HomeworkAssignment or LiveExamSession.
- AnswerKey is locked/versioned once submissions exist.
- Autosave, final submit, grading save and lifecycle transitions must be idempotent and concurrency-safe.

**Important Decisions (Shape Architecture):**
- Angular 22 standalone SPA with route-level feature organization.
- REST controller API with OpenAPI for development.
- `ProblemDetails` plus stable business error codes.
- Structured logs plus audit table for security and domain events.
- Same-origin production deploy to reduce CORS, cookie and protected-media complexity.

**Deferred Decisions (Post-MVP):**
- Separate API and SPA domains.
- Token/OIDC mode for mobile apps or external clients.
- Cloud object storage.
- NgRx/global store.
- CDN/media acceleration.
- Full antivirus/quarantine pipeline, though storage abstraction must leave a hook for it.

### Data Architecture

Use EF Core 10 code-first migrations with SQL Server.

Core data integrity is enforced in both database and application layer:
- Check constraint: `Submission` has exactly one `HomeworkAssignmentId` or one `LiveExamSessionId`.
- `rowversion` concurrency tokens for attempts, submissions, assignments, live sessions and AnswerKey versions.
- UTC/`DateTimeOffset` timestamps for deadlines, open/close windows, submissions and audit events.
- Score records store the `AnswerKeyVersionId` or scoring snapshot used at grading time.
- AnswerKey edits after submissions create a new version; historical submissions never rebind silently.
- Domain services own state transitions for publish, open, close, reopen, autosave, final submit and grading.

Transaction boundaries:
- Create HomeworkAssignment / LiveExamSession.
- Publish assignment.
- Open/close/reopen Live Exam.
- Autosave answers.
- Final submit.
- Save Speaking grade/feedback.
- Create AnswerKey version.

Caching is conservative for MVP:
- No cache for authorization-critical, grading-critical or file-access decisions.
- Dashboard/list caching can be added later only with explicit invalidation rules.

### Authentication & Security

Use ASP.NET Core Identity with SQL Server stores.

Auth mode:
- Same-origin cookie authentication for Angular browser app.
- Auth cookie is `HttpOnly`, `Secure` and SameSite-aware.
- Angular does not store bearer tokens in browser storage.
- Token mode is deferred unless future mobile/external clients require it.

CSRF protection:
- All unsafe state-changing requests validate antiforgery protection: `POST`, `PUT`, `PATCH`, `DELETE`.
- Angular sends the configured XSRF header.
- The auth cookie remains `HttpOnly`; any XSRF token exposed to Angular is not treated as a credential.

Roles:
- `Admin`
- `Teacher`
- `Student`

Authorization:
- Role checks decide broad capability only.
- Resource policy handlers decide actual access by class membership, teacher ownership, assignment/session participation, submission ownership and file ownership.
- Angular route guards are UX helpers only; every API endpoint must enforce server-side authorization.
- Every protected file request repeats authorization checks before streaming.

Security middleware:
- HTTPS required in production.
- HSTS enabled in production.
- Production CORS is deny-by-default because app and API are same-origin.
- Development uses Angular proxy or tightly scoped localhost CORS only.
- Rate limiting applies to login, upload, autosave and submit endpoints.
- Data Protection keys are persisted outside the repo with restricted ACLs so auth cookies survive deploy/restart.

### API & Communication Patterns

Use RESTful controller APIs with DTOs and OpenAPI.

Standards:
- OpenAPI enabled for development; production access restricted.
- `ProblemDetails` for error shape.
- Stable business error codes for Angular handling.
- `409 Conflict` for concurrency or state-transition conflicts.
- `403 Forbidden` for authorized user lacking resource scope.
- `404 Not Found` may be used to avoid leaking existence of resources outside the user's scope.

Idempotency:
- Mutating operations that can be retried accept an idempotency key.
- Store idempotency records unique by `UserId + Operation + Key`.
- Duplicate retries return the original result instead of creating duplicate assignments, sessions, submissions or grades.
- Final submit runs in a transaction and rejects later autosave writes.

### File, Media & Protected Storage

Use an `IFileStorage` abstraction.

Local development implementation:
- Store files outside `wwwroot`.
- Persist generated storage keys, not user-provided filenames.
- Keep metadata in SQL Server: owner/scope, content type, size, checksum, storage key, created by, created at, status.
- Validate extension, MIME type, file size and expected file category.
- Never concatenate user input into physical paths.
- Original filename is display metadata only.

Access rules:
- PDF, Listening audio and Speaking files are never public static assets.
- View/play/download goes through authorized API endpoints.
- API supports range streaming where needed for PDF/audio playback.
- File access denied, missing-file and file-replacement events are audited.
- Storage abstraction leaves a future hook for antivirus/quarantine without changing domain code.

### Frontend Architecture

Use Angular 22 standalone components with route-level feature organization.

Runtime/tooling:
- Angular 22 requires a compatible Node version; local Node must be upgraded before scaffolding.
- TypeScript strict mode stays enabled.

State:
- Angular services + signals/RxJS for MVP.
- No NgRx initially; add later only if cross-screen state becomes hard to reason about.
- Autosave state is explicit in UI: saving, saved, conflict, offline/error, submitted/locked.

UI architecture:
- Route guards for role-aware navigation.
- Shared components for status badges, upload, protected media viewer, autosave state, deadline/live session state and grading panels.
- WDS/DD-001 define behavior and flows.
- Stitch remains visual/layout reference and must not override domain behavior.

### Infrastructure & Deployment

Production deploy uses IIS/Windows Server with ASP.NET Core Hosting Bundle.

Deployment shape:
- Angular and API are separate source projects.
- Angular production build is served by the ASP.NET Core app for same-origin security.
- API routes remain under `/api`.
- Protected files remain outside `wwwroot`.
- No broad production CORS policy.

Configuration:
- Environment-specific `appsettings`.
- Secrets provided through IIS/environment/secret store, not committed.
- Persist ASP.NET Core Data Protection keys outside the repo.
- Separate upload/storage root per environment.
- Structured logs plus audit table for login, assignment publish, live open/close, autosave, final submit, grading, AnswerKey version changes, file missing and file access denial.

### Decision Impact Analysis

**Implementation Sequence:**
1. Create solution, API project and Angular project.
2. Upgrade/pin compatible Node for Angular 22 and pin .NET SDK.
3. Add EF Core, SQL Server, Identity and roles.
4. Add domain models, constraints and migrations.
5. Add policy/resource authorization.
6. Add antiforgery, secure cookie settings, rate limiting and security middleware.
7. Add protected storage abstraction and local implementation.
8. Add Homework/Live Exam/Submission state transitions.
9. Add autosave/final submit idempotency and concurrency handling.
10. Add AnswerKey versioning and score snapshot behavior.
11. Add Angular route shell, guards and shared workflow components.
12. Add audit, logs and security tests.

**Cross-Component Dependencies:**
- Auth and class membership must exist before protected files, assignments, sessions and submissions.
- Submission integrity depends on database constraints and service transaction rules.
- Reporting depends on stable score snapshots and AnswerKey versioning.
- Angular attempt/grading screens depend on API state models being explicit and stable.
- Same-origin deployment keeps cookie, CSRF and protected-media behavior simpler and safer for MVP.

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

**Critical Conflict Points Identified:**
12 areas where AI agents could make incompatible choices: database naming, API naming, DTO naming, project structure, validation, response/error formats, pagination, state/enum serialization, protected file handling, audit/idempotency, Angular state/loading patterns and testing layout.

### Naming Patterns

**Database Naming Conventions:**
- SQL Server tables use PascalCase plural names: `Classes`, `TestTemplates`, `HomeworkAssignments`, `LiveExamSessions`, `Submissions`.
- Identity tables keep ASP.NET Core Identity defaults unless explicitly configured: `AspNetUsers`, `AspNetRoles`. Keep the default Identity schema initially; customize only when there is a clear business need.
- Columns use PascalCase: `ClassId`, `TeacherId`, `CreatedAt`, `RowVersion`.
- Foreign keys use `{EntityName}Id`: `StudentId`, `AnswerKeyVersionId`.
- Indexes use EF/SQL Server style: `IX_Submissions_HomeworkAssignmentId`.
- Constraints use explicit prefixes: `CK_Submissions_ExactlyOneSource`, `FK_Submissions_HomeworkAssignments_HomeworkAssignmentId`.
- EF Core migration names are intent-based: `CreateLiveExamSessions`, `AddHomeworkPublishing`, `AddAnswerKeyVersioning`.

**API Naming Conventions:**
- MVP uses unversioned `/api/...` routes. Add `/api/v2` only for future breaking changes.
- REST paths use lowercase plural resources: `/api/classes`, `/api/test-templates`, `/api/homework-assignments`.
- State transitions use action subresources:
  - `POST /api/submissions/{id}/autosave`
  - `POST /api/submissions/{id}/submit`
  - `POST /api/live-exam-sessions/{id}/open`
  - `POST /api/live-exam-sessions/{id}/close`
  - `POST /api/homework-assignments/{id}/reopen`
- Avoid RPC-style root endpoints such as `/api/submitSubmission`, `/api/openExam` or `/api/final-submit/{id}`.
- Route parameters use `{id}` in ASP.NET routes and `:id` in Angular routes.
- User-facing domain resource IDs should use GUIDs unless a specific entity has a documented reason to use integer IDs.
- GUIDs serialize as lowercase canonical strings.
- Query/body JSON fields use camelCase.
- Custom headers use clear names: `X-Idempotency-Key`, `X-Correlation-Id`, `X-XSRF-TOKEN`.
- Protected media endpoints stay resource-scoped: `/api/files/{fileId}` for authorized metadata and `/api/files/{fileId}/content` for authorized content.

**DTO Naming Conventions:**
- Request DTOs use action names: `CreateHomeworkAssignmentRequest`, `UpdateAnswerKeyRequest`, `SubmitSubmissionRequest`.
- Response DTOs use resource names: `HomeworkAssignmentDto`, `SubmissionDto`, `LiveExamSessionDto`.
- Do not use ambiguous suffixes like `Model`, `ViewModel`, `Payload` or `Input`.
- Student-facing DTOs must never include `answerKey`, `correctAnswer`, `solution` or equivalent fields.

**Code Naming Conventions:**
- C# types use PascalCase: `SubmissionService`, `AnswerKeyVersion`.
- C# private fields use `_camelCase`.
- Angular files use kebab-case: `assignment-list.component.ts`, `protected-media.service.ts`.
- Angular classes use PascalCase and variables/functions use camelCase.
- Shared domain terms must stay consistent: `TestTemplate`, `HomeworkAssignment`, `LiveExamSession`, `Submission`, `AnswerKeyVersion`.

### Structure Patterns

**Project Organization:**
- API source lives in `src/EnglishTestWeb.Api`.
- Angular source lives in `src/EnglishTestWeb.Client`.
- API code is grouped by architectural role: `Domain`, `Application`, `Infrastructure`, `Controllers`.
- Follow layers where behavior exists; simple read endpoints may stay thin but must not bypass authorization, validation, audit or response contracts.
- Angular code is grouped by feature routes plus shared primitives.
- API tests live under `tests/EnglishTestWeb.Api.Tests`.
- Angular unit/component tests are co-located as `.spec.ts`.
- E2E tests live under `tests/EnglishTestWeb.E2E`.

**File Structure Patterns:**
- API DTOs are not EF entities.
- Controllers call application services, not `DbContext` directly.
- Application services/command handlers are transaction boundaries for state-changing operations.
- Controllers must not compose multi-step domain transitions directly.
- Authorization policies/handlers live in a dedicated security area.
- Resource policies use capability names such as `CanViewHomework`, `CanManageExam`, `CanAccessSubmission`, `CanDownloadFile`.
- File storage implementations live behind `IFileStorage`.
- Angular shared components are only for reusable UI/workflow primitives, not page-specific code.

### Format Patterns

**API Response Formats:**
- Successful single-resource responses return the DTO directly.
- `POST create` returns `201 Created` with DTO and `Location`.
- Delete/archive commands return `204 No Content` when no body is needed.
- State transition commands return the updated DTO when UI needs refreshed state.
- List responses with metadata use `{ items, page, pageSize, totalCount }`.
- Errors use `ProblemDetails`.
- `ProblemDetails.extensions.code` is required for business/API errors.
- Code namespaces use dot notation: `auth.*`, `validation.*`, `homework.*`, `submission.*`, `liveExam.*`, `answerKey.*`, `file.*`, `system.*`.
- Validation errors include field-level details using JSON field names.
- Concurrency/state conflicts return `409 Conflict`.
- Tests assert error codes, not user-facing message text.

**Pagination, Filtering & Sorting:**
- List endpoints use `page`, `pageSize`, `sort`, `direction`, `q`.
- Pagination is 1-based.
- `direction` values are `asc` or `desc`.
- Default `pageSize` and max `pageSize` must be explicit; max default is 100 unless a smaller endpoint-specific limit is documented.
- Every paginated endpoint must define deterministic default sorting.
- Filter parameters use camelCase query names.
- Do not invent endpoint-specific pagination shapes.

**Data Exchange Formats:**
- JSON uses camelCase.
- Dates use ISO 8601 UTC strings.
- API rejects local-only ambiguous timestamps.
- Server persists and returns UTC timestamps.
- IDs are opaque to the frontend.
- Null means unknown/not set; empty arrays mean no items.
- State/enum values serialize as stable strings, not integers: `draft`, `published`, `open`, `closed`, `submitted`, `locked`.
- Canonical string states and valid transitions must be documented before implementation.
- Score values use decimal-compatible server types with explicit scale and rounding rules.
- Important editable aggregates expose `rowVersion` in DTOs.

### Communication Patterns

**Audit Event Patterns:**
- Audit event names use dot notation: `assignment.published`, `liveExam.opened`, `submission.finalSubmitted`, `file.accessDenied`.
- Audit payloads include actor id, role, resource id, previous state, next state, timestamp, correlation id and reason where relevant.
- Audit events are append-only.
- Security/audit logs include correlation id and actor id, but avoid sensitive answer content, passwords, cookies, tokens, raw file paths and full original filenames.
- Admin/Teacher impersonation, if ever added, must create explicit audit events.

**Idempotency & Correlation Patterns:**
- Retriable command operations use `X-Idempotency-Key`.
- Idempotency applies to final submit, autosave, create assignment/session, grading save and upload finalize.
- Idempotency keys are not required for GET and should not be applied mechanically to every PUT/PATCH.
- Idempotency records store user id, method, route, operation name, request hash, result status and expiry.
- Reusing the same `X-Idempotency-Key` with the same request returns the original result.
- Reusing the same `X-Idempotency-Key` with a different request body returns `409 Conflict` with a stable idempotency code.
- Requests accept or generate `X-Correlation-Id`.
- Correlation id appears in logs, audit events and error responses where safe.

**State Management Patterns:**
- Backend state transitions are owned by domain/application services.
- Server state is the source of truth.
- Server is authoritative for deadline, open/close, lock and submitted state.
- Client displays countdown from server time/offset but never decides whether a submission is still allowed.
- Server state flows through Angular feature services and RxJS.
- Local component UI state may use signals.
- Do not introduce NgRx/global store unless a future architecture decision approves it.
- Risky domain actions are not optimistic: submit, grading finalization, live exam open/close, template publish and lock/unlock wait for server confirmation.

### Process Patterns

**Validation Patterns:**
- Server validation is authoritative.
- Angular validation exists for immediate UX only.
- Server rejects invalid role/scope/state transitions even if Angular hides the action.
- Validation failures use `ProblemDetails` with stable field names and business codes.
- Angular must not parse free-text error messages for behavior.

**Authorization Patterns:**
- Role checks decide broad capability only; resource policy handlers decide actual access.
- Authorization-sensitive EF queries should filter by actor scope before materialization where practical.
- Authenticated users receive `404` for resources outside their scope to reduce enumeration.
- A visible resource with a disallowed action returns `403`.
- Angular route guards are UX helpers only and never replace API authorization.

**Error Handling Patterns:**
- Angular maps business error codes to user-facing messages.
- Frontend error categories are validation, permission, network, media unavailable, deadline/lock, conflict and server processing.
- Each error category has a standard placement and CTA: inline, toast, banner, modal, retry or escalation.
- Security-sensitive errors must not leak resource existence.
- API logs internal exception detail; UI receives safe messages only.
- Retry is allowed for idempotent operations only.

**Loading State Patterns:**
- Each Angular workflow surface exposes explicit states: `idle`, `loading`, `saving`, `saved`, `error`, `conflict`, `submitted`, `locked`.
- Workflow status precedence is `locked > submitted > deadlinePassed > conflict > error > saving > saved`.
- Autosave UI must distinguish saving, saved, failed, conflict and final-submitted.
- Buttons that trigger state transitions must prevent duplicate submit while request is pending.
- Each page has one primary status region; child components show scoped or secondary status only.

**Frontend Workflow Patterns:**
- Angular uses a shared API client/interceptor layer for credentials, XSRF, correlation id and `ProblemDetails` mapping.
- Feature components call feature services, not raw `HttpClient` directly.
- Feature services return typed models; raw `HttpResponse` is reserved for file/range cases.
- Cross-feature navigation guards use one shared pattern for unsaved changes, uploading media, pending autosave, submission in progress and expired deadlines.
- Controls tied to permissions or locked state should remain visible but disabled with a reason when the user is allowed to know the capability exists.
- Reading/Listening attempt screens keep stable layout regions: passage/media, question list, answer panel, timer/status bar.
- Master-detail grading/results screens standardize selected row, empty detail, loading detail, stale detail, locked grading and dirty grading edits.
- Template wizard steps cannot skip required prerequisites; step validity comes from form/schema validation; review step is a read-only snapshot before publish.
- Each Angular route-level feature should document required permissions, server queries, mutation actions, dirty-state sources, lock/deadline dependencies, primary status region and allowed shared components.
- Stitch is layout/visual reference only; interaction behavior, copy, validation, state precedence and workflow guards come from WDS/DD-001 and Step 5 contracts.

**File, Media & Protected Storage Patterns:**
- PDF, Listening audio and Speaking files are never public static assets.
- Store files outside `wwwroot`.
- Persist generated storage keys, not user-provided filenames.
- Keep metadata in SQL Server: owner/scope, content type, size, checksum, storage key, created by, created at, status.
- Validate extension, MIME type, file size and expected file category.
- Never concatenate user input into physical paths.
- Original filename is display metadata only.
- File responses set server-controlled `Content-Type`, `Content-Disposition` and private/no-store cache headers as appropriate.
- Every full and range request repeats authorization before returning bytes.
- Missing file metadata and missing physical file are separate error cases.
- Protected file responses must not be cached publicly.
- Storage abstraction leaves a future hook for antivirus/quarantine without changing domain code.

**Archive/Delete Patterns:**
- User-facing educational content uses archive/soft-delete by default: HomeworkAssignment, LiveExamSession, TestTemplate, AnswerKeyVersion and file metadata.
- Hard delete is reserved for explicit cleanup/admin workflows and must preserve audit needs.

**Background Job Patterns:**
- If deadline auto-close, cleanup files or exam finalization are added, use a hosted service/job abstraction.
- Do not place scheduled logic in controllers or Angular.

### Enforcement Guidelines

**All AI Agents MUST:**
- Follow naming conventions before adding entities, endpoints, DTOs, Angular routes or tests.
- Keep authorization, validation and state transitions server-side even when Angular has guards.
- Use `ProblemDetails`, stable business error codes and `X-Idempotency-Key` for retriable mutations.
- Use string states/enums in API contracts.
- Keep protected files outside `wwwroot` and stream them only through authorized API endpoints.
- Preserve DD-001/WDS domain behavior over Stitch visual references.

**Pattern Enforcement:**
- Pattern violations should be fixed before continuing implementation.
- New patterns must be added to this architecture document before code relies on them.
- Tests should cover auth scope, validation, idempotency, submission locking, AnswerKey versioning and protected file access.

**Test & Fixture Rules:**
- Tests use shared builders/factories for users, classes, assignments, live exams, submissions, files and AnswerKey versions.
- Test fixtures explicitly set role, class membership, deadline/open-close window, file ownership and AnswerKey version.
- Time-based tests use a fake/server clock abstraction.
- Every protected endpoint has tests for unauthenticated, wrong scope/role, hidden resource policy and success.
- Every transition endpoint has tests for unauthorized, wrong role, invalid state, concurrency conflict and idempotent retry where applicable.
- Tests include duplicate submit with the same idempotency key.
- Tests include same idempotency key with different payload.
- Tests include submit exactly as deadline/session closes.
- Tests include protected file range request with allowed and denied users.
- Tests include AnswerKey version change after a previous submission.
- E2E minimum flows: student homework submit, homework reopen, live exam close, protected file access and AnswerKey version stability.

### Pattern Examples

**Good Examples:**
- `POST /api/submissions/{id}/submit`
- `POST /api/live-exam-sessions/{id}/open`
- `GET /api/files/{fileId}`
- `GET /api/files/{fileId}/content`
- `CK_Submissions_ExactlyOneSource`
- `CreateHomeworkAssignmentRequest`
- `submission.alreadySubmitted`
- `answerKey.versionCreated`
- `protected-media.service.ts`
- `{ items, page, pageSize, totalCount }`

**Anti-Patterns:**
- Public file URLs under `wwwroot/uploads`.
- Angular-only authorization checks.
- Mixed endpoint styles such as `/api/getClass`, `/api/classes/submit` and `/api/submitSubmission`.
- Returning ad hoc errors like `{ message: "failed" }`.
- Serializing states as integers.
- Recalculating historical scores from the latest AnswerKey.
- Student DTOs that include answer keys or correct answers.

## Project Structure & Boundaries

### Complete Project Directory Structure

```text
EnglishTestWeb/
├── EnglishTestWeb.sln
├── global.json
├── README.md
├── AGENTS.md
├── .gitignore
├── .github/
│   └── workflows/
│       └── ci.yml
├── docs/
│   ├── agent-conventions.md
│   ├── architecture/
│   │   └── boundaries.md
│   ├── design-system/
│   │   ├── status.md
│   │   ├── forms.md
│   │   ├── tables.md
│   │   ├── navigation.md
│   │   ├── dialogs.md
│   │   └── responsive.md
│   └── deploy/
│       ├── runtime-storage.md
│       └── data-protection.md
├── src/
│   ├── EnglishTestWeb.Api/
│   │   ├── EnglishTestWeb.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Contracts/
│   │   │   ├── Auth/
│   │   │   ├── Classes/
│   │   │   ├── TestTemplates/
│   │   │   ├── HomeworkAssignments/
│   │   │   ├── LiveExamSessions/
│   │   │   ├── Submissions/
│   │   │   ├── Speaking/
│   │   │   ├── Results/
│   │   │   ├── Files/
│   │   │   └── Common/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── ClassesController.cs
│   │   │   ├── TestTemplatesController.cs
│   │   │   ├── HomeworkAssignmentsController.cs
│   │   │   ├── LiveExamSessionsController.cs
│   │   │   ├── SubmissionsController.cs
│   │   │   ├── FilesController.cs
│   │   │   └── ResultsController.cs
│   │   ├── Domain/
│   │   │   ├── Common/
│   │   │   ├── Identity/
│   │   │   ├── Classes/
│   │   │   ├── TestTemplates/
│   │   │   ├── Assignments/
│   │   │   ├── LiveExams/
│   │   │   ├── Submissions/
│   │   │   ├── Grading/
│   │   │   ├── Files/
│   │   │   └── Audit/
│   │   ├── Application/
│   │   │   ├── Abstractions/
│   │   │   ├── Auth/
│   │   │   ├── Classes/
│   │   │   ├── TestTemplates/
│   │   │   ├── HomeworkAssignments/
│   │   │   ├── LiveExamSessions/
│   │   │   ├── Submissions/
│   │   │   ├── Speaking/
│   │   │   ├── Results/
│   │   │   ├── Files/
│   │   │   └── Common/
│   │   │       ├── CrossCutting/
│   │   │       ├── Errors/
│   │   │       ├── Idempotency/
│   │   │       ├── Time/
│   │   │       └── Validation/
│   │   ├── Infrastructure/
│   │   │   ├── Persistence/
│   │   │   │   ├── EnglishTestWebDbContext.cs
│   │   │   │   ├── Configurations/
│   │   │   │   └── Migrations/
│   │   │   ├── Identity/
│   │   │   ├── Authorization/
│   │   │   │   ├── Policies/
│   │   │   │   └── Handlers/
│   │   │   ├── Storage/
│   │   │   ├── Audit/
│   │   │   ├── Idempotency/
│   │   │   ├── Clock/
│   │   │   └── BackgroundJobs/
│   │   ├── Security/
│   │   │   ├── Antiforgery/
│   │   │   ├── RateLimiting/
│   │   │   └── Headers/
│   │   └── wwwroot/
│   │       └── app/
│   └── EnglishTestWeb.Client/
│       ├── package.json
│       ├── angular.json
│       ├── proxy.conf.json
│       ├── src/
│       │   ├── app/
│       │   │   ├── app.config.ts
│       │   │   ├── app.routes.ts
│       │   │   ├── core/
│       │   │   │   ├── api/
│       │   │   │   ├── auth/
│       │   │   │   ├── interceptors/
│       │   │   │   ├── route-access/
│       │   │   │   ├── errors/
│       │   │   │   ├── status-region/
│       │   │   │   └── time/
│       │   │   ├── shared/
│       │   │   │   ├── ui/
│       │   │   │   ├── patterns/
│       │   │   │   │   ├── upload-queue/
│       │   │   │   │   ├── protected-media/
│       │   │   │   │   ├── submission-status/
│       │   │   │   │   └── autosave-status/
│       │   │   │   ├── layouts/
│       │   │   │   │   ├── teacher-shell/
│       │   │   │   │   ├── student-shell/
│       │   │   │   │   ├── attempt-shell/
│       │   │   │   │   └── grading-shell/
│       │   │   │   ├── forms/
│       │   │   │   └── feedback/
│       │   │   └── features/
│       │   │       ├── teacher-dashboard/
│       │   │       │   └── feature.contract.md
│       │   │       ├── student-class-entry/
│       │   │       │   └── feature.contract.md
│       │   │       ├── test-templates/
│       │   │       │   └── feature.contract.md
│       │   │       ├── homework-assignments/
│       │   │       │   └── feature.contract.md
│       │   │       ├── live-exam-sessions/
│       │   │       │   └── feature.contract.md
│       │   │       ├── assigned-tests/
│       │   │       │   └── feature.contract.md
│       │   │       ├── attempt-workspace/
│       │   │       │   └── feature.contract.md
│       │   │       ├── speaking-submission/
│       │   │       │   └── feature.contract.md
│       │   │       └── results-grading/
│       │   │           └── feature.contract.md
│       │   ├── environments/
│       │   └── styles.css
│       └── public/
├── tests/
│   ├── EnglishTestWeb.Api.Tests/
│   │   ├── Architecture/
│   │   ├── Unit/
│   │   ├── Integration/
│   │   ├── Security/
│   │   │   └── AuthorizationMatrixTests.cs
│   │   ├── ApiContract/
│   │   ├── Domain/
│   │   │   └── AnswerKeys/
│   │   └── TestKit/
│   │       ├── Builders/
│   │       ├── Fakes/
│   │       ├── Auth/
│   │       ├── Clock/
│   │       └── Database/
│   └── EnglishTestWeb.E2E/
│       ├── playwright.config.ts
│       ├── fixtures/
│       └── flows/
│           ├── login-and-class-access/
│           ├── homework-autosave-submit/
│           ├── live-exam-open-close/
│           ├── protected-media-access/
│           └── reporting-smoke/
└── deploy/
    └── iis/
        ├── web.config.template
        └── scripts/
            ├── publish.ps1
            └── verify.ps1
```

### Architectural Boundaries

**API Boundaries:**
- `Contracts/` is the API DTO surface: request DTOs, response DTOs, route constants if used, and serialization contracts.
- Controllers expose REST endpoints only and delegate behavior to application use-cases/services.
- Controllers do not access `DbContext`, `UserManager`, filesystem APIs or domain mutation logic directly.
- Application services own transactions, validation, idempotency and state transitions.
- Domain contains entities, invariants and state rules; it does not know about HTTP, Angular, EF Core, Identity stores or filesystem paths.
- Infrastructure owns EF Core persistence, Identity integration, authorization handlers, protected storage implementations, audit persistence and hosted jobs.
- Protected files cross the HTTP boundary only through `FilesController` and `IFileStorage`.

**Dependency Direction:**
- `Controllers -> Application -> Domain`.
- `Application -> abstractions only` for storage, clock, current user, idempotency and background jobs.
- `Infrastructure -> implements Application abstractions`.
- `Angular features -> core/api and feature services`, not backend internals.
- `Tests -> approved fixtures/builders`.
- `Common` folders are for abstractions or utilities genuinely shared by multiple features, not miscellaneous dumping grounds.

**Identity Boundary:**
- `Infrastructure/Identity` integrates ASP.NET Core Identity stores and services.
- `Domain/Identity` may reference user identifiers and app-specific profile concepts only.
- Domain must not wrap or duplicate ASP.NET Core Identity user, role, claims, password, lockout or token internals.

**Component Boundaries:**
- Angular route-level features own workflow-specific pages, components, dialogs, models, services, guards and local state.
- `core/` owns API client, auth, interceptors, route access, error mapping, status region contracts and server-clock services.
- `shared/ui` contains primitive reusable UI components.
- `shared/patterns` contains workflow components such as upload queue, protected media viewer, submission status and autosave status.
- `shared/layouts` contains `TeacherShell`, `StudentShell`, `AttemptShell` and `GradingShell`.
- `shared/forms` and `shared/feedback` contain reusable form and empty/loading/error/status primitives.
- Feature components call feature services/facades; only `core/api` or feature data services use raw HTTP.

**Data Boundaries:**
- SQL Server is the source of truth for Identity, domain records, file metadata, audit and idempotency records.
- Physical files are outside `wwwroot`; database stores metadata and generated storage keys.
- Score/history reads must use `AnswerKeyVersionId` or scoring snapshot.
- Runtime protected storage paths are configured outside the repository and documented in deployment docs.

### Requirements To Structure Mapping

**FR-1 to FR-3 Accounts, Roles, Classes, Access**
- API: `Domain/Identity`, `Domain/Classes`, `Application/Auth`, `Application/Classes`, `Contracts/Auth`, `Contracts/Classes`, `Infrastructure/Identity`, `Infrastructure/Authorization`.
- Angular: `core/auth`, `core/route-access`, `features/student-class-entry`.
- Tests: `Security/AuthorizationMatrixTests.cs`, `ApiContract/Auth`, `Integration/Classes`.

**FR-4 to FR-7 Template Library And Test Creation**
- API: `Domain/TestTemplates`, `Application/TestTemplates`, `Contracts/TestTemplates`, `Application/Files`, `Infrastructure/Storage`.
- Angular: `features/test-templates`, `shared/patterns/upload-queue`, `shared/patterns/protected-media`.
- Tests: template wizard, PDF/audio upload, AnswerKey versioning, file metadata/content contracts.

**FR-8 to FR-10 Homework And Live Exam Usage Modes**
- API: `Domain/Assignments`, `Domain/LiveExams`, `Application/HomeworkAssignments`, `Application/LiveExamSessions`, `Contracts/HomeworkAssignments`, `Contracts/LiveExamSessions`.
- Angular: `features/homework-assignments`, `features/live-exam-sessions`.
- Tests: publish, open, close, reopen, duplicate command protection, invalid state transitions.

**FR-11 to FR-14 Student Assigned Work And Attempt**
- API: `Domain/Submissions`, `Application/Submissions`, `Contracts/Submissions`.
- Angular: `features/assigned-tests`, `features/attempt-workspace`, `shared/layouts/attempt-shell`, `shared/patterns/autosave-status`.
- Tests: autosave, final submit, deadline/session close race, submission source mode preservation.

**FR-15 to FR-16 Speaking Submission And Manual Grading**
- API: `Domain/Submissions`, `Domain/Grading`, `Application/Speaking`, `Application/Results`, `Application/Files`, `Contracts/Speaking`.
- Angular: `features/speaking-submission`, `features/results-grading`, `shared/patterns/upload-queue`, `shared/patterns/protected-media`.
- Tests: upload draft/final, protected playback, grading save, missing file recovery.

**FR-17 to FR-19 Results, Grading, Dashboard**
- API: `Application/Results`, `Domain/Grading`, `Contracts/Results`.
- Angular: `features/results-grading`, `features/teacher-dashboard`, `shared/layouts/grading-shell`, `shared/layouts/teacher-shell`.
- Tests: filters, pagination, master-detail state, stable score history.

**FR-20 Visual And Interaction Reference**
- Angular: `shared/layouts`, `shared/ui`, `shared/feedback`, route-level feature layouts.
- Docs: `docs/design-system/*`.
- Rule: WDS/DD-001 controls behavior; Stitch controls visual/layout reference only.

### Integration Points

**Internal Communication:**
- Angular calls `/api/...` through the same-origin API client.
- API authenticates by Identity cookie and validates XSRF on unsafe methods.
- Application services use EF Core via Infrastructure and `IFileStorage` via abstraction.
- Controllers and Angular components never access physical storage paths.

**External Integrations:**
- SQL Server.
- Local protected file storage for MVP/dev.
- IIS/Windows Server hosting.
- ASP.NET Core Data Protection key persistence.
- Future cloud storage, antivirus/quarantine and scheduled cleanup remain behind storage/job abstractions.

**Data Flow:**
- Teacher uploads PDF/audio -> file metadata + protected storage -> template ready -> homework/live session.
- Student opens assigned work -> server checks class/mode/state -> file stream + answer DTO -> autosave/final submit.
- Final submit -> lock submission -> grade with pinned AnswerKey version -> results/dashboard.
- Speaking upload -> draft file -> final submit -> teacher grading -> score/audit update.

### File Organization Patterns

**Configuration Files:**
- API config in `appsettings*.json`; secrets via environment/IIS/secret store.
- Angular config in `environment*.ts` and `proxy.conf.json`.
- Deployment config under `deploy/iis`.
- Runtime storage and Data Protection setup documented under `docs/deploy`.

**Source Organization:**
- API code is organized by architecture boundary and feature/use-case.
- Angular code is organized by route-level feature plus shared/core layers.
- No code should be organized by Stitch page name alone.
- `Api/wwwroot/app` is generated deployment output for Angular and must not be edited manually.
- Generated files must be marked as generated and not manually changed except through their source generation flow.

**Feature Folder Pattern:**
- Angular route-level feature folders may contain `pages/`, `components/`, `dialogs/`, `models/`, `services/`, `guards/` and `state/` when needed.
- Each major feature has `feature.contract.md` documenting routes, permissions, server queries, mutation actions, dirty-state sources, lock/deadline dependencies, primary status region, allowed shared components and test expectations.

**Test Organization:**
- API unit/integration/security/API-contract tests live under `tests/EnglishTestWeb.Api.Tests`.
- Architecture tests verify dependency direction and convention registration.
- API contract tests verify route shape, status codes, `ProblemDetails`, pagination, hidden-resource behavior, DTO serialization and protected file headers/range/content type.
- TestKit builders/fakes/auth/clock/database helpers are the only approved shared test fixture layer.
- Angular `.spec.ts` tests are co-located with components/services.
- E2E tests live under `tests/EnglishTestWeb.E2E/flows` and are organized by product flow, not individual page.

**Asset Organization:**
- Public Angular assets live in client `public/`.
- Protected user uploads live outside the repo and outside runtime `wwwroot`.
- API `wwwroot/app` is for built Angular production assets only.

### Development Workflow Integration

**Development Server Structure:**
- Run API and Angular separately in development.
- Angular proxy routes `/api` to ASP.NET Core.
- Local protected storage root is configured outside the repository.

**Build Process Structure:**
- `dotnet build` builds the API.
- Angular CLI builds the SPA.
- Production build copies Angular output into API static app hosting area.
- CI gates should run architecture/unit tests first, API contract/security tests next, E2E smoke tests last.

**Deployment Structure:**
- IIS hosts the ASP.NET Core app.
- Same origin serves Angular static files and `/api`.
- Protected files remain outside `wwwroot`.
- Data Protection keys and upload roots are environment-specific and excluded from source control.
- `deploy/iis/scripts/verify.ps1` verifies IIS binding, static client files, API health, protected file directory ACLs and log directory writability.

**Agent Conventions:**
- `AGENTS.md` and `docs/agent-conventions.md` summarize the rules implementation agents must follow.
- Core reminders: DTO is not Entity, Controller calls Application only, feature services do not use raw `HttpClient`, `ProblemDetails` is required for API errors, idempotency is required where specified, protected files stay outside `wwwroot`.

## Architecture Validation Results

### Coherence Validation

**Decision Compatibility:**
Architecture is coherent. `.NET 10 + Angular 22 + SQL Server + ASP.NET Core Identity + IIS same-origin deployment` work together without architectural conflict. Same-origin deployment supports cookie auth, CSRF/XSRF, protected file streaming and reduced CORS exposure.

**Pattern Consistency:**
Implementation patterns support all core decisions: DTO/entity separation, REST `/api`, `ProblemDetails`, idempotency, server-side authorization, UTC timestamps, string states, protected file access, AnswerKey versioning and Angular feature contracts are aligned.

**Structure Alignment:**
The project structure supports the architecture: API boundaries are split into `Contracts`, `Controllers`, `Domain`, `Application`, `Infrastructure`; Angular is split into `core`, `shared`, `features`; tests are organized around architecture, API contract, security, fixtures and E2E flows.

### Requirements Coverage Validation

**Feature Coverage:**
All 7 PRD feature categories are architecturally supported:

- Accounts/Roles/Classes: Identity, class membership, resource authorization.
- Template Library: TestTemplate, TestMaterial, AnswerKey versioning, protected file storage.
- Homework/Live Exam: separate domain modules and state transitions.
- Student Attempt: assigned list, attempt workspace, autosave, final submit, source mode integrity.
- Speaking: upload draft/final, protected playback, manual grading.
- Results/Dashboard: filtering, master-detail grading, stable score snapshots.
- Visual Reference: WDS/DD-001 behavior preserved; Stitch is visual/layout reference only.

**Functional Requirements Coverage:**
FR-1 through FR-20 are covered by decisions, patterns and project structure mapping.

**Non-Functional Requirements Coverage:**
- NFR-1 performance is addressed by route-level structure, pagination, deterministic list patterns and conservative caching rules.
- NFR-2 autosave feedback is addressed through autosave state, idempotency, concurrency and UI status patterns.
- NFR-3 accessibility is addressed through shared layout/status/feedback, keyboard navigation expectations and design-system docs.
- NFR-4 security/scope is addressed through server-side resource policies, cookie auth, CSRF, 404 hidden-resource policy and protected files.
- NFR-5 data integrity is addressed through transaction boundaries, rowversion, idempotency and DB constraints.
- NFR-6 file safety is addressed through protected storage, metadata, range auth and no public upload URLs.
- NFR-7 auditability is addressed through audit event patterns and correlation IDs.
- NFR-8 responsive baseline is addressed through route feature contracts, shells and layout rules.

### Implementation Readiness Validation

**Decision Completeness:**
Critical architectural decisions are documented with current stack versions, deployment mode, auth method, data integrity strategy, protected storage, AnswerKey versioning and frontend state approach.

**Structure Completeness:**
The project tree is complete enough for initial scaffolding and implementation. It defines API/client/tests/deploy/docs boundaries and maps every FR group to directories.

**Pattern Completeness:**
Potential conflict points are covered: naming, DTOs, API routes, response formats, validation, pagination, state serialization, auth, protected files, idempotency, Angular state, UX workflow patterns and test fixtures.

### Gap Analysis Results

**Critical Gaps:**
None.

**Important Non-Blocking Gaps:**
- Local Node must be upgraded before Angular 22 scaffolding.
- File format and max upload size policy must be finalized during implementation stories.
- Speaking score range and validation scale must be finalized in the grading story.
- Student score visibility after Reading/Listening submission must be a product/story decision.
- Class/student account creation flow must be clarified: manual creation, admin-created, or import.

**Story-Level Policy Decisions:**
- AnswerKey edit behavior is no longer open at architecture level: architecture chooses versioning/snapshot preservation.
- Live Exam automatic schedule opening remains deferred; MVP uses manual open/close.
- Speaking browser recording remains deferred; MVP uses upload-first.
- Homework reopen is architecturally supported as an explicit audited transition, but exact teacher permission, student visibility and deadline-extension copy must be finalized in the Homework story.

**Deferred / Post-MVP Gaps:**
- Browser-based Speaking recording remains deferred.
- Automatic schedule-based Live Exam open/close remains deferred unless re-scoped.
- Cloud storage, CDN/media acceleration and antivirus/quarantine pipeline remain behind abstractions.

### Validation Issues Addressed

- Submission mode integrity is addressed by DB constraint, DTO/API rules and tests.
- AnswerKey historical stability is addressed by versioning/snapshot rules.
- Protected media access is addressed by storage boundary and authorized streaming endpoints.
- Same-origin deployment resolves SPA cookie/CORS complexity for MVP.
- WDS/DD-001 vs Stitch hierarchy is preserved.
- Deadline/live exam state changes during autosave or submit must return deterministic `409` behavior.
- Missing file metadata and missing physical file are separate recoverable errors.
- Student-facing DTOs must be contract-tested to exclude answer keys/correct answers.
- Hidden out-of-scope resources return `404`; visible-but-disallowed actions return `403`.

### Critical Failure Prevention Notes

- First implementation stories must not skip security scaffolding. Identity, class membership, resource authorization, CSRF and protected file storage must exist before student/teacher workflow screens are treated as complete.
- Do not implement Angular screens against mock/public file URLs; protected file metadata/content endpoints must be part of the first file/template slice.
- Do not implement submissions without the exactly-one-source constraint and AnswerKey version/snapshot fields.
- Upgrade Node before Angular 22 scaffolding.
- Verify Angular CLI command flags against official CLI docs during scaffold; current Angular 22 docs support `--test-runner vitest`.
- Add `global.json` for .NET SDK pinning before implementation begins.

### Architecture Completeness Checklist

**Requirements Analysis**

- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**Architectural Decisions**

- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**Implementation Patterns**

- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**

- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

Architecture is ready for scaffolding and story implementation. Several product policy details remain intentionally story-level decisions and are listed above; none block architectural handoff.

**Confidence Level:** High

Confidence is high for architecture readiness because the critical invariants, security model, storage model, boundaries and test gates are explicit. Confidence is not absolute because product policy details still require story-level acceptance decisions.

**Key Strengths:**
- Clear source hierarchy: PRD/DD-001/WDS for behavior, Stitch for visual reference.
- Strong security model for same-origin SPA, Identity cookies, CSRF and protected files.
- Explicit state/data integrity rules for Submission, Homework, Live Exam and AnswerKey.
- Implementation patterns are detailed enough to prevent agent drift.
- Project structure maps all FR groups to concrete backend/frontend/test locations.

**Areas for Future Enhancement:**
- Cloud storage implementation.
- Automatic Live Exam scheduling.
- Browser Speaking recorder.
- Advanced analytics/export.
- Antivirus/quarantine pipeline.

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented.
- Use implementation patterns consistently across all components.
- Respect project structure and boundaries.
- Refer to this document for all architectural questions.
- Do not let Stitch visual references override DD-001/WDS behavior.

**First Implementation Priority:**
Create the solution and starter projects:

```bash
dotnet new sln -n EnglishTestWeb
dotnet new webapi -n EnglishTestWeb.Api -o src/EnglishTestWeb.Api -f net10.0 --use-controllers
dotnet sln EnglishTestWeb.sln add src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj
npx @angular/cli@22 new english-test-web-client --directory src/EnglishTestWeb.Client --routing --style css --standalone --strict --test-runner vitest --package-manager npm --skip-git
```
