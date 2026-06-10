---
baseline_commit: 241e9d553074db22e2ccb6e33863e1e82ad5bed8
---

# Story 2.1: Thư Viện Đề List, Search, Filter, And Template Inspection

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

Là giáo viên,
tôi muốn một Thư viện đề có thể tìm kiếm và lọc,
để tôi tìm được Đề gốc tái sử dụng và chọn hành động phù hợp nhanh chóng.

## Acceptance Criteria

1. **Given** giáo viên có templates ở trạng thái Draft, Ready, và Archived
   **When** mở `/teacher/library`
   **Then** trang hiển thị title, skill, status, last-used metadata (khi có), và row actions.

2. **Given** giáo viên search hoặc filter theo skill/status
   **When** filter thay đổi
   **Then** danh sách cập nhật trong performance budget hợp lý
   **And** filter state phản ánh trong query params.

3. **Given** không có template khớp filter hiện tại
   **When** list rỗng
   **Then** empty state rõ ràng với tùy chọn xóa filter hoặc tạo Đề gốc mới.

4. **Given** template chưa Ready
   **When** giáo viên mở row actions
   **Then** Giao homework và Tạo thi trực tiếp bị disabled hoặc blocked với `ERR_TEMPLATE_NOT_READY`.

5. **Given** điều hướng bằng keyboard
   **When** focus di chuyển qua filters, rows, action menus
   **Then** focus order và visible focus states usable.

**Implementation Note (từ Epic 1.4):** Story này **giới thiệu TestTemplate** — phải thêm resource-specific authorization policy (`CanViewTemplateAsTeacher`, ownership scope) trong cùng story.

## Tasks / Subtasks

- [x] Domain + persistence (AC: 1)
  - [x] `Domain/TestTemplates/TestTemplate.cs` — `Id`, `TeacherId`, `Title`, `Skill`, `Description?`, `Status` (draft/ready/archived), `CreatedAt`, `UpdatedAt`, `LastUsedAt?`, `ArchivedAt?`.
  - [x] `Domain/TestTemplates/TemplateSkill.cs`, `TemplateStatuses.cs` constants.
  - [x] EF configuration + migration `TestTemplates` table; index `(TeacherId, Status)`, `(TeacherId, Title)`.
  - [x] MVP demo seeder: 3+ templates (Draft, Ready, Archived) cho demo teacher.

- [x] Application + API list/inspect (AC: 1, 2, Implementation Note)
  - [x] `Application/TestTemplates/ITestTemplateService.cs` — `ListForTeacherAsync`, `GetByIdForTeacherAsync` (scoped by teacherId).
  - [x] `Application/Security/ITemplateAuthorizationService.cs` + `Infrastructure/Authorization/TemplateAuthorizationService.cs` — teacher ownership; hidden 404 cho cross-teacher access.
  - [x] Policy `CanViewTemplateAsTeacher` + handler wired trong `Program.cs`.
  - [x] `Contracts/TestTemplates/TestTemplateListItemResponse.cs`, `TestTemplateDetailResponse.cs`, `TestTemplateListQuery`.
  - [x] `Controllers/TestTemplatesController.cs` — `GET /api/test-templates` (list + query), `GET /api/test-templates/{id}` (detail/inspect).
  - [x] ProblemDetails codes: `templates.notFound`, `templates.forbidden`, `ERR_TEMPLATE_NOT_READY` (extensions.code hoặc client constant — align UX spec).

- [x] API tests (AC: 1, 2, Implementation Note)
  - [x] `TestTemplatesControllerTests` — teacher list scoped; other teacher detail → 404 hidden; student/unauth → 401/403.
  - [x] Filter/search query param tests; empty list shape.

- [x] Angular library page (AC: 1–5)
  - [x] Replace `teacher-placeholder` tại `app.routes.ts` `/teacher/library` bằng `features/test-template-library/`.
  - [x] `core/test-templates/test-templates-api.service.ts` + models; sync filters ↔ query params (`skill`, `status`, `q`).
  - [x] Table/list: title, skill badge, status badge, last-used, row actions menu.
  - [x] Empty state + clear filters + "Tạo đề mới" → `/teacher/library/new/setup` (placeholder route OK nếu 2.2 chưa có — link disabled với tooltip hoặc route tới placeholder).
  - [x] Row actions: Edit/Inspect enabled; Homework + Live Exam disabled khi status ≠ Ready với message `ERR_TEMPLATE_NOT_READY`.
  - [x] Keyboard: tab order filters → table → actions; visible `:focus-visible`.
  - [x] Vitest: filter state, empty state, not-ready action guard.

- [x] Docs + quality
  - [x] `docs/setup/development.md` — smoke teacher library list.
  - [x] `.\scripts\quality.ps1` pass.

### Review Findings

- [x] [Review][Decision] UI template inspection — **inline panel** (metadata từ `GET /api/test-templates/{id}` hiển thị dưới bảng).
- [x] [Review][Patch] Wire inspect action [test-template-library.component.html:99]
- [x] [Review][Patch] Thêm matrix tests cho `/api/test-templates` trong AuthorizationMatrixTests [tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs]
- [x] [Review][Patch] Escape đóng action menu (AC 5 keyboard) [test-template-library.component.ts]
- [x] [Review][Patch] Hủy/ignore stale list fetch khi filter đổi nhanh [test-template-library.component.ts:166]
- [x] [Review][Defer] Triple authorization check trên GetById — mirror ClassesController pattern [TestTemplatesController.cs:56] — deferred, consistent pattern
- [x] [Review][Defer] Working tree trộn Story 1.4 + 2.1 chưa commit — nên tách commit trước merge — deferred, process

## Dev Notes

### Discovery Results

- `epics_content`: `_bmad-output/planning-artifacts/epics.md` — Story 2.1, FR-4, FR-20, UX-DR5, NFR-1, NFR-3.
- `architecture_content`: `_bmad-output/planning-artifacts/architecture.md` — `Domain/TestTemplates`, teacher-owned MVP, hidden 404, `features/test-templates`.
- `ux_content`: `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.3-test-list-test-library/1.3-test-list-test-library.md`
- `prd_content`: `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md` — FR-4 list/search/filter.
- Previous story: `1-4-base-authorization-pattern-and-class-scope-guards.md` (done) — reuse `IHiddenResourceResponseFactory`, `AuthorizationDenialAuditor`, policy handler pattern.

### Story Foundation

Story 2.1 là **vertical slice đầu Epic 2**: giới thiệu `TestTemplate` read surface (list + inspect). Không implement create wizard (2.2), upload (2.3), answer key (2.4), mark ready (2.5).

**Phụ thuộc Epic 1 (done):** Teacher auth, `teacherGuard`, cookie session, authorization framework, ProblemDetails pattern, `ClassesController` patterns.

**Không kéo scope:** HomeworkAssignment/LiveExamSession creation flows, file upload, AnswerKey, template edit form, duplicate/archive mutations (có thể stub row actions disabled nếu mutation thuộc story sau).

### Current Codebase State

| Area | Hiện tại | Thay đổi |
|------|----------|----------|
| `app.routes.ts` | `/teacher/library` → `TeacherPlaceholderComponent` | Real library feature component |
| `teacher-shell` | Nav link `/teacher/library` exists | Giữ nguyên |
| API | Không có `TestTemplate` entity/controller | New domain + endpoints |
| DB | Chỉ Identity + Classes tables | Migration `TestTemplates` |

### Architecture Compliance

- Teacher-owned templates: `TeacherId` FK; list/detail filter `TeacherId == currentUser`. [architecture.md]
- Cross-teacher read → hidden **404** `templates.notFound` (same pattern as Classes story 1.4).
- Student/unauthenticated → 401/403 role gate.
- Controllers → Application → Domain; Infrastructure implements services + EF.
- Cookie auth only; API tests dùng `AuthTestHelper.SignInTeacherAsync`.
- ProblemDetails `extensions.code`; tests assert codes only. [AGENTS.md]

### UX Reference (1.3)

- Route: `/teacher/library`
- Filters: skill (All/Reading/Listening/Speaking), status (Draft/Ready/Archived), search debounced
- Query params persist filter state
- Empty state: "Chưa có đề nào" + clear filters + create CTA
- Row actions: homework/live exam require Ready — `ERR_TEMPLATE_NOT_READY`
- Content keys: `library.title`, `library.status.draft`, etc. (inline Vietnamese OK for MVP; i18n file optional)

### Technical Requirements

- List endpoint: `GET /api/test-templates?skill=&status=&q=&page=&pageSize=` — MVP pageSize default 50, server-side filter.
- Detail endpoint: `GET /api/test-templates/{id}` — metadata only (no materials/answer key in 2.1).
- `LastUsedAt` nullable — có thể null cho seeded data; hiển thị "—" trên UI.
- Performance: debounce search 300ms client; indexed DB columns.
- Seeder idempotent trong `MvpDemoDataSeeder` hoặc test helper riêng.

### Anti-Patterns (từ Epic 1 learnings)

- Không leak template existence cross-teacher (404 not 403).
- Không JWT/localStorage.
- Không implement create/edit API trong story này.
- Policy handler phải scoped DI (không Singleton).
- Angular guard ≠ security — API enforce ownership.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Story 2.1]
- [Source: `_bmad-output/C-UX-Scenarios/.../1.3-test-list-test-library.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — FR-4 mapping]
- [Source: `1-4-base-authorization-pattern-and-class-scope-guards.md` — authorization patterns]

## Dev Agent Record

### Agent Model Used

Composer

### Debug Log References

### Completion Notes List

- Implemented `TestTemplate` domain entity, EF migration `AddTestTemplates`, and idempotent MVP demo seeder (3 templates: Draft/Ready/Archived).
- Added teacher-scoped list/detail API with `CanViewTemplateAsTeacher` policy, hidden 404 for cross-teacher access, and 11 API integration tests.
- Built Angular `/teacher/library` page with debounced search, skill/status filters synced to query params, empty state, row action menu with `ERR_TEMPLATE_NOT_READY` guard, and 4 Vitest specs.
- Quality gate: 62 API tests + 44 client tests pass.

### File List

- src/EnglishTestWeb.Api/Domain/TestTemplates/TestTemplate.cs
- src/EnglishTestWeb.Api/Domain/TestTemplates/TemplateSkill.cs
- src/EnglishTestWeb.Api/Domain/TestTemplates/TemplateStatuses.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/TestTemplateConfiguration.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/*AddTestTemplates*
- src/EnglishTestWeb.Api/Application/TestTemplates/ITestTemplateService.cs
- src/EnglishTestWeb.Api/Application/Security/ITemplateAuthorizationService.cs
- src/EnglishTestWeb.Api/Application/Security/AuthorizationDenialReason.cs
- src/EnglishTestWeb.Api/Contracts/TestTemplates/TestTemplateListItemResponse.cs
- src/EnglishTestWeb.Api/Contracts/TestTemplates/TestTemplateDetailResponse.cs
- src/EnglishTestWeb.Api/Contracts/TestTemplates/TestTemplateListQuery.cs
- src/EnglishTestWeb.Api/Controllers/TestTemplatesController.cs
- src/EnglishTestWeb.Api/Infrastructure/TestTemplates/TestTemplateService.cs
- src/EnglishTestWeb.Api/Infrastructure/Authorization/TemplateAuthorizationService.cs
- src/EnglishTestWeb.Api/Infrastructure/Authorization/Handlers/TemplateTeacherViewRequirement.cs
- src/EnglishTestWeb.Api/Infrastructure/Authorization/Handlers/TemplateTeacherAuthorizationHandler.cs
- src/EnglishTestWeb.Api/Infrastructure/Authorization/Policies/AuthorizationPolicies.cs
- src/EnglishTestWeb.Api/Infrastructure/Identity/MvpDemoDataSeeder.cs
- src/EnglishTestWeb.Api/Program.cs
- tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplatesTestHelper.cs
- tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplatesControllerTests.cs
- src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates.models.ts
- src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates-api.service.ts
- src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.ts
- src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.html
- src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.css
- src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.spec.ts
- src/EnglishTestWeb.Client/src/app/app.routes.ts
- docs/setup/development.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

## Change Log

- 2026-06-10: Story 2.1 created from epics + UX 1.3 + architecture; ready-for-dev.
- 2026-06-10: Story 2.1 implemented — TestTemplate read surface, teacher library UI, authorization policy, tests; status → review.
- 2026-06-10: Code review (uncommitted all) — 1 decision-needed, 4 patch, 2 defer, 2 dismissed; status → in-progress.
- 2026-06-10: Review fixes applied — inline inspect panel, matrix tests, Escape menu, stale fetch guard; status → review.
- 2026-06-10: Re-review pass 2 — clean; all ACs satisfied; status → done.
