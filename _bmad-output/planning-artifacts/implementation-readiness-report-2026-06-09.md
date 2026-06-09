---
workflowType: "implementation-readiness"
project_name: "EnglishTestWeb"
date: "2026-06-09"
stepsCompleted: [1, 2, 3, 4, 5, 6]
status: "complete"
readinessStatus: "ready-with-minor-warnings"
inputDocuments:
  prd:
    - "E:\\Code\\EnglishTestWeb\\_bmad-output\\planning-artifacts\\prds\\prd-EnglishTestWeb-2026-06-09\\prd.md"
    - "E:\\Code\\EnglishTestWeb\\_bmad-output\\planning-artifacts\\prds\\prd-EnglishTestWeb-2026-06-09\\addendum.md"
  architecture:
    - "E:\\Code\\EnglishTestWeb\\_bmad-output\\planning-artifacts\\architecture.md"
  epics:
    - "E:\\Code\\EnglishTestWeb\\_bmad-output\\planning-artifacts\\epics.md"
  ux:
    - "E:\\Code\\EnglishTestWeb\\_bmad-output\\C-UX-Scenarios"
  behavior:
    - "E:\\Code\\EnglishTestWeb\\_bmad-output\\E-Development\\deliveries\\DD-001-mvp-test-workflows.yaml"
    - "E:\\Code\\EnglishTestWeb\\_bmad-output\\E-Development\\test-scenarios\\TS-001-mvp-test-workflows.yaml"
  visualReference:
    - "E:\\Code\\EnglishTestWeb\\docs\\stitch_h_th_ng_kh_o_th_englishtestweb\\STITCH_MAPPING.md"
---

# Implementation Readiness Assessment Report

**Date:** 2026-06-09
**Project:** EnglishTestWeb

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**

- `E:\Code\EnglishTestWeb\_bmad-output\planning-artifacts\prds\prd-EnglishTestWeb-2026-06-09\prd.md`
  - Size: 25,627 bytes
  - Modified: 2026-06-09 10:43:50
- `E:\Code\EnglishTestWeb\_bmad-output\planning-artifacts\prds\prd-EnglishTestWeb-2026-06-09\addendum.md`
  - Size: 2,083 bytes
  - Modified: 2026-06-09 10:43:50
- `E:\Code\EnglishTestWeb\_bmad-output\planning-artifacts\prds\prd-EnglishTestWeb-2026-06-09\.decision-log.md`
  - Size: 2,094 bytes
  - Modified: 2026-06-09 10:43:50

**Sharded Documents:**

- No PRD `index.md` shard set found.

### Architecture Files Found

**Whole Documents:**

- `E:\Code\EnglishTestWeb\_bmad-output\planning-artifacts\architecture.md`
  - Size: 64,974 bytes
  - Modified: 2026-06-09 15:21:16

**Sharded Documents:**

- No architecture `index.md` shard set found.

### Epics And Stories Files Found

**Whole Documents:**

- `E:\Code\EnglishTestWeb\_bmad-output\planning-artifacts\epics.md`
  - Size: 53,749 bytes
  - Modified: 2026-06-09 21:11:27

**Sharded Documents:**

- No epics `index.md` shard set found.

### UX Design Files Found

**Whole Documents Under Planning Artifacts:**

- No UX whole document found under `E:\Code\EnglishTestWeb\_bmad-output\planning-artifacts`.

**Project UX Source Folder:**

- `E:\Code\EnglishTestWeb\_bmad-output\C-UX-Scenarios`
  - 19 Markdown files found.
  - Includes scenario index, 3 scenario outlines, and 15 page specs.
  - This folder will be treated as the UX behavior/spec source for the assessment.

### Behavior, Test, And Visual Reference Sources Found

- `E:\Code\EnglishTestWeb\_bmad-output\E-Development\deliveries\DD-001-mvp-test-workflows.yaml`
  - Size: 12,140 bytes
  - Modified: 2026-06-09 10:22:22
- `E:\Code\EnglishTestWeb\_bmad-output\E-Development\test-scenarios\TS-001-mvp-test-workflows.yaml`
  - Size: 9,290 bytes
  - Modified: 2026-06-08 22:08:07
- `E:\Code\EnglishTestWeb\docs\stitch_h_th_ng_kh_o_th_englishtestweb\STITCH_MAPPING.md`
  - Size: 4,278 bytes
  - Modified: 2026-06-09 09:42:30

### Discovery Issues

- No critical duplicate whole-vs-sharded document formats were found.
- UX specs are outside the default `planning_artifacts` folder, but they are explicit source inputs for this project and will be included.
- The previous readiness report was stale because it was generated before `epics.md` existed; this report refreshes the run with the current epics and stories artifact.

### Confirmed Assessment Inputs

- Primary PRD: `prd.md`
- PRD addendum: `addendum.md`
- Architecture: `architecture.md`
- Epics and stories: `epics.md`
- Behavior/domain source: `DD-001-mvp-test-workflows.yaml` and WDS page specs
- Test source: `TS-001-mvp-test-workflows.yaml`
- Visual/layout reference only: `STITCH_MAPPING.md`

## PRD Analysis

### Functional Requirements

FR-1: Teacher Authentication And Role Access. Teacher can log in and access Teacher Dashboard, Thư viện đề, lớp, and kết quả surfaces. Teacher-only routes reject unauthenticated users. Student users cannot access teacher management or grading screens. Teacher sees only classes, templates, assignments, sessions, and submissions in their scope.

FR-2: Student Class Code Entry. Student can enter a class code before student login and confirm the selected Class context. Invalid or expired class code shows a clear retryable error. Class confirmation appears before Student proceeds to login. Class context is preserved after login.

FR-3: Student Membership Enforcement. Student can access work only when a ClassMembership exists for the selected Class. Student account not in selected Class is blocked with a clear next step. Direct route access to another Class, HomeworkAssignment, LiveExamSession, or Submission is rejected. All Student lists and attempts are scoped to the active Class.

FR-4: Create And Manage Đề gốc. Teacher can create, save draft, edit, list, search/filter, and inspect Đề gốc in Thư viện đề. Đề gốc stores title, skill, description, and status. Draft Đề gốc can be edited without creating Student-visible work. Ready Đề gốc exposes usage actions: Giao homework and Tạo thi trực tiếp.

FR-5: Upload TestMaterial. Teacher can attach required PDF and optional audio/cue materials to Đề gốc. Reading requires a PDF before mark ready. Listening requires a PDF and can include audio. Speaking can use text/cue card/PDF prompt plus later Student upload. Upload failure preserves draft state and allows retry/replace. Large uploads show progress.

FR-6: Configure AnswerKey For Reading/Listening. Teacher can configure question count, correct answer, scoring mode, and score per question or total score for Reading/Listening. Missing answer rows are identified by question number. Invalid scoring blocks mark ready. AnswerKey is versioned or otherwise protected so submitted work remains gradeable against the intended key.

FR-7: Mark Đề gốc Ready. Teacher can mark Đề gốc as Ready only when required TestMaterial and validation rules pass. Double-clicking mark ready creates only one state transition. Ready state is required before creating HomeworkAssignment or LiveExamSession. Ready does not assign the template to any Class by itself.

FR-8: Create HomeworkAssignment. Teacher can create HomeworkAssignment from a Ready Đề gốc for a Class with due date and optional time limit. HomeworkAssignment references exactly one Đề gốc and one Class. Student sees Homework only when assigned and allowed by membership/status. New attempts are blocked after deadline unless extension/reopen rules are later defined. Homework due state appears in Student Assigned Tests and Results.

FR-9: Create And Control LiveExamSession. Teacher can create LiveExamSession from a Ready Đề gốc for a Class and control whether the session is open or closed. LiveExamSession references exactly one Đề gốc and one Class. Student cannot start Live Exam before the session is open. Closed LiveExamSession blocks new attempts. MVP assumes manual open/close unless later re-scoped.

FR-10: Preserve Mode Context. System must show and persist whether work is Homework or Thi trực tiếp in Student lists, exam workspace, submissions, results, and grading. Submission must reference either HomeworkAssignment or LiveExamSession. A Submission cannot reference both modes at the same time. Results filtering includes Mode. UI labels avoid using "Bài thi" generically for Đề gốc when the object is still only a reusable template.

FR-11: Student Assigned Tests List. Student can view available Homework and Thi trực tiếp items grouped or filtered by mode and status. Empty state is tied to active Class, not a generic failure. Status labels include not started, in progress, submitted, not open, open now, needs grading, and graded where relevant. Speaking routes to Speaking submission; Reading/Listening routes to exam workspace.

FR-12: Reading/Listening Exam Workspace. Student can view PDF by page, play audio when present, enter answers in a separate answer form, track progress, and submit. PDF viewer and answer form remain visible in a stable split workspace on desktop/laptop. Listening audio is playable in the workspace. Student can see active Class, Đề gốc title, Mode, save state, and submit action. MVP does not require rendering individual parsed questions from PDF.

FR-13: Draft Answer Persistence. System saves answer drafts during Reading/Listening attempts where technically feasible. Autosave acknowledgement appears within 1 second on normal connection. Reload should restore saved/local answers where technically feasible. Degraded/offline state must not imply final submission succeeded.

FR-14: Final Submission And Auto-Grading. Student can final-submit Reading/Listening; system locks the attempt and auto-grades against AnswerKey. Submission confirmation warns if answers are missing. Final submission cannot be duplicated by double-click. Submitted answers become read-only for Student. Auto_score is stored on Submission and results become visible to Teacher.

FR-15: Student Speaking File Submission. Student can upload a valid Speaking file, see draft upload status, and confirm final submission. Missing or invalid file blocks final submit with clear error. Uploaded-but-not-submitted file remains draft. Final submitted state shows filename and timestamp. MVP supports file upload first unless browser recording is explicitly re-scoped.

FR-16: Teacher Speaking Grading. Teacher can open SpeakingSubmission, play the file, enter score and feedback, and save grading. Score validation enforces configured max/min score. Save updates row status to Đã chấm. Missing file error is recoverable and does not erase score/feedback draft. Grading context shows Student, Class, Đề gốc, and Mode.

FR-17: Results Filtering. Teacher can filter results by Class, Đề gốc, Mode, Student, skill, and status. No-match filter state provides a clear empty state and option to clear filters. Result rows preserve HomeworkAssignment or LiveExamSession context. Teacher cannot view results outside their scope.

FR-18: Master-Detail Grading Workspace. Teacher can select a result row and see detail without losing list context. Results table and detail/grading panel can be used side by side on desktop. Speaking audio player, score input, feedback, and save action are together. Keyboard navigation and focus states work across list and detail panel.

FR-19: Teacher Dashboard Summary. Teacher Dashboard shows scan-level metrics and recent work, then routes to modules rather than hiding core workflows inside dashboard cards. Dashboard can show source templates, active Homework, live exams today, new submissions, and Speaking queue. Primary navigation includes Dashboard, Thư viện đề, Lớp, and Kết quả. Creating Đề gốc starts from Thư viện đề, not from an ambiguous dashboard shortcut.

FR-20: Apply Visual Mapping Without Changing Domain Semantics. Implementation can borrow layout, spacing, badges, sidebar, tables, wizard, and split-panel patterns from Stitch, but must keep DD-001 domain semantics. Library actions say Giao homework and Tạo thi trực tiếp, not generic Giao bài only. Student-facing labels can use Bài tập về nhà and Thi trực tiếp. Results always show usage mode. Styling should remain calm, operational, and utility-first.

**Total FRs:** 20

### Non-Functional Requirements

NFR-1 Performance: Dashboard, library, assigned work, and results list load initial content in under 2 seconds on normal broadband.

NFR-2 Autosave Feedback: Autosave acknowledgement appears within 1 second when online.

NFR-3 Accessibility: Core flows are keyboard accessible; form labels are visible/programmatic; focus order follows visual order; color contrast meets WCAG AA.

NFR-4 Security And Scope: Role-based access prevents Teacher/Student viewing data outside their scope; direct route access is guarded server-side.

NFR-5 Data Integrity: Submission, mark-ready, homework creation, live-session creation, and grading save are protected against duplicate actions.

NFR-6 File Safety: PDF/audio/Speaking storage requires secure access controls, upload progress, retry/replace behavior, and recoverable errors.

NFR-7 Auditability: Key state transitions should be traceable enough for Teacher support: template ready, assignment/session created, session opened/closed, submission finalized, and grading saved.

NFR-8 Responsive Baseline: MVP is desktop/laptop-first, but pages should degrade safely on tablet/mobile web without content overlap or blocked critical actions.

**Total NFRs:** 8

### Additional Requirements

- Source hierarchy: PRD is the consolidated requirements bridge; DD-001 and WDS page specs are behavior/domain source of truth; Stitch HTML/screens are visual references only.
- MVP uses upload PDF/audio plus separate answer form; it does not parse PDF into questions.
- Thư viện đề contains reusable Đề gốc only; HomeworkAssignment and LiveExamSession are separate usage modes.
- Reading/Listening is auto-graded using teacher-defined AnswerKey.
- Speaking is manually graded; AI Speaking grading is out of scope.
- Native mobile app, LMS/CRM/payment/scheduling, export, advanced analytics, browser-based Speaking recording, and automatic live-exam opening are out of MVP unless re-scoped.
- Success metrics include creating a Reading Đề gốc in under 10 minutes, student submission without teacher guidance, teacher Speaking grading in one workspace, 100% TS-001 happy path/blocking error pass, no critical RBAC defects, and autosave/reload preservation in normal online use.

### Open Questions From PRD

1. Can Teacher extend or reopen Homework after the due date?
2. Should LiveExamSession open manually only, by schedule only, or both?
3. What max score and validation range should Speaking use in MVP?
4. What file formats and max file sizes are allowed for PDF, Listening audio, and Speaking uploads?
5. Should Student see auto-score immediately after Reading/Listening submission, or should scores remain Teacher-only until released?
6. Should AnswerKey edits be blocked after submissions exist, or create a new version for future submissions?
7. Is browser-based Speaking recording intentionally deferred, or should it be included in the first build?
8. Are Class and Student accounts created manually by Teacher/admin in MVP, or imported from a spreadsheet?

### PRD Completeness Assessment

The PRD is complete enough for readiness validation: it has 20 numbered FRs, 8 NFRs, clear MVP scope, explicit non-goals, source hierarchy, glossary, user journeys, success metrics, and assumptions. Several policy details remain open, but most are either resolved by architecture or suitable to finalize at story implementation time.

## Epic Coverage Validation

### Epic FR Coverage Extracted

FR-1: Covered in Epic 1 and Epic 6; stories 1.1, 1.2, 1.4, and 6.3.

FR-2: Covered in Epic 1; story 1.3.

FR-3: Covered in Epic 1; stories 1.3 and 1.4.

FR-4: Covered in Epic 2; stories 2.1 and 2.2.

FR-5: Covered in Epic 2; story 2.3.

FR-6: Covered in Epic 2; story 2.4.

FR-7: Covered in Epic 2; story 2.5.

FR-8: Covered in Epic 3; story 3.1.

FR-9: Covered in Epic 3; story 3.2.

FR-10: Covered in Epic 3, Epic 4, Epic 5, and Epic 6; stories 3.1, 3.2, 3.3, 4.1, 4.2, 4.4, 5.2, and 6.1.

FR-11: Covered in Epic 4; story 4.1.

FR-12: Covered in Epic 4; story 4.2.

FR-13: Covered in Epic 4; story 4.3.

FR-14: Covered in Epic 4; story 4.4.

FR-15: Covered in Epic 5; stories 5.1 and 5.2.

FR-16: Covered in Epic 5 and Epic 6; stories 5.3 and 6.2.

FR-17: Covered in Epic 6; story 6.1.

FR-18: Covered in Epic 6; story 6.2.

FR-19: Covered in Epic 6; story 6.3.

FR-20: Covered in Epic 2, Epic 3, and Epic 6; stories 2.1, 2.3, 2.5, 3.3, and 6.4.

**Total FRs in epics:** 20

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Teacher authentication and role access | Epic 1 Stories 1.1, 1.2, 1.4; Epic 6 Story 6.3 | Covered |
| FR-2 | Student class code entry | Epic 1 Story 1.3 | Covered |
| FR-3 | Student membership enforcement | Epic 1 Stories 1.3, 1.4 | Covered |
| FR-4 | Create and manage Đề gốc | Epic 2 Stories 2.1, 2.2 | Covered |
| FR-5 | Upload TestMaterial | Epic 2 Story 2.3 | Covered |
| FR-6 | Configure AnswerKey for Reading/Listening | Epic 2 Story 2.4 | Covered |
| FR-7 | Mark Đề gốc Ready | Epic 2 Story 2.5 | Covered |
| FR-8 | Create HomeworkAssignment | Epic 3 Story 3.1 | Covered |
| FR-9 | Create and control LiveExamSession | Epic 3 Story 3.2 | Covered |
| FR-10 | Preserve mode context | Epic 3 Stories 3.1-3.3; Epic 4 Stories 4.1, 4.2, 4.4; Epic 5 Story 5.2; Epic 6 Story 6.1 | Covered |
| FR-11 | Student Assigned Tests list | Epic 4 Story 4.1 | Covered |
| FR-12 | Reading/Listening workspace | Epic 4 Story 4.2 | Covered |
| FR-13 | Draft answer persistence | Epic 4 Story 4.3 | Covered |
| FR-14 | Final submission and auto-grading | Epic 4 Story 4.4 | Covered |
| FR-15 | Student Speaking file submission | Epic 5 Stories 5.1, 5.2 | Covered |
| FR-16 | Teacher Speaking grading | Epic 5 Story 5.3; Epic 6 Story 6.2 | Covered |
| FR-17 | Results filtering | Epic 6 Story 6.1 | Covered |
| FR-18 | Master-detail grading workspace | Epic 6 Story 6.2 | Covered |
| FR-19 | Teacher Dashboard summary | Epic 6 Story 6.3 | Covered |
| FR-20 | Apply visual mapping without changing domain semantics | Epic 2 Stories 2.1, 2.3, 2.5; Epic 3 Story 3.3; Epic 6 Story 6.4 | Covered |

### Missing Requirements

No missing FR coverage found. All PRD FR-1 through FR-20 are represented in the epic coverage map and in story-level coverage declarations.

### Coverage Statistics

- Total PRD FRs: 20
- FRs covered in epics: 20
- Coverage percentage: 100%
- FRs in epics but not in PRD: None found.

## UX Alignment Assessment

### UX Document Status

UX documentation exists and is usable for readiness validation.

- 19 WDS Markdown scenario/page spec files found under `E:\Code\EnglishTestWeb\_bmad-output\C-UX-Scenarios`.
- UX coverage includes 3 scenario outlines and 15 page specs:
  - Teacher login, dashboard, library, create-template setup/materials/answer-key/review.
  - Student class code, student login, assigned tests, Reading/Listening attempt, Speaking submission.
  - Teacher results-dashboard path and Results & Grading workspace.
- Visual/layout mapping exists at `E:\Code\EnglishTestWeb\docs\stitch_h_th_ng_kh_o_th_englishtestweb\STITCH_MAPPING.md`.
- Visual token/style reference exists at `E:\Code\EnglishTestWeb\docs\stitch_h_th_ng_kh_o_th_englishtestweb\proctor_pedagogy\DESIGN.md`.

### UX To PRD Alignment

- Aligned: PRD UJ-1 maps to WDS Scenario 01 and Epic 2 template creation stories.
- Aligned: PRD UJ-2 maps to WDS Scenario 02 and Epic 4/Epic 5 student work stories.
- Aligned: PRD UJ-3 maps to WDS Scenario 03 and Epic 6 results/grading stories.
- Aligned: PRD corrected domain model is reinforced in WDS: Thư viện đề contains reusable Đề gốc; Homework and Live Exam are separate usage modes.
- Aligned: PRD PDF/audio plus separate answer form is reflected in WDS technical notes for upload/materials and attempt workspace.
- Aligned: PRD Speaking upload-first/manual grading is reflected in WDS Speaking submission and Results & Grading specs.
- Aligned: PRD visual source rule is reflected in Stitch mapping and in Epic 6 Story 6.4: Stitch is visual/layout inspiration only and cannot override PRD/DD/WDS behavior.

### UX To Architecture Alignment

- Aligned: Architecture assigns Angular route-level features for student-class-entry, test-templates, homework-assignments, live-exam-sessions, assigned-tests, attempt-workspace, speaking-submission, results-grading, and dashboard.
- Aligned: Architecture supports WDS split workspaces via shared layouts such as `AttemptShell` and `GradingShell`.
- Aligned: Architecture supports upload and media UX through protected storage, authorized streaming, range support, upload queue, protected media viewer, and file metadata.
- Aligned: Architecture supports UX state requirements through shared status, autosave, deadline/live session state, `ProblemDetails`, stable business error codes, and route/API guard separation.
- Aligned: Architecture covers NFRs that UX depends on: under-2-second list loads, autosave feedback, WCAG AA, keyboard focus, server-side authorization, idempotency, protected files, and responsive baseline.

### Alignment Issues

1. **Route naming inconsistency in create-template WDS specs.**
   - Status: Corrected in post-assessment pass.
   - Evidence: Setup/review routes used `/teacher/library/new/...`, while materials/answer-key page specs previously used the older teacher-tests route family.
   - Impact: Dev agents may create inconsistent Angular routes or route aliases for the same wizard.
   - Resolution: Materials and answer-key routes were normalized under `/teacher/library/new/...`.

2. **No dedicated WDS page spec for HomeworkAssignment creation and LiveExamSession creation/control.**
   - Status: Mitigated in post-assessment pass.
   - Evidence: WDS review page says next actions route to Homework assignment flow or Live exam session flow, and PRD/Epics define FR-8/FR-9, but no separate page spec exists for those teacher creation/control screens.
   - Impact: Stories 3.1 and 3.2 are implementable from PRD/DD/Architecture, but UX object IDs, layout, copy, and states for those screens are less precise than other core screens.
   - Resolution: Story 3.1 and Story 3.2 now require feature contracts with routes, stable object ids, fields, validation, and states. Full WDS page specs remain optional.

### Warnings

- UX docs live outside `planning_artifacts`; this is acceptable because the project explicitly references `_bmad-output/C-UX-Scenarios`, but future automation should keep this path in input documents.
- Product policy details still require story-level finalization: Homework reopen/extension copy, Speaking score range, file format/size limits, student score visibility, and class/student provisioning.

### UX Alignment Verdict

UX is substantially aligned with PRD and Architecture. The two issues above are not fatal blockers for implementation readiness if sprint planning explicitly resolves them, but they should not be left implicit for dev agents.

## Epic Quality Review

### Overall Structure Assessment

The epic structure is mostly user-value oriented and follows a coherent delivery path:

1. Secure workspace and class access.
2. Reusable Đề gốc library and creation.
3. Homework/Live Exam delivery.
4. Student assigned work and Reading/Listening attempts.
5. Speaking submission and manual grading.
6. Results, dashboard, visual consistency, and MVP validation.

The required starter/baseline story exists as Story 1.1 and correctly includes .NET 10 Web API, Angular 22, SQL Server, Identity, and protected storage. It does not create all domain tables upfront, which is correct.

### Critical Violations

No critical violation found that would make the entire epic/story set unusable.

### Corrected Major Findings From Initial Assessment

The following findings are retained as historical audit trail only. Each item was corrected in the post-assessment pass; the current status is summarized in the Status and Applied correction fields.

#### Corrected Major 1: Original Story 1.4 Scope Guard Overreach

**Original story:** 1.4 Server-Side Resource Scope Guards

**Status:** Corrected in post-assessment pass.

**Original issue:** The story required scope checks for classes, templates, assignments, sessions, submissions, and files before many of those resources existed. This created a forward-dependency risk and could have forced premature generic authorization code that later stories would rework.

**Why it matters:** Story 1.4 should be completable using only Story 1.1-1.3 output. At that point, Identity/Class/ClassMembership can exist, but TestTemplate, HomeworkAssignment, LiveExamSession, Submission, and file metadata may not.

**Applied correction:**

- Story 1.4 is now `Base Authorization Pattern And Class Scope Guards`.
- It builds the resource-authorization framework, current-user service, policy test harness, and class/member scope checks.
- Template/file/assignment/session/submission-specific scope ACs are left to the first story that creates each resource.

#### Corrected Major 2: Original Story 3.3 Forward Reference To Future Submission Constraints

**Original story:** 3.3 Usage Mode Contract Across Delivery Surfaces

**Status:** Corrected in post-assessment pass.

**Original issue:** AC 5 said future Submission records would enforce exactly-one HomeworkAssignment or LiveExamSession when database constraints were applied in the attempt story. That was explicitly a future-story dependency.

**Why it matters:** Story 3.3 can validate delivery DTOs and mode labels independently, but it cannot complete a future Submission constraint until Epic 4 creates attempts/submissions.

**Applied correction:** The future Submission constraint was removed from Story 3.3 ACs and retained only as a non-AC implementation note pointing to Story 4.2, where the actual exactly-one source constraint belongs.

#### Corrected Major 3: Original Account, Class, And Membership Provisioning Gap

**Originally affected stories:** 1.1, 1.2, 1.3

**Status:** Corrected in post-assessment pass.

**Original issue:** The PRD left class/student account provisioning open. Story 1.3 assumed an active Class with class code and student memberships existed, but there was no explicit story that created teacher/student accounts, classes, memberships, or seed/provisioning data for MVP beyond role seeding.

**Why it matters:** FR-1 to FR-3 cannot be tested end-to-end without a defined way to create or seed users/classes/memberships.

**Applied correction:** Story 1.3 now states the chosen MVP provisioning path: idempotent seed/admin provisioning creates Teacher, Student, Class, and ClassMembership test/demo data for the first build.

#### Corrected Major 4: Original Story 6.5 Test Gate Was Too Broad

**Original story:** 6.5 MVP Workflow Test Coverage And Sign-Off Gate

**Status:** Corrected in post-assessment pass.

**Original issue:** It covered all happy paths, blocking errors, edge cases, security tests, accessibility checks, and final evidence mapping. That was closer to a QA epic/gate than a single implementation story.

**Why it matters:** The create-epics-and-stories standard requires each story to be completable by a single dev agent. Story 6.5 risks being too large and hard to estimate.

**Applied correction:** The original broad test gate was split into Stories 6.5 through 6.9:

- API/security/contract test coverage.
- Playwright E2E happy paths.
- Blocking error and edge-case test suite.
- Accessibility and visual QA pass.
- Final FR/TS evidence matrix.

### Minor Concerns

#### Minor 1: CI/CD Or Automated Quality Gate Is Not Explicit Early

**Status:** Corrected in post-assessment pass.

**Original issue:** Story 1.1 included local build/test smoke checks, but there was no explicit CI or automated quality gate story.

**Applied correction:** Story 1.1 now includes a minimal CI/local quality smoke AC for API build/tests and Angular install/build/test smoke once the scaffold exists.

#### Minor 2: Several Policy Values Are Referenced But Not Finalized

**Examples:** file type/size limits, Speaking score range, student score visibility, Homework extension/reopen copy.

**Recommendation:** Resolve these during sprint planning or include explicit "MVP default policy" ACs in the first story that needs each value.

#### Minor 3: Route Naming Should Be Normalized Before Implementation

**Status:** Corrected in post-assessment pass.

**Original issue:** WDS create-template routes previously mixed the teacher-library and teacher-tests route families.

**Applied correction:** Materials and answer-key WDS routes were normalized under `/teacher/library/new/...` to preserve the corrected Thư viện đề model.

### Best Practices Compliance Checklist

| Check | Result | Notes |
| --- | --- | --- |
| Epics deliver user value | Pass with caveat | Epic 1 contains required technical baseline, but overall goal is secure workspace entry. |
| Epic independence | Mostly pass | Epic 1-6 flow is natural; no later epic is needed for earlier epic's main user value, except noted forward references. |
| Stories appropriately sized | Pass after correction | Original Story 6.5 was split into Stories 6.5 through 6.9. |
| No forward dependencies | Pass after correction | Story 1.4 and Story 3.3 forward dependencies were moved out of ACs. |
| Database/entity timing | Pass after correction | Story 1.1 avoids all-domain-table creation; resource-specific constraints are handled in first-needed stories. |
| Clear acceptance criteria | Mostly pass | ACs are generally testable; policy values need final defaults. |
| Traceability to FRs | Pass | FR coverage is complete and story-level coverage is present. |

### Epic Quality Verdict

After the targeted correction pass, epics/stories meet the strict BMad structure well enough to proceed to sprint planning with minor policy warnings. The remaining decisions are first-needed defaults, not structural blockers.

## Summary and Recommendations

### Overall Readiness Status

**READY WITH MINOR WARNINGS**

EnglishTestWeb is not blocked by missing PRD/Architecture/Epics coverage. The core artifacts are present, FR coverage is complete, architecture supports the product direction, and UX is substantially aligned. The initial assessment found major handoff issues, but a targeted correction pass has now resolved the sprint-planning blockers.

### Findings Summary

- Document discovery: all required core sources found.
- PRD analysis: 20 FRs and 8 NFRs extracted; PRD is complete enough for validation.
- Epic coverage: 20/20 FRs covered; 100% FR coverage.
- UX alignment: substantially aligned; route inconsistency has been normalized, and Homework/Live Exam feature contracts are now called out in stories.
- Epic quality: no critical violations; initial major issues have been corrected, with minor policy defaults remaining for sprint planning.

### Critical Issues Requiring Immediate Action

No critical issues found.

### Major Issues Identified And Corrected

1. **Refactor Story 1.4 scope guards.**
   - Status: Corrected.
   - Story 1.4 is now a base authorization framework/class-scope story.
   - Resource-specific scope requirements are left to the stories that introduce each resource.

2. **Remove forward dependency from Story 3.3.**
   - Status: Corrected.
   - The future Submission constraint was removed from ACs and retained as an implementation note pointing to Story 4.2.

3. **Close MVP account/class/membership provisioning.**
   - Status: Corrected.
   - Story 1.3 now chooses idempotent seed/admin provisioning for Teacher, Student, Class, and ClassMembership in the first build.

4. **Split or reclassify Story 6.5.**
   - Status: Corrected.
   - The original broad test gate is now split into Stories 6.5 through 6.9.

### Remaining Minor Warnings

1. First-needed MVP policy defaults still need final values during sprint planning:
   - PDF/audio/Speaking allowed file formats and size limits.
   - Speaking score range.
   - Student score visibility after Reading/Listening.
   - Homework reopen/extension behavior or explicit deferral.

2. Story 3.1 and Story 3.2 now define route/object contract expectations, but a future UX pass could still create full WDS page specs for HomeworkAssignment creation and LiveExamSession control if desired.

3. Story 1.1 now includes a minimal CI/local quality smoke AC; a full CI pipeline can still be expanded later if needed.

### Recommended Next Steps

1. Optionally re-run `bmad-check-implementation-readiness` in a fresh pass to generate a clean report from the corrected artifacts.

2. Proceed to `[SP] bmad-sprint-planning` if the team accepts the remaining minor warnings as sprint-planning decisions.

3. During sprint planning, keep Story 1.1 first exactly as required: baseline .NET 10 Web API + Angular 22 + SQL Server + Identity + protected storage.

### Final Note

This assessment initially identified 9 unique issues/warnings across UX alignment, story quality, and implementation policy readiness. The targeted correction pass resolved the major sprint-planning blockers. The remaining work is policy-detail selection that can be handled during sprint planning or the first story that needs each value.

**Assessor:** Codex using `bmad-check-implementation-readiness`  
**Completed:** 2026-06-09  
**Post-correction update:** 2026-06-09
