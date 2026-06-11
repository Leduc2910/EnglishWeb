---
baseline_commit: 0c3e45d3a8f2e1b0c9d4a7e6f5b4c3d2e1f0a9b8
---

# Story 2.2: Create, Edit, And Save Draft Template Setup

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

Là giáo viên,
tôi muốn tạo hoặc chỉnh sửa thông tin cơ bản của một Đề gốc,
để template có đúng tên, kỹ năng và ghi chú trước khi upload tài liệu.

## Acceptance Criteria

1. **Given** giáo viên bắt đầu tạo đề mới
   **When** `/teacher/library/new/setup` load
   **Then** wizard hiển thị Bước 1/4 với tên đề, skill segmented control, mô tả tùy chọn, tags tùy chọn, draft summary sidebar, và footer actions (Quay lại | Lưu nháp | Tiếp tục).

2. **Given** giáo viên submit tên đề trống hoặc quá ngắn (< 3 ký tự)
   **When** validation chạy (blur hoặc submit)
   **Then** field hiển thị lỗi `ERR_TEMPLATE_NAME_REQUIRED` (server: `templates.nameRequired`)
   **And** không advance sang bước upload.

3. **Given** giáo viên chọn Reading, Listening, hoặc Speaking
   **When** skill thay đổi
   **Then** draft summary cập nhật checklist yêu cầu tài liệu/AnswerKey theo skill (UI-only preview cho bước 2–3)
   **And** form **không** hỏi Class, deadline, time limit, hay session timing.

4. **Given** dữ liệu setup hợp lệ
   **When** giáo viên Lưu nháp hoặc Tiếp tục
   **Then** draft `TestTemplate` được tạo mới hoặc cập nhật trong phạm vi teacher
   **And** double-click / retry không tạo thêm bản draft trùng (POST một lần, sau đó chỉ PUT cùng `templateId`).

5. **Given** giáo viên resume một draft đã có
   **When** mở `/teacher/library/{templateId}/setup`
   **Then** các giá trị setup trước đó được load và chỉnh sửa được
   **And** template Ready/Archived không cho sửa setup (API `409` + `templates.notEditable`; UI redirect hoặc read-only message).

**Implementation Note:** Story này **mở rộng TestTemplate write surface** — thêm policy mutate (`CanEditTemplateAsTeacher`), validation contracts, và wizard Step 1. Không implement upload (2.3), AnswerKey (2.4), mark ready (2.5).

## Tasks / Subtasks

- [x] Domain + persistence (AC: 4, 5)
  - [x] Mở rộng `TestTemplate` — thêm `Tags` (JSON array string hoặc owned collection; MVP: column `TagsJson` max 500 chars, deserialize `string[]`).
  - [x] Cập nhật `TestTemplateConfiguration` — `Title` validation length align UX (3–120 client, DB max 200 OK).
  - [x] Migration mới (ví dụ `AddTestTemplateTags`) — không breaking existing seeded rows.
  - [x] Đảm bảo `Status` mặc định `draft` khi create.

- [x] Application + API create/update (AC: 2, 4, 5, Implementation Note)
  - [x] Mở rộng `ITestTemplateService`:
    - `CreateDraftForTeacherAsync(teacherId, CreateTestTemplateRequest)`
    - `UpdateDraftSetupForTeacherAsync(templateId, teacherId, UpdateTestTemplateRequest)`
  - [x] Contracts:
    - `CreateTestTemplateRequest` — `title`, `skill`, `description?`, `tags?`
    - `UpdateTestTemplateRequest` — same fields
    - `TestTemplateSetupResponse` — detail fields + `tags` array (camelCase JSON)
  - [x] Mở rộng `TestTemplateDetailResponse` — include `tags`.
  - [x] `TestTemplatesController`:
    - `POST /api/test-templates` — `[Authorize(Roles = Teacher)]`, XSRF required; tạo draft; trả `201` + setup response.
    - `PUT /api/test-templates/{id}` — ownership + **chỉ `status == draft`**; trả `200` hoặc `409 templates.notEditable`.
  - [x] Validation + ProblemDetails codes:
    - `templates.nameRequired` ↔ client `ERR_TEMPLATE_NAME_REQUIRED`
    - `templates.skillRequired` ↔ `ERR_SKILL_REQUIRED`
    - `templates.tagLimit` ↔ `ERR_TAG_LIMIT` (max 10 tags)
    - `templates.skillInvalid` — skill không thuộc reading/listening/speaking
    - `templates.notEditable` — sửa Ready/Archived
    - Giữ `templates.notFound` hidden 404 cho cross-teacher.
  - [x] Policy `CanEditTemplateAsTeacher` + handler (scoped DI) — teacher owns template; handler có thể chỉ check ownership, service enforce draft-only mutate.
  - [x] Idempotency: không có “create duplicate on retry” — client POST once; server không cần Idempotency-Key cho MVP nếu client đúng pattern; test double POST rapid chỉ khi body khác title (2 records OK) — AC4 intent là **cùng một wizard session** không spam POST: test bằng client state + disabled submit.

- [x] API tests (AC: 2, 4, 5)
  - [x] `TestTemplatesControllerTests` (mở rộng):
    - Create draft success; invalid name; invalid skill; tag limit.
    - Update own draft success; cross-teacher update → 404 hidden.
    - Update Ready template → 409 `templates.notEditable`.
    - Student/unauth POST/PUT → 401/403.
  - [x] `AuthorizationMatrixTests` — matrix rows cho POST/PUT `/api/test-templates`.

- [x] Angular wizard Step 1 (AC: 1–5)
  - [x] Feature `features/test-template-setup/` (hoặc `test-template-wizard/setup/`):
    - Component wizard Step 1 với object IDs UX 01.4 (`create-setup-*`).
    - Stepper 1/4: Setup active; Upload, Answer key, Review pending/disabled.
    - Form: reactive forms; segmented skill control; optional description textarea; tags input (comma hoặc chip, parse max 10).
    - Sidebar draft summary: required checklist, save status (`Đang lưu nháp` / `Đã lưu nháp`).
    - Skill change → cập nhật checklist text (Reading: PDF bắt buộc; Listening: PDF + audio tùy chọn; Speaking: cue/prompt — **chỉ hiển thị**, không validate file ở story này).
  - [x] Routes `app.routes.ts`:
    - `/teacher/library/new/setup` → setup component (create mode).
    - `/teacher/library/:templateId/setup` → setup component (edit mode).
    - Thay `teacher-placeholder` hiện tại.
  - [x] `TestTemplatesApiService` — `createTemplate()`, `updateTemplate()`; map error codes.
  - [x] `test-templates.models.ts` — thêm error messages cho codes mới.
  - [x] Flow create: user nhập form → Lưu nháp/Tiếp tục → POST → lưu `templateId` trong component → các lần save sau PUT.
  - [x] Flow edit: route param → GET detail → populate form → PUT on save.
  - [x] Tiếp tục (valid) → save → navigate `/teacher/library/{templateId}/materials` (placeholder component OK với message Story 2.3).
  - [x] Quay lại → `/teacher/library`.
  - [x] Disable primary/secondary actions khi `isSaving`; prevent double submit.
  - [x] Vitest: validation errors, skill checklist update, save-then-continue uses PUT not second POST, edit mode loads values.

- [x] Library integration (AC: 5)
  - [x] `test-template-library.component`: action "Xem / chỉnh sửa" — nếu `status === draft` → `routerLink` `/teacher/library/{id}/setup`; nếu Ready/Archived → giữ inspect panel (hoặc inspect + nút "Chỉnh sửa" disabled với tooltip).

- [x] Docs + quality
  - [x] `docs/setup/development.md` — smoke: tạo draft từ library → lưu nháp → resume edit.
  - [x] `.\scripts\quality.ps1` pass.

## Dev Notes

### Discovery Results

- `epics_content`: `_bmad-output/planning-artifacts/epics.md` — Story 2.2, FR-4, UX-DR6, NFR-5.
- `architecture_content`: `_bmad-output/planning-artifacts/architecture.md` — `Domain/TestTemplates`, teacher-owned MVP, idempotency AC-DI-03, wizard medium complexity, `features/test-templates`.
- `ux_content`: `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.4-create-test-setup/1.4-create-test-setup.md`
- `prd_content`: `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md` — FR-4 create/edit/save draft.
- Previous story: `2-1-thu-vien-de-list-search-filter-and-template-inspection.md` (done) — TestTemplate read surface, `CanViewTemplateAsTeacher`, library UI, hidden 404 pattern.

### Story Foundation

Story 2.2 là **write slice đầu tiên của Epic 2**: wizard Step 1 (setup) cho Đề gốc. Build trên entity/API read-only từ 2.1; thêm create/update draft và UI wizard.

**Phụ thuộc (done):** Story 2.1 — `TestTemplate` entity, list/detail API, teacher auth, `CanViewTemplateAsTeacher`, library page, demo seeder.

**Không kéo scope:** File upload (2.3), AnswerKey (2.4), mark ready (2.5), duplicate/archive mutations, Homework/Live Exam routes, class/deadline/session fields trên template, autosave debounce phức tạp (manual save + save-before-continue đủ cho MVP).

### Current Codebase State (baseline 0c3e45d — Story 2.1)

| Area | Hiện tại | Thay đổi story 2.2 |
|------|----------|----------------------|
| `TestTemplate` | `Title`, `Skill`, `Description`, no `Tags` | Thêm `Tags`; create/update paths |
| `TestTemplatesController` | GET list + GET by id | + POST create, PUT update draft |
| `ITestTemplateService` | Read only | + create/update draft methods |
| `AuthorizationPolicies` | `CanViewTemplateAsTeacher` | + `CanEditTemplateAsTeacher` |
| `app.routes.ts` | `/teacher/library/new/setup` → placeholder | Real setup component + `:templateId/setup` |
| Library UI | Inspect panel only on "Xem / chỉnh sửa" | Draft → navigate edit setup |

### Architecture Compliance

- **Teacher-owned:** `TeacherId` set từ `ICurrentUserContext` on create; update verifies ownership via `ITemplateAuthorizationService` + policy.
- **Cross-teacher mutate/read** → hidden **404** `templates.notFound` (giữ pattern 2.1).
- **Draft-only edit:** Ready/Archived templates immutable ở setup — `409 templates.notEditable` (state conflict, không phải hidden 404).
- **Controllers → Application → Domain;** Infrastructure implements EF mutations.
- **Cookie auth + XSRF** cho POST/PUT; Angular gửi XSRF header (đã có từ baseline).
- **ProblemDetails** `extensions.code`; tests assert codes only. [AGENTS.md]
- **Không** Class/deadline/session trên `TestTemplate` — thuộc HomeworkAssignment/LiveExamSession (Epic 3). [UX 01.4 Technical Notes]

### UX Reference (01.4 Create Template: Setup)

- Route create: `/teacher/library/new/setup`
- Route resume: `/teacher/library/{templateId}/setup` (suy ra từ UX "Resume draft from Test Library" + pattern materials `{id}` ở 01.5)
- Layout: wizard header + stepper 1–4 | main form | draft summary sidebar | footer
- Object IDs: `create-setup-wizard-header`, `create-setup-form`, `create-setup-draft-summary`, `create-setup-footer-actions`
- Validation:
  - Tên đề: required, 3–120 chars
  - Skill: required (`ERR_SKILL_REQUIRED`)
  - Tags: optional, max 10 (`ERR_TAG_LIMIT`)
- Continue label: "Tiếp tục upload tài liệu" → route materials (placeholder OK)
- Save draft: stays on page, status "Đã lưu nháp"
- **Không** parse PDF content; **không** hỏi lớp/hạn nộp/phiên thi

### Skill → Material Expectations (AC3 — UI sidebar only)

| Skill | Step 2 preview (materials) | Step 3 preview (AnswerKey) |
|-------|---------------------------|---------------------------|
| `reading` | PDF bắt buộc | Answer key bắt buộc |
| `listening` | PDF bắt buộc; audio tùy chọn | Answer key bắt buộc |
| `speaking` | Cue/prompt material | Không AnswerKey auto-grade (manual grading) |

Chỉ hiển thị trong draft summary checklist — validation file thuộc 2.3/2.4.

### Technical Requirements

**API contracts (camelCase JSON):**

```json
// POST /api/test-templates
{ "title": "...", "skill": "reading", "description": "...", "tags": ["midterm", "grade-10"] }

// PUT /api/test-templates/{id}
// same body shape

// Response (create/update/get detail)
{
  "templateId": "uuid",
  "title": "...",
  "skill": "reading",
  "description": "...",
  "tags": [],
  "status": "draft",
  "createdAt": "...",
  "updatedAt": "..."
}
```

**Server validation rules:**

- `title`: trim, length 3–120, required
- `skill`: one of `reading` | `listening` | `speaking` (lowercase, match `TemplateSkill` constants)
- `description`: optional, max 2000 (existing column)
- `tags`: optional, max 10 items, each trimmed non-empty, max 32 chars per tag
- Create always sets `Status = draft`, `CreatedAt`/`UpdatedAt` UTC now
- Update sets `UpdatedAt` only; reject if `Status != draft`

**Idempotency pattern (AC4):**

1. Create mode: component `templateId = null` until first successful POST.
2. After POST, set `templateId`; optionally `router.replaceUrl` to `/teacher/library/{id}/setup` để refresh-safe.
3. Mọi Lưu nháp/Tiếp tục tiếp theo → PUT `/api/test-templates/{id}`.
4. UI: `isSaving` disables buttons; không fire parallel POST.

**Authorization:**

- Reuse `TemplateTeacherAuthorizationHandler` pattern từ 2.1 — thêm requirement `CanEditTemplateAsTeacher`.
- Controller: authorize policy trước mutate; audit deny qua `AuthorizationDenialAuditor`.

### File Structure Requirements

**API (new/modify):**

- `Contracts/TestTemplates/CreateTestTemplateRequest.cs`
- `Contracts/TestTemplates/UpdateTestTemplateRequest.cs`
- `Application/TestTemplates/ITestTemplateService.cs` — extend
- `Infrastructure/TestTemplates/TestTemplateService.cs` — extend
- `Infrastructure/Authorization/Handlers/TemplateTeacherEditRequirement.cs`
- `Infrastructure/Authorization/Handlers/TemplateTeacherEditAuthorizationHandler.cs` (hoặc extend handler hiện có nếu gọn)
- `Controllers/TestTemplatesController.cs` — POST/PUT
- `Program.cs` — register policy handler
- `Infrastructure/Persistence/Migrations/*AddTestTemplateTags*`

**Client (new/modify):**

- `features/test-template-setup/test-template-setup.component.{ts,html,css,spec.ts}`
- `core/test-templates/test-templates-api.service.ts` — extend
- `core/test-templates/test-templates.models.ts` — extend
- `app.routes.ts` — routes + materials placeholder child route optional
- `features/test-template-library/test-template-library.component.ts` — draft edit navigation

### Testing Requirements

- API integration tests với `TestApiFactory` + `AuthTestHelper.SignInTeacherAsync` + `TestTemplatesTestHelper`.
- Assert `extensions.code` trong ProblemDetails, không assert message text.
- Matrix tests cho POST/PUT trong `AuthorizationMatrixTests` (mirror GET rows đã có).
- Angular Vitest: form validation, skill sidebar text, create→PUT flow mock HttpClient.
- Regression: existing 2.1 list/detail tests vẫn pass.

### Anti-Patterns (từ Epic 1 + 2.1 learnings)

- Không leak template cross-teacher (404 not 403) trên read; mutate cross-teacher cùng rule.
- Không JWT/localStorage.
- Không upload/file storage trong story này.
- Không cho sửa setup template Ready — tránh silent regression AnswerKey/materials đã gắn.
- Policy handler **scoped** DI, không Singleton.
- Angular guard ≠ security — API enforce ownership + draft status.
- Không duplicate authorization logic controller vs service — `ITemplateAuthorizationService` là single source.

### Previous Story Intelligence (2.1)

- Files đã tạo: toàn bộ `Domain/TestTemplates/*`, `TestTemplatesController` (GET), library feature, `CanViewTemplateAsTeacher`.
- Review learnings áp dụng: Escape đóng menu, stale fetch guard, matrix tests — pattern tương tự cho wizard save race (ignore stale GET if templateId changed).
- `inspectTemplate` hiện mở panel — story 2.2 tách **draft edit** (navigate wizard) vs **inspect** (Ready/Archived metadata).
- Seeder có 3 templates — verify PUT không break seeded Ready/Archived rows.

### Git Intelligence Summary

- Commit `0c3e45d` — feat: add teacher template library list and inspect (story 2.1): established TestTemplate domain, GET endpoints, Angular library, authorization policy pattern.
- Follow same commit style: `feat: add template setup wizard create/edit draft (story 2.2)`.

### Latest Tech Information

- **.NET 10 / ASP.NET Core 10:** Minimal APIs không dùng — giữ controller pattern. `[ApiController]` automatic 400 cho invalid model state; vẫn map business codes qua `IHiddenResourceResponseFactory` / custom validation results cho stable codes.
- **Angular 22:** Standalone components, signals (đã dùng ở library), Reactive Forms (pattern từ `teacher-login`). `input()` / route params via `ActivatedRoute` snapshot hoặc `toSignal`.
- **EF Core 10:** JSON column có thể dùng `HasConversion` cho `List<string>` Tags — hoặc `string` JSON đơn giản cho MVP.

### Project Context Reference

- [AGENTS.md] — stack boundaries, ProblemDetails, no JWT, quality script before feature work.
- [docs/setup/development.md] — smoke flows, demo teacher credentials.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Story 2.2]
- [Source: `_bmad-output/C-UX-Scenarios/.../1.4-create-test-setup.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — FR-4, idempotency AC-DI-03]
- [Source: `2-1-thu-vien-de-list-search-filter-and-template-inspection.md` — read surface patterns]

## Dev Agent Record

### Agent Model Used

Composer

### Debug Log References

### Completion Notes List

- Added `TagsJson` column + migration `AddTestTemplateTags`; create/update draft API with validation codes (`templates.nameRequired`, `templates.skillInvalid`, `templates.tagLimit`, `templates.notEditable`).
- Policy `CanEditTemplateAsTeacher` + edit authorization handler; POST/PUT endpoints with XSRF and teacher scope.
- Angular wizard Step 1 at `/teacher/library/new/setup` and `/teacher/library/:templateId/setup` with skill checklist sidebar, save/continue flow (POST once then PUT), materials placeholder route.
- Library: draft "Xem / chỉnh sửa" navigates to setup; Ready/Archived keeps inspect panel.
- Tests: 79 API + 51 client tests pass; quality gate green.

### File List

- src/EnglishTestWeb.Api/Domain/TestTemplates/TestTemplate.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/TestTemplateConfiguration.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/*AddTestTemplateTags*
- src/EnglishTestWeb.Api/Application/TestTemplates/TestTemplateSetupValidation.cs
- src/EnglishTestWeb.Api/Application/TestTemplates/TestTemplateTagsSerializer.cs
- src/EnglishTestWeb.Api/Application/TestTemplates/ITestTemplateService.cs
- src/EnglishTestWeb.Api/Infrastructure/TestTemplates/TestTemplateService.cs
- src/EnglishTestWeb.Api/Contracts/TestTemplates/CreateTestTemplateRequest.cs
- src/EnglishTestWeb.Api/Contracts/TestTemplates/UpdateTestTemplateRequest.cs
- src/EnglishTestWeb.Api/Contracts/TestTemplates/TestTemplateSetupResponse.cs
- src/EnglishTestWeb.Api/Contracts/TestTemplates/TestTemplateDetailResponse.cs
- src/EnglishTestWeb.Api/Controllers/TestTemplatesController.cs
- src/EnglishTestWeb.Api/Infrastructure/Authorization/Policies/AuthorizationPolicies.cs
- src/EnglishTestWeb.Api/Infrastructure/Authorization/Handlers/TemplateTeacherEditRequirement.cs
- src/EnglishTestWeb.Api/Infrastructure/Authorization/Handlers/TemplateTeacherEditAuthorizationHandler.cs
- src/EnglishTestWeb.Api/Program.cs
- tests/EnglishTestWeb.Api.Tests/Auth/AuthTestHelper.cs
- tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplatesTestHelper.cs
- tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplatesControllerTests.cs
- tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs
- src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates.models.ts
- src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates-api.service.ts
- src/EnglishTestWeb.Client/src/app/features/test-template-setup/test-template-setup.component.ts
- src/EnglishTestWeb.Client/src/app/features/test-template-setup/test-template-setup.component.html
- src/EnglishTestWeb.Client/src/app/features/test-template-setup/test-template-setup.component.css
- src/EnglishTestWeb.Client/src/app/features/test-template-setup/test-template-setup.component.spec.ts
- src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.ts
- src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.spec.ts
- src/EnglishTestWeb.Client/src/app/app.routes.ts
- docs/setup/development.md

### Review Findings

- [x] [Review][Defer] Tags chip UI vs comma-only input — **Resolved:** comma-only đủ MVP; chip UI defer sang polish Epic 2.

- [x] [Review][Patch] Race double-click POST và Continue khi save đang chạy [test-template-setup.component.ts] — fixed: `saveInFlight` promise coalescing
- [x] [Review][Patch] Edit mode không reload khi route `templateId` thay đổi [test-template-setup.component.ts] — fixed: subscribe `paramMap`
- [x] [Review][Patch] Client thiếu map `templates.descriptionTooLong` [test-templates.models.ts] — fixed
- [x] [Review][Patch] Server title >120 trả `templates.nameRequired` — fixed: `templates.titleTooLong`
- [x] [Review][Patch] `templates.tagLimit` dùng chung cho per-tag length — fixed: `templates.tagTooLong`
- [x] [Review][Patch] Client `parseTagsInput` không dedupe case-insensitive — fixed
- [x] [Review][Patch] Thiếu validate serialized TagsJson ≤500 — fixed: `ValidateSerializedLength`
- [x] [Review][Patch] Null element trong tags array NRE — fixed: skip null
- [x] [Review][Patch] DbUpdateException không map ProblemDetails — fixed: catch → `templates.tagLimit`
- [x] [Review][Patch] Save draft invalid form vẫn "Đã lưu nháp" — fixed: reset saveState to idle
- [x] [Review][Patch] Auth handler thiếu CancellationToken — fixed: `RequestAborted`
- [x] [Review][Patch] Test `Create_WithEmptyTitle` misnamed — fixed + split tests
- [x] [Review][Patch] Thiếu API tests validation paths — fixed
- [x] [Review][Patch] Thiếu Vitest continue PUT + ready blocking — fixed
- [x] [Review][Patch] Auth matrix thiếu positive POST/PUT — fixed

- [x] [Review][Defer] Concurrent PUT cùng draft last-write-wins [TestTemplateService.cs:312] — deferred, không có concurrency AC trong story MVP
- [x] [Review][Defer] TagsJson corrupt deserialize silently → `[]` [TestTemplateTagsSerializer.cs:23] — deferred, edge case hiếm; MVP chấp nhận

### Re-review Findings (2026-06-10)

- [x] [Review][Patch] Skill checklist sidebar không cập nhật khi đổi skill (AC3) — fixed: `toSignal(skill.valueChanges)`
- [x] [Review][Patch] `loadTemplate` stale-response race — fixed: `loadRequestId` guard
- [x] [Review][Patch] `replaceUrl` sau POST trigger GET thừa — fixed: `skipLoadTemplateId`
- [x] [Review][Patch] `saveInFlight` bỏ qua payload mới — fixed: `pendingSavePayload` chain
- [x] [Review][Patch] Message misleading cho TagsJson overflow — fixed: `templates.tagsStorageLimit`

- [x] [Review][Defer] `DbUpdateException` luôn map `templates.tagLimit` [TestTemplateService.cs] — deferred, MVP catch-all acceptable

### Re-review Findings — Pass 3 (2026-06-10)

- [x] [Review][Patch] Save chain resolve sớm — fixed: `saveChainPromise` bọc toàn bộ `runSaveChain` loop
- [x] [Review][Patch] `resetCreateMode` không bump `loadRequestId` — fixed: `loadRequestId++` on reset

## Change Log

- 2026-06-10: Story 2.2 created from epics + UX 01.4 + architecture + 2.1 learnings; ready-for-dev.
- 2026-06-10: Story 2.2 implemented — template setup wizard create/edit draft, API POST/PUT, tests; status → review.
- 2026-06-10: Code review — 1 decision-needed, 14 patch, 2 defer, 4 dismissed.
- 2026-06-10: Code review fixes applied — all patches resolved; status → done.
- 2026-06-10: Re-review — 4 patch, 1 defer; status → in-progress.
- 2026-06-10: Re-review patches applied; quality gate green; status → done.
- 2026-06-10: Re-review pass 3 — Acceptance CLEAN; 2 patch remaining; status → in-progress.
- 2026-06-10: Re-review pass 3 patches applied; status → done.
