---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/addendum.md"
  - "_bmad-output/planning-artifacts/architecture.md"
  - "_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml"
  - "_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml"
  - "_bmad-output/C-UX-Scenarios/"
  - "docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md"
project: "EnglishTestWeb"
status: "ready-for-development"
created: "2026-06-09"
updated: "2026-06-09"
sourceHierarchy:
  primary: "PRD"
  behaviorDomain: "DD-001 and WDS page specs"
  technicalConstraints: "Architecture"
  visualReference: "Stitch mapping only"
---

# EnglishTestWeb - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for EnglishTestWeb, decomposing the requirements from the PRD, DD-001/WDS behavior specs, Architecture constraints, and Stitch visual mapping into implementable stories.

Source rules used for this breakdown:

- PRD is the main product requirements source.
- DD-001 and WDS page specs are the source of truth for behavior, validation, access control, object/page semantics, and domain model.
- Architecture is the source of truth for technical constraints and implementation boundaries.
- Stitch mapping is a visual/layout reference only and does not override domain semantics.

## Requirements Inventory

### Functional Requirements

FR-1: Teacher can log in and access Teacher Dashboard, Thư viện đề, lớp, and kết quả surfaces with teacher-only route protection and teacher-scoped data.

FR-2: Student can enter a class code before student login, see a clear class confirmation, and preserve selected Class context through login.

FR-3: Student can access work only when a ClassMembership exists for the selected Class; direct access to another class, assignment, session, or submission is rejected.

FR-4: Teacher can create, save draft, edit, list, search/filter, and inspect reusable Đề gốc in Thư viện đề.

FR-5: Teacher can attach required PDF and optional audio/cue materials to Đề gốc with progress, retry, replace, and secure file handling.

FR-6: Teacher can configure question count, correct answers, scoring mode, and score rules for Reading/Listening AnswerKey.

FR-7: Teacher can mark Đề gốc Ready only when required TestMaterial and validation rules pass; Ready templates expose Giao homework and Tạo thi trực tiếp actions.

FR-8: Teacher can create HomeworkAssignment from a Ready Đề gốc for a Class with due date and optional time limit.

FR-9: Teacher can create and control LiveExamSession from a Ready Đề gốc for a Class, including manual open/close for MVP.

FR-10: System must show and persist whether work is Homework or Thi trực tiếp in Student lists, exam workspace, submissions, results, and grading.

FR-11: Student can view available Homework and Thi trực tiếp items for the active Class, grouped or filtered by mode and status.

FR-12: Student can use a Reading/Listening workspace with PDF viewer, optional audio player, separate answer form, progress, class/template/mode context, and submit action.

FR-13: System saves Reading/Listening draft answers where technically feasible, shows autosave acknowledgement within 1 second online, and restores saved/local answers after reload where feasible.

FR-14: Student can final-submit Reading/Listening; the system locks the attempt, prevents duplicate submission, auto-grades against AnswerKey, and stores auto_score.

FR-15: Student can upload a valid Speaking file, see draft upload status, replace before final submission, and confirm final submission.

FR-16: Teacher can open SpeakingSubmission, play the file, enter score and feedback, validate score, save grading, and recover from missing-file errors without losing grading context.

FR-17: Teacher can filter results by Class, Đề gốc, Mode, Student, skill, and status while remaining scoped to their own data.

FR-18: Teacher can use a master-detail grading workspace where result list context remains visible while detail/grading panel is active.

FR-19: Teacher Dashboard shows scan-level metrics and recent work, routing primary work to modules rather than hiding workflows in dashboard cards.

FR-20: Implementation can borrow Stitch layout, spacing, badges, sidebar, tables, wizard, and split-panel patterns while preserving DD-001 domain semantics and correct wording.

### NonFunctional Requirements

NFR-1: Dashboard, library, assigned work, and results list load initial content in under 2 seconds on normal broadband.

NFR-2: Autosave acknowledgement appears within 1 second when online.

NFR-3: Core flows are keyboard accessible; labels are visible/programmatic; focus order follows visual order; color contrast meets WCAG AA.

NFR-4: Role-based and resource-scoped server-side access prevents Teacher/Student viewing data outside their scope.

NFR-5: Submission, mark-ready, homework creation, live-session creation, and grading save are protected against duplicate actions.

NFR-6: PDF/audio/Speaking storage requires secure access controls, upload progress, retry/replace behavior, and recoverable errors.

NFR-7: Key state transitions are traceable enough for teacher support: template ready, assignment/session created, session opened/closed, submission finalized, grading saved.

NFR-8: MVP is desktop/laptop-first, but pages degrade safely on tablet/mobile web without content overlap or blocked critical actions.

### Additional Requirements

- Architecture selects a custom two-project starter: ASP.NET Core Web API on .NET 10 and Angular 22 SPA.
- Story 1.1 must set up the baseline .NET 10 Web API + Angular 22 + SQL Server + ASP.NET Core Identity + protected storage foundation.
- Add `global.json` for .NET SDK pinning and document Node upgrade/engine expectations before Angular 22 scaffolding.
- Use ASP.NET Core Identity with SQL Server stores and same-origin cookie authentication for the browser app.
- Angular must not store access tokens in `localStorage` or `sessionStorage`.
- Unsafe state-changing API requests must use antiforgery/XSRF protection.
- Production deployment should be same-origin through ASP.NET Core/IIS to reduce CORS, cookie, and protected-media complexity.
- EF Core code-first migrations use SQL Server; Identity and EnglishTestWeb domain schema share one database for MVP.
- Controllers expose REST endpoints and delegate to Application services; controllers do not access `DbContext`, filesystem APIs, or domain mutation logic directly.
- Use `ProblemDetails` with stable business error codes; use `409 Conflict` for concurrency/state conflicts, `403` for visible-but-disallowed actions, and `404` to hide out-of-scope resources where appropriate.
- Server-side resource authorization is mandatory for every read/write involving classes, templates, assignments, live sessions, submissions, grading, and files.
- Uploaded files must stay outside public `wwwroot`; file access goes through authorized API endpoints and `IFileStorage`.
- File metadata should track generated storage key, original filename, content type, size, checksum, owner/scope, status, creator, and timestamps.
- Protected PDF/audio/Speaking streams should support range requests where needed for PDF/audio playback.
- `Submission` must reference exactly one source mode: one HomeworkAssignment or one LiveExamSession, never both and never neither.
- AnswerKey edits after submissions create a new version or scoring snapshot; historical submissions never rebind silently.
- Autosave, final submit, mark-ready, homework/live session creation, open/close, and grading save must be idempotent/concurrency-safe.
- Audit events should capture actor, timestamp, resource id, previous state, next state, and reason where relevant.
- API and Angular code should follow architecture boundaries: `Contracts`, `Controllers`, `Domain`, `Application`, `Infrastructure`; Angular `core`, `shared`, and route-level `features`.
- Test coverage should include API contract/security tests, authorization matrix, protected file access, autosave/final submit, AnswerKey versioning, and Playwright MVP flow tests.

### UX Design Requirements

UX-DR1: Teacher login uses a quiet task-focused page with brand bar, role/context panel, username/password form, show/hide password, loading/error states, keyboard navigation, and auth-guard return-route handling.

UX-DR2: Teacher app shell exposes persistent navigation for Dashboard, Thư viện đề, Lớp học, and Kết quả, with dashboard acting as a scan surface rather than the main workflow launcher.

UX-DR3: Student class-code entry is a single-purpose public page with code normalization, lookup loading state, invalid/expired retryable errors, and a class confirmation card before login.

UX-DR4: Student login always displays the selected class context, allows changing class, verifies membership after authentication, and routes directly to Assigned Tests.

UX-DR5: Thư viện đề provides searchable/filterable template list/table, status badges for Draft/Ready/Archived, template inspection, and row actions for edit, duplicate/archive, Giao homework, and Tạo thi trực tiếp.

UX-DR6: Create Template setup is a four-step wizard with a stepper, template name, skill segmented control, optional description/tags, draft summary, save-draft state, and no class/deadline/session fields.

UX-DR7: Materials upload uses a PDF/audio/cue upload zone with progress, file card, retry/replace, preview link, required-material checklist, and no PDF question parsing in MVP.

UX-DR8: AnswerKey setup uses question count controls, scoring mode switch, answer grid, per-row validation, validation summary, autosave, and warnings before review.

UX-DR9: Review Template is a checklist-style review page with basic info/materials/answer key cards, readiness panel, mark-ready confirmation, success state, and next actions for Homework or Live Exam.

UX-DR10: Homework and Live Exam creation/control surfaces must show the Ready template source, class selection, due date or open/close state, optional time limit/schedule metadata, and mode-specific labels.

UX-DR11: Assigned Tests shows active class context, Homework and Thi trực tiếp tabs, status filters, skill filters, cards/list rows, empty state tied to class, and statuses including not open, open now, submitted, needs grading, and graded.

UX-DR12: Reading/Listening attempt workspace is a stable split layout with exam header, mode badge, class context, optional timer, autosave status, PDF viewer, page controls, optional audio player, answer panel, missing-answer jump, confirmation modal, and submitted success state.

UX-DR13: Speaking submission keeps prompt/cue card and upload panel visible together, distinguishes uploaded draft from final submitted file, supports replace before final submit, and confirms filename/timestamp after submit.

UX-DR14: Results & Grading is a master-detail workspace with filters by class/type/template/student/skill/status, result rows with mode context, detail panel, Speaking player, score input, feedback, save state, and next-pending action.

UX-DR15: Visual implementation should follow Stitch/Proctor & Pedagogy as visual reference only: calm operational UI, Inter typography, green primary actions, amber pending, blue live-session states, consistent tables, wizard, split panels, WCAG AA, visible focus, and responsive-safe layout.

### FR Coverage Map

FR-1: Epic 1 - secure teacher authentication, teacher shell access, and teacher-scoped route/API protection; Epic 6 adds dashboard metrics.

FR-2: Epic 1 - student class-code lookup, confirmation, and class context preservation through login.

FR-3: Epic 1 - ClassMembership enforcement and server-side direct-route/resource rejection.

FR-4: Epic 2 - reusable Đề gốc library, list/search/filter, create/edit/save draft, and inspect.

FR-5: Epic 2 - protected PDF/audio/cue material upload with progress, retry/replace, and preview.

FR-6: Epic 2 - AnswerKey question count, scoring mode, row validation, and version-ready storage.

FR-7: Epic 2 - mark-ready validation, idempotent Ready transition, and next actions.

FR-8: Epic 3 - HomeworkAssignment creation from Ready template for a Class with due date/time limit.

FR-9: Epic 3 - LiveExamSession creation and manual open/close control from Ready template.

FR-10: Epic 3 and Epic 4 - usage-mode context in assignment/session contracts, attempts, submissions, results, and grading.

FR-11: Epic 4 - student Assigned Tests list with active Class, Homework/Live Exam grouping, status, and guards.

FR-12: Epic 4 - Reading/Listening workspace with PDF, optional audio, answer form, progress, and submit action.

FR-13: Epic 4 - draft answer persistence, autosave acknowledgement, reload restore, and degraded/offline status.

FR-14: Epic 4 - final submit lock, duplicate prevention, AnswerKey-version auto-grading, and auto_score storage.

FR-15: Epic 5 - Speaking upload draft, replace, validation, final submit, filename/timestamp confirmation.

FR-16: Epic 5 and Epic 6 - Speaking playback, score/feedback validation, save, row status update, and missing-file recovery.

FR-17: Epic 6 - teacher Results filtering by Class, Đề gốc, Mode, Student, skill, and status with scope protection.

FR-18: Epic 6 - master-detail Results & Grading workspace with keyboard/focus behavior.

FR-19: Epic 6 - dashboard scan metrics, recent work, navigation to modules, and no ambiguous dashboard-only workflow.

FR-20: Epic 6 - Stitch-informed visual/layout implementation while preserving PRD/DD/WDS domain semantics and wording.

## Epic List

### Epic 1: Secure Workspace Foundation And Class Access

Teachers and students can enter the correct secure workspace, with the selected stack, Identity, SQL Server, protected storage, role-based navigation, class-code entry, and server-side scope enforcement in place.

**FRs covered:** FR-1, FR-2, FR-3.

### Epic 2: Reusable Đề Gốc Library And Template Creation

Teachers can create, validate, and manage reusable Đề gốc from existing PDF/audio/cue materials and AnswerKey, then mark a template Ready for later use.

**FRs covered:** FR-4, FR-5, FR-6, FR-7.

### Epic 3: Homework And Live Exam Delivery From Ready Templates

Teachers can use a Ready Đề gốc as either Homework with a due date or a manually controlled Live Exam session while the system preserves mode semantics.

**FRs covered:** FR-8, FR-9, FR-10.

### Epic 4: Student Assigned Work And Reading/Listening Attempts

Students can see the right work in the active Class, complete Reading/Listening with PDF/audio plus answer form, autosave drafts, and final-submit for auto-grading.

**FRs covered:** FR-10, FR-11, FR-12, FR-13, FR-14.

### Epic 5: Speaking Submission And Manual Teacher Grading

Students can submit Speaking files safely, and teachers can play, grade, and save feedback for Speaking submissions.

**FRs covered:** FR-15, FR-16.

### Epic 6: Results, Dashboard, Visual Consistency, And MVP Validation

Teachers can review submissions across classes/templates/modes, grade in a master-detail workspace, scan dashboard metrics, and validate the MVP against WDS/DD/TS expectations.

**FRs covered:** FR-16, FR-17, FR-18, FR-19, FR-20.

## Epic 1: Secure Workspace Foundation And Class Access

Teachers and students can enter the correct secure workspace, with the selected stack, Identity, SQL Server, protected storage, role-based navigation, class-code entry, and server-side scope enforcement in place.

### Story 1.1: Setup Baseline .NET 10 Web API + Angular 22 + SQL Server + Identity + Protected Storage

As a development team,
I want the EnglishTestWeb baseline solution scaffolded with the selected backend, frontend, database, Identity, and protected file-storage foundation,
So that every later story builds on the approved architecture instead of temporary mock infrastructure.

**Coverage:** FR-1 foundation, NFR-4, NFR-6, Architecture starter requirement.

**Acceptance Criteria:**

1. **Given** a clean repository or current workspace
   **When** the baseline scaffold is created
   **Then** the solution contains `src/EnglishTestWeb.Api` targeting `net10.0`, `src/EnglishTestWeb.Client` using Angular 22 standalone strict mode, and `EnglishTestWeb.sln`
   **And** `global.json`, frontend engine notes, and setup documentation identify the required .NET 10 SDK and Node version expectations.

2. **Given** SQL Server connection settings are provided
   **When** EF Core migrations are applied
   **Then** ASP.NET Core Identity schema is created in SQL Server
   **And** Teacher and Student roles can be seeded without creating unrelated domain tables early.

3. **Given** the Angular app calls authenticated API endpoints
   **When** login succeeds
   **Then** the browser uses same-origin cookie authentication with HttpOnly/Secure cookie settings appropriate for environment
   **And** Angular does not store access tokens in `localStorage` or `sessionStorage`.

4. **Given** an unsafe API request is sent without valid antiforgery/XSRF protection
   **When** the API receives the request
   **Then** the request is rejected with a stable `ProblemDetails` error
   **And** Angular is configured to send the expected XSRF header for unsafe methods.

5. **Given** protected storage is configured
   **When** a file is written through `IFileStorage`
   **Then** the file is stored outside public `wwwroot`
   **And** access is possible only through an authorized API/service path, not a public static URL.

6. **Given** the baseline is complete
   **When** `dotnet build` and the Angular install/build/test smoke commands run in the documented environment
   **Then** they complete successfully
   **And** failures document the missing SDK/Node/database prerequisite rather than hiding it.

7. **Given** the baseline scaffold is committed
   **When** the minimal CI or local quality script runs
   **Then** it executes API build/test smoke and Angular install/build/test smoke
   **And** the command is documented for sprint agents before feature stories begin.

### Story 1.2: Teacher Login And Teacher App Shell

As a teacher,
I want to log in and reach a predictable teacher shell,
So that I can start from Dashboard and navigate to Thư viện đề, Lớp học, and Kết quả without role confusion.

**Coverage:** FR-1, UX-DR1, UX-DR2, NFR-3, NFR-4.

**Acceptance Criteria:**

1. **Given** an unauthenticated teacher visits `/login`
   **When** the login page loads
   **Then** it shows EnglishTestWeb branding, teacher context copy, username/email input, password input with show/hide, remember option if supported, forgot-password link, and visible labels.

2. **Given** the teacher submits missing or invalid credentials
   **When** validation or authentication fails
   **Then** inline errors use stable error codes and Vietnamese copy
   **And** the response does not reveal whether the email exists.

3. **Given** valid teacher credentials
   **When** the teacher signs in
   **Then** the app routes to `/teacher/dashboard` or the originally requested teacher route
   **And** Teacher Dashboard, Thư viện đề, Lớp học, and Kết quả navigation items are visible.

4. **Given** a Student account tries to access a teacher route
   **When** the route and API authorization checks run
   **Then** access is denied server-side
   **And** the Angular route guard shows an appropriate blocked or login state without exposing teacher data.

5. **Given** the teacher shell is operated by keyboard
   **When** the teacher tabs through nav and login controls
   **Then** focus is visible and follows visual order.

### Story 1.3: Class Roster, Class Code Lookup, And Student Login

As a student,
I want to enter a class code, confirm the class, and log in with that context,
So that I land in the correct class workspace before seeing assigned work.

**Coverage:** FR-2, FR-3, UX-DR3, UX-DR4, NFR-4.

**MVP Provisioning Decision:** Until a full class/student management or import flow is explicitly scoped, the first build uses idempotent seed/admin provisioning for Teacher, Student, Class, and ClassMembership test/demo data. This keeps FR-1 to FR-3 testable without adding an unplanned LMS/admin module.

**Acceptance Criteria:**

1. **Given** the MVP environment needs testable access before full class-management UX exists
   **When** the seed/admin provisioning command runs
   **Then** it creates or verifies one Teacher, one Student, one active Class, and one active ClassMembership
   **And** the operation is idempotent for local/dev and test environments.

2. **Given** a teacher has a seeded or admin-created active Class with a class code and student memberships
   **When** the teacher opens the Lớp học surface
   **Then** the teacher can see class name, class code, active status, and enrolled students within their own scope.

3. **Given** a student opens `/class`
   **When** they enter a class code with spaces, dashes, or lowercase characters
   **Then** the system normalizes the code for lookup
   **And** preserves safe input handling.

4. **Given** the class code is valid and active
   **When** lookup succeeds
   **Then** a confirmation card shows class name and teacher context
   **And** the student must confirm before navigating to `/student/login`.

5. **Given** the code is invalid or expired
   **When** lookup fails
   **Then** the student sees a retryable Vietnamese error
   **And** no class roster or assigned tests are exposed.

6. **Given** a selected class context and valid student credentials
   **When** the student logs in
   **Then** the API verifies ClassMembership server-side
   **And** the student routes directly to Assigned Tests for that active Class.

7. **Given** the student account is not a member of the selected Class
   **When** login completes credential validation
   **Then** access to that class is blocked with a clear next step
   **And** no assignments, sessions, submissions, or roster details are returned.

### Story 1.4: Base Authorization Pattern And Class Scope Guards

As the system owner,
I want a reusable server-side authorization pattern and class/member scope checks,
So that current resources are protected now and future resources can add their own scope policies when they are created.

**Coverage:** FR-1, FR-3, NFR-4, Architecture authorization requirements.

**Acceptance Criteria:**

1. **Given** Identity, roles, Classes, and ClassMembership exist
   **When** the authorization framework is implemented
   **Then** it provides current-user access, role checks, resource-scope policy handlers, hidden-resource response helpers, and test fixtures for Teacher, Student, and unauthenticated users.

2. **Given** a teacher requests a Class outside their ownership
   **When** the class-scope policy evaluates the request
   **Then** the API returns `403` or hidden `404` according to the architecture rule
   **And** no class or roster data is serialized.

3. **Given** a student requests a Class outside their active ClassMembership
   **When** the class-membership policy evaluates the request
   **Then** the API rejects the request server-side
   **And** direct Angular route access cannot bypass the decision.

4. **Given** a protected class or roster request is denied
   **When** the denial is logged
   **Then** no sensitive identifiers beyond allowed audit metadata are exposed to the caller
   **And** audit captures actor, resource id when safe, and reason category.

**Implementation Note:** Stories that introduce TestTemplate, TestMaterial, HomeworkAssignment, LiveExamSession, Submission, SpeakingSubmission, or grading resources must add resource-specific authorization policies in the same story that introduces the resource.

5. **Given** authorization tests run
   **When** Teacher, Student, and unauthenticated cases are executed for current Class and membership resources
   **Then** the authorization matrix covers allowed, forbidden, and hidden-resource cases.

## Epic 2: Reusable Đề Gốc Library And Template Creation

Teachers can create, validate, and manage reusable Đề gốc from existing PDF/audio/cue materials and AnswerKey, then mark a template Ready for later use.

### Story 2.1: Thư Viện Đề List, Search, Filter, And Template Inspection

As a teacher,
I want a searchable and filterable Thư viện đề,
So that I can find existing reusable Đề gốc and choose the right action quickly.

**Coverage:** FR-4, FR-20, UX-DR5, NFR-1, NFR-3.

**Acceptance Criteria:**

1. **Given** a teacher has templates in Draft, Ready, and Archived states
   **When** they open `/teacher/library`
   **Then** the page shows title, skill, status, last-used metadata where available, and row actions.

2. **Given** the teacher searches or filters by skill/status
   **When** filters change
   **Then** the list updates within the expected performance budget
   **And** filter state is reflected in query params.

3. **Given** no templates match the current filters
   **When** the list is empty
   **Then** a calm empty state appears with clear options to clear filters or create a new Đề gốc.

4. **Given** a template is not Ready
   **When** the teacher opens row actions
   **Then** Giao homework and Tạo thi trực tiếp are disabled or blocked with `ERR_TEMPLATE_NOT_READY`.

5. **Given** the page is navigated by keyboard
   **When** focus moves through filters, rows, and action menus
   **Then** focus order and visible focus states are usable.

### Story 2.2: Create, Edit, And Save Draft Template Setup

As a teacher,
I want to create or edit the basic setup for a Đề gốc,
So that the reusable template has the correct name, skill, and notes before materials are uploaded.

**Coverage:** FR-4, UX-DR6, NFR-5.

**Acceptance Criteria:**

1. **Given** a teacher starts a new template
   **When** `/teacher/library/new/setup` loads
   **Then** the wizard shows Step 1 of 4 with name, skill segmented control, optional description, optional tags, draft summary, and footer actions.

2. **Given** the teacher submits an empty or too-short template name
   **When** validation runs
   **Then** the field shows `ERR_TEMPLATE_NAME_REQUIRED` or equivalent stable error
   **And** the template is not advanced to the next step.

3. **Given** the teacher selects Reading, Listening, or Speaking
   **When** skill changes
   **Then** required material and AnswerKey expectations update without asking for Class, deadline, time limit, or session timing.

4. **Given** valid setup data
   **When** the teacher saves draft or continues
   **Then** a draft TestTemplate is created or updated in the teacher scope
   **And** duplicate clicks do not create duplicate draft templates.

5. **Given** the teacher resumes an existing draft
   **When** setup loads
   **Then** prior setup values are restored and editable.

### Story 2.3: Protected TestMaterial Upload And Preview

As a teacher,
I want to upload required PDF and optional audio/cue materials with progress and retry,
So that the Đề gốc has secure source materials without re-entering the PDF content.

**Coverage:** FR-5, UX-DR7, NFR-6, FR-20.

**Acceptance Criteria:**

1. **Given** a Reading template draft exists
   **When** the teacher opens the materials step
   **Then** the page shows Step 2 of 4, PDF dropzone/file picker, requirement checklist, file card area, upload status, and footer actions.

2. **Given** the teacher selects a non-PDF for a Reading PDF requirement
   **When** client and server validation run
   **Then** the upload is rejected with `ERR_FILE_TYPE`
   **And** the draft remains editable.

3. **Given** the teacher uploads a valid file
   **When** upload is in progress
   **Then** progress is visible
   **And** the Continue action remains disabled until upload completes.

4. **Given** upload succeeds
   **When** the file card displays
   **Then** it shows original filename, size, success status, preview action, remove/replace action, and protected file id metadata
   **And** the physical file is outside `wwwroot`.

5. **Given** upload fails or a file is replaced
   **When** the teacher retries or replaces
   **Then** the draft state is preserved
   **And** storage metadata/audit records reflect the current active material.

6. **Given** the teacher previews a PDF or audio material
   **When** the request is made
   **Then** it streams through an authorized endpoint with range support where applicable.

### Story 2.4: AnswerKey And Scoring Configuration

As a teacher,
I want to configure question count, correct answers, and scoring,
So that Reading/Listening submissions can be auto-graded from a stable AnswerKey.

**Coverage:** FR-6, UX-DR8, NFR-5.

**Acceptance Criteria:**

1. **Given** a draft template has required materials
   **When** the teacher opens Answer key & Scoring
   **Then** the page shows Step 3 of 4, question count, scoring mode, total score or per-question score inputs, answer grid, validation summary, and save-draft action.

2. **Given** the teacher enters an invalid question count
   **When** validation runs
   **Then** the system blocks continue with `ERR_QUESTION_COUNT_INVALID`.

3. **Given** the teacher configures rows
   **When** answers or scores change
   **Then** the validation summary updates missing answer count and score total
   **And** draft changes are autosaved or manually saved without losing typed rows.

4. **Given** any answer row is missing
   **When** the teacher tries to continue
   **Then** the system identifies the missing question number using `ERR_ANSWER_MISSING`.

5. **Given** valid AnswerKey and scoring data
   **When** the teacher continues to review
   **Then** AnswerKey rows are stored structurally, independent of PDF page content
   **And** the initial AnswerKey version or version-ready record is prepared for future submission history.

### Story 2.5: Review Template, Mark Ready, And Next Actions

As a teacher,
I want to review readiness and mark a Đề gốc ready,
So that I can confidently use the same source template for Homework or Live Exam.

**Coverage:** FR-7, FR-20, UX-DR9, NFR-5, NFR-7.

**Acceptance Criteria:**

1. **Given** setup, materials, and AnswerKey are complete
   **When** the teacher opens review
   **Then** the page shows Step 4 of 4, basic info, materials, AnswerKey/scoring cards, readiness checklist, warnings, and mark-ready action.

2. **Given** any required readiness check fails
   **When** the teacher clicks Mark ready
   **Then** the system focuses the first blocking issue
   **And** the template remains Draft.

3. **Given** all checks pass
   **When** the teacher confirms Mark ready
   **Then** the template status changes to Ready exactly once
   **And** double-clicks or retries return the same result instead of creating duplicate transitions.

4. **Given** a template is Ready
   **When** the success state appears
   **Then** it shows Giao homework and Tạo thi trực tiếp as separate next actions
   **And** no class/deadline/session timing is stored on the template itself.

5. **Given** the Ready transition succeeds
   **When** audit records are inspected
   **Then** actor, previous state, next state, template id, and timestamp are recorded.

## Epic 3: Homework And Live Exam Delivery From Ready Templates

Teachers can use a Ready Đề gốc as either Homework with a due date or a manually controlled Live Exam session while the system preserves mode semantics.

### Story 3.1: Create HomeworkAssignment From A Ready Template

As a teacher,
I want to assign a Ready Đề gốc as Homework to a Class with due date and optional time limit,
So that students can complete it at home within the allowed window.

**Coverage:** FR-8, FR-10, UX-DR10, NFR-5, NFR-7.

**Acceptance Criteria:**

1. **Given** a teacher selects Giao homework from a Ready template
   **When** the Homework creation surface opens
   **Then** it opens `/teacher/homework/new?templateId={templateId}` or a documented equivalent route
   **And** it shows the source template, skill, selected class, due date, optional time limit, and mode label Homework.

2. **Given** no separate WDS page spec exists for Homework creation
   **When** implementation starts
   **Then** the feature contract defines stable object ids for source-template summary, class select, due-date input, time-limit input, create action, cancel action, loading state, validation errors, and success state.

3. **Given** the teacher selects a class outside their scope
   **When** the create request is submitted
   **Then** the API rejects the request server-side
   **And** no HomeworkAssignment is created.

4. **Given** due date or time limit validation fails
   **When** the teacher submits
   **Then** inline errors explain the invalid field
   **And** the template remains Ready and unchanged.

5. **Given** valid Homework data
   **When** the teacher creates the assignment
   **Then** HomeworkAssignment references exactly one Ready TestTemplate and one Class
   **And** duplicate clicks or retries do not create duplicate assignments.

6. **Given** the HomeworkAssignment is created
   **When** it is viewed by Teacher or Student APIs
   **Then** the response preserves Homework mode, source template title, class, due/deadline state, and student availability state.

7. **Given** the create operation succeeds
   **When** audit is reviewed
   **Then** assignment id, template id, class id, actor, created state, and timestamp are recorded.

### Story 3.2: Create And Control LiveExamSession

As a teacher,
I want to create a Live Exam session from a Ready Đề gốc and manually open or close it,
So that students can only start in-class work when the session is allowed.

**Coverage:** FR-9, FR-10, UX-DR10, NFR-5, NFR-7.

**Acceptance Criteria:**

1. **Given** a teacher selects Tạo thi trực tiếp from a Ready template
   **When** the Live Exam creation surface opens
   **Then** it opens `/teacher/live-exams/new?templateId={templateId}` or a documented equivalent route
   **And** it shows source template, selected class, optional scheduled start/end display fields, and mode label Thi trực tiếp.

2. **Given** no separate WDS page spec exists for Live Exam creation/control
   **When** implementation starts
   **Then** the feature contract defines stable object ids for source-template summary, class select, schedule display fields, create action, open action, close action, status badge, validation errors, and success/conflict states.

3. **Given** valid Live Exam data
   **When** the teacher creates the session
   **Then** LiveExamSession references exactly one Ready TestTemplate and one Class
   **And** initial status is scheduled/not open unless the teacher explicitly opens it.

4. **Given** a LiveExamSession exists
   **When** the teacher opens the session
   **Then** status changes to Open exactly once
   **And** students in the class can start allowed attempts.

5. **Given** a LiveExamSession is Open
   **When** the teacher closes it
   **Then** status changes to Closed
   **And** new attempts are blocked while existing submitted work remains available for results.

6. **Given** duplicate open/close requests are sent
   **When** the API handles them
   **Then** the transition is idempotent or returns deterministic `409 Conflict`
   **And** audit captures previous and next state.

7. **Given** scheduled fields are present in MVP
   **When** the scheduled time arrives
   **Then** the system does not auto-open unless explicitly implemented later
   **And** the UI copy makes manual open/close behavior clear.

### Story 3.3: Usage Mode Contract Across Delivery Surfaces

As a teacher and student,
I want Homework and Thi trực tiếp context to remain visible and structurally distinct,
So that nobody confuses a reusable Đề gốc with a specific assigned or live-session instance.

**Coverage:** FR-10, FR-20, NFR-5.

**Acceptance Criteria:**

1. **Given** HomeworkAssignment and LiveExamSession APIs return list/detail DTOs
   **When** those DTOs are serialized
   **Then** they include mode, source template id/title, class id/name, instance id, status, and allowed actions.

2. **Given** a template is displayed in Thư viện đề
   **When** usage actions are shown
   **Then** labels are Giao homework and Tạo thi trực tiếp
   **And** the template itself is not labeled as an assigned bài thi.

3. **Given** a student-facing item is Homework
   **When** the item is displayed
   **Then** the label and status copy use Homework/Bài tập về nhà semantics.

4. **Given** a student-facing item is Live Exam
   **When** the item is displayed
   **Then** the label and status copy use Thi trực tiếp semantics, including not open/open now/closed where relevant.

**Implementation Note:** The exactly-one Submission source database/application constraint is implemented in Story 4.2, where Submission/Attempt records are first created.

## Epic 4: Student Assigned Work And Reading/Listening Attempts

Students can see the right work in the active Class, complete Reading/Listening with PDF/audio plus answer form, autosave drafts, and final-submit for auto-grading.

### Story 4.1: Student Assigned Tests List

As a student,
I want to see available Homework and Thi trực tiếp items for my active Class,
So that I can choose the correct work without asking the teacher.

**Coverage:** FR-10, FR-11, UX-DR11, NFR-1, NFR-3, NFR-4.

**Acceptance Criteria:**

1. **Given** a logged-in student has an active ClassMembership
   **When** they open `/student/tests`
   **Then** the page shows active class context, Homework and Thi trực tiếp tabs, status filters, skill filter, and assigned work cards/list rows.

2. **Given** Homework or Live Exam items are not available for the active Class
   **When** the list loads
   **Then** the empty state is tied to that active Class and not shown as a generic failure.

3. **Given** Homework is past deadline
   **When** the student views the item
   **Then** the status communicates closed/expired state
   **And** starting a new attempt is blocked.

4. **Given** LiveExamSession is not open
   **When** the student views the item
   **Then** the status communicates not-open state
   **And** starting is blocked with `ERR_LIVE_EXAM_NOT_OPEN`.

5. **Given** the student filters by mode/status/skill
   **When** filters change
   **Then** the list updates while preserving active Class context.

6. **Given** a student directly requests another class's assigned item
   **When** the API evaluates scope
   **Then** the request is rejected server-side.

### Story 4.2: Reading/Listening Attempt Workspace

As a student,
I want a stable workspace with PDF/audio and a separate answer form,
So that I can complete Reading or Listening without the system needing to parse the PDF into questions.

**Coverage:** FR-12, FR-10, UX-DR12, NFR-3, NFR-6.

**Acceptance Criteria:**

1. **Given** a student opens an available Reading or Listening Homework/Live Exam item
   **When** an attempt starts or resumes
   **Then** the Submission/Attempt references exactly one HomeworkAssignment or one LiveExamSession
   **And** database/application validation prevents both-null and both-set source modes.

2. **Given** the workspace loads
   **When** materials are available
   **Then** it shows exam title, skill, active class, mode badge, optional timer, autosave status region, PDF viewer, page controls, optional audio player, answer progress, answer rows, missing-answer jump, and submit button.

3. **Given** the test is Listening with audio
   **When** the student plays audio
   **Then** playback occurs in-page through authorized file streaming
   **And** the audio player is keyboard operable.

4. **Given** protected PDF/audio access fails or file is missing
   **When** the workspace loads or plays media
   **Then** a recoverable error is shown
   **And** no storage path is exposed.

5. **Given** the student navigates PDF pages
   **When** the page changes
   **Then** the answer panel remains stable and does not lose entered answers.

6. **Given** the workspace is rendered at desktop and tablet widths
   **When** viewport changes
   **Then** critical actions and text do not overlap or become unreachable.

### Story 4.3: Draft Answer Autosave And Restore

As a student,
I want my Reading/Listening answers saved while I work,
So that reloads or normal network interruptions do not wipe my progress.

**Coverage:** FR-13, UX-DR12, NFR-2, NFR-5.

**Acceptance Criteria:**

1. **Given** the student edits an answer row
   **When** input changes
   **Then** the answer is stored locally immediately
   **And** a server autosave is queued for that attempt.

2. **Given** the connection is online and normal
   **When** autosave succeeds
   **Then** the UI shows saved acknowledgement within 1 second.

3. **Given** autosave is pending or fails
   **When** the student continues working
   **Then** the UI shows saving/offline/degraded state without claiming final submission succeeded.

4. **Given** the student reloads an in-progress attempt
   **When** saved server or local draft exists
   **Then** answers are restored where technically feasible
   **And** the restored source/state is visible enough to avoid confusion.

5. **Given** a final submission already exists
   **When** a late autosave request arrives
   **Then** the API rejects or ignores it deterministically
   **And** submitted answers remain locked.

6. **Given** duplicate autosave requests arrive out of order
   **When** the API processes them
   **Then** rowversion/timestamp handling prevents stale data from overwriting newer answers.

### Story 4.4: Final Submission And Reading/Listening Auto-Grading

As a student,
I want to final-submit my Reading/Listening work and know it is locked,
So that the teacher receives a stable, auto-graded submission.

**Coverage:** FR-14, FR-10, NFR-5, NFR-7.

**Acceptance Criteria:**

1. **Given** an in-progress Reading/Listening attempt has missing answers
   **When** the student clicks Nộp bài
   **Then** a confirmation modal warns about missing answer count
   **And** the student can return to edit if the attempt is still open.

2. **Given** the student confirms final submission
   **When** the API accepts the submit command
   **Then** the Submission status becomes Submitted or Auto-graded
   **And** answers become read-only for the student.

3. **Given** the AnswerKey has a current version for the template
   **When** final submit completes
   **Then** SubmissionAnswer rows are graded against that version
   **And** auto_score and AnswerKeyVersionId or scoring snapshot are stored.

4. **Given** the student double-clicks submit or the request is retried
   **When** the API receives duplicate submit commands
   **Then** only one final Submission result exists
   **And** duplicate requests return the original result or deterministic conflict.

5. **Given** Homework deadline passed or LiveExamSession closed before submit
   **When** the student submits
   **Then** the API applies the configured blocking rule
   **And** the UI shows a clear recoverable message.

6. **Given** final submit succeeds
   **When** the success state appears
   **Then** it shows submitted timestamp, test title, mode, and route back to Assigned Tests.

## Epic 5: Speaking Submission And Manual Teacher Grading

Students can submit Speaking files safely, and teachers can play, grade, and save feedback for Speaking submissions.

### Story 5.1: Student Speaking Prompt And Upload Draft

As a student,
I want to view the Speaking prompt and upload a valid file as a draft,
So that I can verify the file before final submission.

**Coverage:** FR-15, UX-DR13, NFR-6.

**Acceptance Criteria:**

1. **Given** a student opens an available Speaking Homework or Live Exam
   **When** the Speaking submission page loads
   **Then** it shows title, skill, active class, mode/status badge, prompt/cue card or attachment, upload panel, and current draft/submitted status.

2. **Given** the student selects an unsupported file type or oversized file
   **When** validation runs
   **Then** the upload is rejected with stable file type/size errors
   **And** the student can choose another file.

3. **Given** the student uploads a valid file
   **When** upload is in progress
   **Then** progress is shown
   **And** final submit remains disabled until upload completes.

4. **Given** upload succeeds
   **When** the file card appears
   **Then** it shows filename, size, draft status, replace/remove actions, and protected file metadata
   **And** uploading alone does not mark the SpeakingSubmission as final submitted.

5. **Given** the student replaces a draft file
   **When** replacement succeeds
   **Then** the active draft points to the new file
   **And** the old draft file is handled according to storage retention rules without becoming public.

### Story 5.2: Final Speaking Submission Lock And Confirmation

As a student,
I want to final-submit my Speaking file with explicit confirmation,
So that I know the correct file was submitted for the correct assignment/session.

**Coverage:** FR-15, FR-10, UX-DR13, NFR-5, NFR-7.

**Acceptance Criteria:**

1. **Given** no valid uploaded draft file exists
   **When** the student clicks submit
   **Then** final submission is blocked with `ERR_SPEAKING_FILE_REQUIRED`.

2. **Given** a valid draft file exists
   **When** the student clicks Nộp bài Speaking
   **Then** a confirmation modal shows filename, test title, class, and mode.

3. **Given** the student confirms
   **When** the API accepts final submit
   **Then** SpeakingSubmission becomes Submitted
   **And** the file is locked from student replacement.

4. **Given** the student retries or double-clicks final submit
   **When** duplicate requests reach the API
   **Then** only one final submission is recorded
   **And** the result is idempotent or a deterministic conflict.

5. **Given** final submit succeeds
   **When** the success panel appears
   **Then** it shows filename, submitted timestamp, class, mode, and return action.

6. **Given** the assignment/session is no longer open
   **When** final submit is attempted
   **Then** the API blocks the submit with a clear deadline/session-state error.

### Story 5.3: Teacher Speaking Playback And Manual Grading

As a teacher,
I want to open a Speaking submission, play the file, enter score and feedback, and save,
So that Speaking work is graded manually without scattered files or separate notes.

**Coverage:** FR-16, UX-DR14, NFR-5, NFR-6, NFR-7.

**Acceptance Criteria:**

1. **Given** a teacher opens a Speaking submission within their scope
   **When** the detail loads
   **Then** it shows student, class, Đề gốc, mode, submitted timestamp, protected audio/video player, score input, feedback textarea, and save action.

2. **Given** the submitted file exists and the teacher is authorized
   **When** the teacher plays the file
   **Then** playback streams through an authorized endpoint
   **And** the file is not exposed as a public static URL.

3. **Given** the teacher enters an invalid score
   **When** they save
   **Then** the system blocks save with `ERR_SCORE_INVALID`
   **And** feedback draft remains in the UI.

4. **Given** the teacher enters valid score and feedback
   **When** save succeeds
   **Then** grading status changes to Đã chấm/Graded
   **And** score, feedback, grader id, graded timestamp, and audit event are stored.

5. **Given** the Speaking file metadata exists but the physical file is unavailable
   **When** the detail or player loads
   **Then** a recoverable missing-file error is shown
   **And** score/feedback draft is not erased.

6. **Given** the teacher double-clicks save or retries
   **When** duplicate save requests arrive
   **Then** grading save is idempotent/concurrency-safe
   **And** no duplicate grading records are created.

## Epic 6: Results, Dashboard, Visual Consistency, And MVP Validation

Teachers can review submissions across classes/templates/modes, grade in a master-detail workspace, scan dashboard metrics, and validate the MVP against WDS/DD/TS expectations.

### Story 6.1: Results Filtering Table

As a teacher,
I want to filter results by class, template, mode, student, skill, and status,
So that I can find the submissions that need review without leaving my scope.

**Coverage:** FR-17, FR-10, UX-DR14, NFR-1, NFR-4.

**Acceptance Criteria:**

1. **Given** a teacher opens `/teacher/results`
   **When** results load
   **Then** the page shows class, type/mode, template/test, student search, skill, and status filters plus summary counts.

2. **Given** result rows include Homework and Live Exam submissions
   **When** rows render
   **Then** each row preserves mode context, student, class, source template, score/status, and submitted timestamp.

3. **Given** the teacher changes filters
   **When** the query runs
   **Then** rows update within performance budget
   **And** any selected detail is cleared if it no longer matches.

4. **Given** no rows match the filters
   **When** the table is empty
   **Then** the page shows a clear empty state and a clear-filters action.

5. **Given** a teacher tries to filter or open results outside their scope
   **When** the API evaluates resource policies
   **Then** inaccessible data is excluded or rejected server-side.

### Story 6.2: Master-Detail Results And Grading Workspace

As a teacher,
I want a master-detail grading workspace,
So that I can inspect submissions and grade Speaking without losing list context.

**Coverage:** FR-18, FR-16, UX-DR14, NFR-3.

**Acceptance Criteria:**

1. **Given** result rows are loaded
   **When** the teacher selects a row
   **Then** the detail panel opens without navigating away from the table
   **And** selected row state is visually clear.

2. **Given** the selected row is Reading or Listening
   **When** detail loads
   **Then** it shows answer summary and auto_score from the stored AnswerKey version/snapshot.

3. **Given** the selected row is Speaking
   **When** detail loads
   **Then** it reuses the Speaking player/score/feedback save behavior from Story 5.3 within the master-detail panel.

4. **Given** the teacher saves grading in the detail panel
   **When** save succeeds
   **Then** the row status and detail saved timestamp update without losing current filters.

5. **Given** there are more pending Speaking submissions
   **When** the teacher clicks next pending
   **Then** the next scoped pending Speaking row opens in the detail panel.

6. **Given** the workspace is operated with keyboard
   **When** focus moves between filters, table, detail panel, player, score, feedback, and save
   **Then** focus order follows visual order and remains visible.

### Story 6.3: Teacher Dashboard Summary And Recent Work Routing

As a teacher,
I want a quiet dashboard with scan-level metrics and recent work,
So that I can understand current workload and navigate to the right module quickly.

**Coverage:** FR-19, FR-1, UX-DR2, NFR-1.

**Acceptance Criteria:**

1. **Given** a teacher opens `/teacher/dashboard`
   **When** dashboard data loads
   **Then** it shows scan metrics for source templates, active Homework, Live Exams today/open, recent submissions, and Speaking needing grading where data exists.

2. **Given** a class filter is selected
   **When** metrics refresh
   **Then** counts and recent work reflect only the teacher-scoped selected class.

3. **Given** the teacher clicks Thư viện đề, Lớp học, or Kết quả navigation
   **When** navigation occurs
   **Then** the teacher moves to the proper module for core work.

4. **Given** the dashboard has no data yet
   **When** it renders empty metrics
   **Then** it stays calm and operational
   **And** it does not introduce an ambiguous dashboard-only "create test" workflow.

5. **Given** recent work rows are shown
   **When** the teacher opens a row
   **Then** the route preserves whether the row represents template, HomeworkAssignment, LiveExamSession, or result context.

### Story 6.4: Stitch-Informed Visual And Accessibility Hardening

As a teacher or student,
I want the MVP screens to feel consistent, clear, and accessible,
So that operational test workflows are easy to scan and safe to use.

**Coverage:** FR-20, UX-DR15, NFR-3, NFR-8.

**Acceptance Criteria:**

1. **Given** implementation uses Stitch references
   **When** UI copy and layout decisions conflict with PRD/DD/WDS domain semantics
   **Then** PRD/DD/WDS semantics win
   **And** Stitch remains visual/layout inspiration only.

2. **Given** Thư viện đề, create-template wizard, assigned tests, exam workspace, Speaking submission, and Results pages are implemented
   **When** visual review is performed
   **Then** layout patterns align with approved references: operational sidebar/nav, tables/list density, wizard stepper, upload panels, split workspace, badges, and master-detail.

3. **Given** status badges are rendered
   **When** states include Draft, Ready, Homework, Thi trực tiếp, Not open, Open now, Submitted, Needs grading, and Graded
   **Then** colors and labels are consistent and accessible
   **And** mode wording does not collapse Homework and Live Exam into generic "Bài thi".

4. **Given** forms, tables, modals, players, upload zones, and split panels are used
   **When** accessibility checks run
   **Then** labels are visible/programmatic, contrast meets WCAG AA, focus is visible, and keyboard navigation reaches all critical controls.

5. **Given** desktop, tablet, and narrow web viewports
   **When** responsive checks run
   **Then** text and controls do not overlap, critical actions remain reachable, and no page blocks core completion.

6. **Given** the final UI uses shared primitives
   **When** repeated components are inspected
   **Then** status badges, empty states, error banners, upload queues, protected media viewers, autosave status, and shell navigation are reused or intentionally consistent.

### Story 6.5: API Security And Contract Test Coverage

As a product owner,
I want API security and contract tests for the MVP workflow resources,
So that role/scope, DTO shape, errors, and protected media behavior are verified before E2E testing.

**Coverage:** FR-1, FR-3, FR-5, FR-8, FR-9, FR-10, FR-14, FR-15, FR-16, FR-17, NFR-4, NFR-5, NFR-6.

**Acceptance Criteria:**

1. **Given** API contract tests run
   **When** auth, class, template, material, homework, live exam, submission, speaking, grading, and results endpoints are exercised
   **Then** DTO shape, status codes, pagination where used, `ProblemDetails`, and stable business error codes are verified.

2. **Given** role/scope security tests run
   **When** unauthenticated, wrong-role, wrong-teacher-scope, and wrong-student-class cases are exercised
   **Then** protected resources return the expected `401`, `403`, or hidden `404` behavior
   **And** no out-of-scope data is serialized.

3. **Given** protected file tests run
   **When** allowed and denied users request PDF/audio/Speaking files
   **Then** authorized streams work through API endpoints with expected headers/range behavior
   **And** denied users never receive public paths or storage keys.

4. **Given** duplicate action tests run
   **When** mark-ready, create Homework, create/open/close Live Exam, final submit, and grading save are retried
   **Then** idempotency or deterministic conflict behavior is verified.

### Story 6.6: Playwright Happy Path E2E Coverage

As a product owner,
I want automated happy path E2E coverage for the MVP workflows,
So that the product can be reviewed against DD-001 user outcomes.

**Coverage:** FR-4 through FR-18, TS-001 happy paths.

**Acceptance Criteria:**

1. **Given** E2E fixtures create teacher, student, class, membership, and valid files
   **When** Playwright happy path tests run
   **Then** teacher can create a reusable Reading template, upload PDF, enter AnswerKey, review, and mark ready.

2. **Given** a Ready template exists
   **When** E2E happy path tests run
   **Then** teacher can create Homework and Live Exam instances from that template.

3. **Given** assigned student work exists
   **When** E2E happy path tests run
   **Then** student can enter class code, log in, open allowed Reading/Listening work, enter answers, and final-submit.

4. **Given** a Speaking assignment/session exists
   **When** E2E happy path tests run
   **Then** student can upload a Speaking file, final-submit it, and see filename/timestamp confirmation.

5. **Given** a submitted Speaking file exists
   **When** E2E happy path tests run
   **Then** teacher can filter results, open the Speaking submission, play the file, save score/feedback, and see row status update.

### Story 6.7: Blocking Error And Edge Case Test Coverage

As a product owner,
I want automated coverage for MVP blocking errors and edge cases,
So that known TS-001 failure modes are handled before sign-off.

**Coverage:** FR-2, FR-3, FR-5, FR-6, FR-8, FR-9, FR-13, FR-14, FR-15, FR-16, FR-17, TS-001 error and edge cases.

**Acceptance Criteria:**

1. **Given** blocking error tests run
   **When** invalid class code, not-in-class login, missing setup fields, invalid upload, incomplete AnswerKey, invalid Speaking file, invalid score, expired Homework, or unopened Live Exam are triggered
   **Then** each case produces the expected recoverable error and does not corrupt data.

2. **Given** draft and autosave edge tests run
   **When** a student reloads after entering answers or submits with missing answers
   **Then** saved/local answers restore where technically feasible
   **And** the missing-answer warning appears before final submit.

3. **Given** duplicate action edge tests run
   **When** Mark Ready, Create Session, final submit, or grading save are double-clicked or retried
   **Then** only one state transition or record is produced.

4. **Given** results edge tests run
   **When** filters match no rows or a Speaking file is missing
   **Then** the UI shows the expected empty/recoverable state without losing grading context.

### Story 6.8: Accessibility And Visual QA Pass

As a teacher or student,
I want critical flows checked for accessibility and visual consistency,
So that the MVP is usable and aligned with WDS/Stitch references before sign-off.

**Coverage:** FR-20, NFR-3, NFR-8, UX-DR15.

**Acceptance Criteria:**

1. **Given** accessibility checks run
   **When** critical flows are operated by keyboard
   **Then** all blocking keyboard, label, focus, and contrast issues are fixed before MVP sign-off.

2. **Given** responsive checks run
   **When** desktop, tablet, and narrow web viewports are tested
   **Then** critical actions remain reachable
   **And** text, badges, cards, tables, split panels, and modals do not overlap.

3. **Given** visual QA compares implemented screens to WDS/Stitch references
   **When** Thư viện đề, create-template wizard, assigned tests, exam workspace, Speaking submission, Results, and Dashboard are inspected
   **Then** layouts remain operational and scan-friendly
   **And** Stitch does not override DD-001/WDS behavior or wording.

### Story 6.9: Final FR And TS Evidence Matrix

As a product owner,
I want a final evidence matrix mapped to FR-1 through FR-20 and TS-001,
So that MVP sign-off is based on traceable behavior evidence.

**Coverage:** All FRs, all NFRs, TS-001.

**Acceptance Criteria:**

1. **Given** implementation and validation are complete
   **When** the validation report is produced
   **Then** it maps test evidence back to FR-1 through FR-20 and TS-001 happy path/error/edge coverage.

2. **Given** any FR or TS-001 case lacks evidence
   **When** the evidence matrix is reviewed
   **Then** the gap is listed as must-fix, accepted-risk, or explicitly deferred with owner and rationale.

3. **Given** all must-fix issues are resolved
   **When** the final sign-off package is reviewed
   **Then** it includes current readiness status, test summary, known accepted risks, and links to relevant artifacts.
