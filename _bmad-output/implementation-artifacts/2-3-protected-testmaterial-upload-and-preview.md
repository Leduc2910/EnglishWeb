---
baseline_commit: 0c3e45d4ad9159101d616db0c4f10ed3a46aed84
---

# Story 2.3: Protected TestMaterial Upload And Preview

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

Là giáo viên,
tôi muốn upload PDF bắt buộc và audio/cue tùy chọn kèm tiến trình và thử lại,
để Đề gốc có tài liệu nguồn an toàn mà không cần nhập lại nội dung PDF.

## Acceptance Criteria

1. **Given** một draft Reading `TestTemplate` đã tồn tại (sau bước Setup)
   **When** giáo viên mở `/teacher/library/{templateId}/materials`
   **Then** wizard hiển thị Bước 2/4 với dropzone/file picker PDF, checklist yêu cầu theo skill, khu vực file card, trạng thái upload, sidebar summary, và footer (Quay lại | Lưu nháp | Tiếp tục).

2. **Given** giáo viên chọn file không phải PDF cho slot PDF bắt buộc (Reading/Listening)
   **When** client và server validation chạy
   **Then** upload bị từ chối với `ERR_FILE_TYPE` (server: `files.invalidType`)
   **And** draft template vẫn chỉnh sửa được.

3. **Given** giáo viên upload file hợp lệ
   **When** upload đang chạy
   **Then** progress hiển thị (percent hoặc indeterminate + label)
   **And** nút Tiếp tục bị disabled cho đến khi upload hoàn tất.

4. **Given** upload thành công
   **When** file card hiển thị
   **Then** hiện original filename, size, trạng thái success, action preview, remove/replace, và `fileId` metadata
   **And** bytes vật lý nằm ngoài `wwwroot` (qua `IFileStorage` + storage key opaque).

5. **Given** upload thất bại hoặc giáo viên thay file
   **When** retry hoặc replace
   **Then** trạng thái draft template được giữ (không mất setup/metadata template)
   **And** metadata DB phản ánh material **active** hiện tại (material cũ soft-deactivated/archived, không orphan active duplicate cho cùng role).

6. **Given** giáo viên preview PDF hoặc audio material
   **When** request preview/stream
   **Then** nội dung trả qua authorized endpoint (`FilesController`) với `Accept-Ranges` / range support khi applicable
   **And** cross-teacher hoặc unauth → hidden 404 / 401.

**Skill-specific material rules (FR-5, checklist 2.2):**

| Skill | Slots | Continue enabled when |
|-------|-------|----------------------|
| `reading` | 1× PDF (`role=pdf`) | PDF active uploaded |
| `listening` | 1× PDF (`role=pdf`) + 1× audio optional (`role=audio`) | PDF active uploaded (audio optional) |
| `speaking` | 1× cue/prompt (`role=cue`, PDF hoặc image MVP: **PDF only** cho story này) | Cue PDF active uploaded |

**Implementation Note:** Story này **giới thiệu `TestMaterial` + protected file metadata** và wizard Step 2. Không implement AnswerKey (2.4), mark ready (2.5), student playback (Epic 4/5), hay physical orphan file GC.

## Tasks / Subtasks

- [x] Domain + persistence (AC: 4, 5, Implementation Note)
  - [x] `Domain/Files/StoredFile.cs` — `Id`, `StorageKey`, `OriginalFileName`, `ContentType`, `SizeBytes`, `ChecksumSha256?`, `OwnerUserId`, `Status` (active/archived), `CreatedAt`, `UpdatedAt`.
  - [x] `Domain/TestTemplates/TestMaterial.cs` — `Id`, `TemplateId`, `StoredFileId`, `Role` (`pdf` | `audio` | `cue`), `IsActive`, `CreatedAt`, `ArchivedAt?`.
  - [x] EF configurations + migration `AddTestMaterialsAndStoredFiles`; FK + filtered unique index per active role.
  - [x] `EnglishTestWebDbContext` — `DbSet<StoredFile>`, `DbSet<TestMaterial>`.

- [x] Storage abstraction extend (AC: 4, 6)
  - [x] `IFileStorage` + `OpenReadAsync`, `DeleteAsync`
  - [x] `LocalProtectedFileStorage` read/delete + storage key validation
  - [x] SHA256 checksum inline trong `TestTemplateMaterialService` upload

- [x] Application + API materials (AC: 2–5, Implementation Note)
  - [x] `ITestTemplateMaterialService` + `TestTemplateMaterialService`
  - [x] `IProtectedFileService` + `ProtectedFileService`
  - [x] `MaterialUploadValidation` — PDF/audio MIME, size limits 25MB/50MB
  - [x] `TestMaterialResponse`, `TestMaterialListResponse`
  - [x] `TestTemplateMaterialsController` — GET/POST/DELETE materials
  - [x] `FilesController` — GET content với range processing
  - [x] ProblemDetails codes: `files.*`, `materials.*`, `templates.notEditable`
  - [x] Replace flow archives prior active material + stored file metadata
  - [x] Reuse `CanEditTemplateAsTeacher` / `CanViewTemplateAsTeacher`

- [x] API tests (AC: 2, 4, 5, 6)
  - [x] `TestTemplateMaterialsControllerTests`
  - [x] `ProtectedFileAccessTests` (200, 206 range, hidden 404)
  - [x] `AuthorizationMatrixTests` materials + file content rows

- [x] Angular wizard Step 2 (AC: 1–6)
  - [x] `features/test-template-materials/` — stepper 2/4, skill slots, upload progress, preview modal
  - [x] `app.routes.ts` — materials component + answer-key placeholder
  - [x] API services + material models/error mapping
  - [x] Vitest materials component specs

- [x] Docs + quality
  - [x] `docs/setup/development.md` — materials smoke
  - [x] `.\scripts\quality.ps1` pass (100 API + 58 client tests)

### Review Findings

- [x] [Review][Patch] Orphan physical file when DB save/commit fails after successful write [TestTemplateMaterialService.cs:185]
- [x] [Review][Patch] `ListMaterialsAsync` incorrectly requires draft status — read should use view access only [TestTemplateMaterialService.cs:26]
- [x] [Review][Patch] Client/server file-type validation mismatch (PDF/audio MIME); server rejects `application/octet-stream` PDFs [MaterialUploadValidation.cs:33, test-templates.models.ts:173]
- [x] [Review][Patch] Concurrent same-role upload can hit unique index → 500 + orphan file [TestTemplateMaterialService.cs:91]
- [x] [Review][Patch] Missing null guard on multipart `role` → possible 500 [TestTemplateMaterialsController.cs]
- [x] [Review][Patch] `mapMaterialApiError` uses localized string as sentinel [test-templates.models.ts:115]
- [x] [Review][Patch] Preview button hard-coded "Xem nhanh PDF" and iframe for audio materials [test-template-materials.component.html]
- [x] [Review][Patch] Speaking skill shows Reading-specific `ERR_PDF_REQUIRED` on Continue [test-template-materials.component.ts:206]
- [x] [Review][Patch] `refreshMaterials` / `onSaveDraft` unhandled errors — no user feedback [test-template-materials.component.ts:274]
- [x] [Review][Patch] Dropzone accepts files during in-progress upload with no feedback [test-template-materials.component.ts:291]
- [x] [Review][Patch] `AuthorizationMatrixTests` missing cross-teacher `GET /api/files/{id}/content` row [AuthorizationMatrixTests.cs]
- [x] [Review][Defer] Triple ownership DB round-trips per materials request [TestTemplateMaterialsController.cs] — deferred, pre-existing pattern
- [x] [Review][Defer] Edit auth handler does not enforce draft status; service is sole gate [TemplateTeacherEditAuthorizationHandler.cs] — deferred, pre-existing
- [x] [Review][Defer] Archived physical files never deleted from disk [TestTemplateMaterialService.cs] — deferred, pre-existing
- [x] [Review][Defer] File access checks template owner only, not `StoredFile.OwnerUserId` [ProtectedFileService.cs] — deferred, pre-existing
- [x] [Review][Defer] Non-seekable upload stream skips pre-write size check [TestTemplateMaterialService.cs:75] — deferred, pre-existing

## Dev Notes

### Discovery Results

- `epics_content`: `_bmad-output/planning-artifacts/epics.md` — Story 2.3, FR-5, UX-DR7, NFR-6, FR-20.
- `architecture_content`: `_bmad-output/planning-artifacts/architecture.md` — `IFileStorage`, `FilesController`, metadata in SQL, range streaming, no public URLs, `Domain/Files`, `Application/Files`.
- `ux_content`: `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.5-create-test-upload-materials/1.5-create-test-upload-materials.md`
- `prd_content`: `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md` — FR-5 upload TestMaterial, retry/replace, progress.
- Previous story: `2-2-create-edit-and-save-draft-template-setup.md` (done) — wizard Step 1, POST/PUT draft, `CanEditTemplateAsTeacher`, route `/teacher/library/{templateId}/materials` placeholder.

### Story Foundation

Story 2.3 là **protected file vertical slice đầu tiên**: metadata DB + upload + authorized preview stream cho wizard Step 2. Build trên `TestTemplate` draft write surface từ 2.2; **phụ thuộc** template đã có `templateId` (không upload trên `/library/new/materials` — UX 01.5 route `new` được supersede bởi flow 2.2: setup POST trước, rồi navigate `/{id}/materials`).

**Phụ thuộc (done / in repo):** Story 2.1 read surface; Story 2.2 create/edit draft setup, policies, wizard Step 1, materials placeholder route.

**Không kéo scope:** AnswerKey (2.4), mark-ready material validation gate (2.5), student attempt PDF viewer (4.x), Speaking student upload (5.x), shared `upload-queue` package extraction (inline upload OK), rate limiting middleware (architecture defers — không block story), physical orphan file sweeper, antivirus pipeline.

### Current Codebase State (baseline `0c3e45d` + story 2.2 working tree)

| Area | Hiện tại | Thay đổi story 2.3 |
|------|----------|---------------------|
| `IFileStorage` | Chỉ `WriteAsync` | + `OpenReadAsync` (+ optional `DeleteAsync`) |
| `FilesController` | Không tồn tại | Mới — authorized content stream + range |
| `TestTemplate` | Setup fields only, no materials nav | Materials collection via `TestMaterial` |
| `TestTemplatesController` | GET, POST, PUT setup | + materials sub-routes hoặc sibling controller |
| `app.routes.ts` | `library/:templateId/materials` → placeholder | Real materials wizard component |
| DB | Chỉ `TestTemplates` | + `StoredFiles`, `TestMaterials` |

### Architecture Compliance

- **Protected storage:** Files outside `wwwroot`; `StorageKey` opaque GUID; original filename display-only. [architecture.md File/Media]
- **Metadata in SQL:** owner, content type, size, checksum, status, timestamps. [architecture.md]
- **Every stream re-authorizes:** `FilesController` checks teacher owns template linked to file before bytes. [architecture.md]
- **Range support:** `Accept-Ranges: bytes` + `206` for PDF preview và future audio. [AC6, architecture.md]
- **Hidden 404:** cross-teacher file/template access → `files.notFound` / `templates.notFound`. [pattern 2.1/2.2]
- **Draft-only mutate:** upload/delete materials chỉ khi `TestTemplate.Status == draft`; Ready → `409 templates.notEditable`.
- **Controllers → Application → Domain;** `IFileStorage` chỉ Infrastructure.
- **Cookie auth + XSRF** cho POST/DELETE upload; GET content uses cookie session (no bearer).
- **ProblemDetails** stable `extensions.code`; tests assert codes not message text. [AGENTS.md]
- **No PDF parsing** — upload only. [UX-DR7, PRD]
- **Policy handler scoped DI** — nếu thêm `CanAccessFileAsTeacher`, register scoped. [Epic 1.4 note]

### UX Reference (01.5 Create Template: Upload Materials)

- Route thực tế: `/teacher/library/{templateId}/materials` (resume + continue from setup)
- Layout: wizard header stepper 2/4 | upload zone + file card | material summary sidebar | footer
- Object IDs: `create-materials-wizard-header`, `create-materials-dropzone`, `create-materials-file-card`, `create-materials-preview-link`, `create-materials-summary`, `create-materials-footer-actions`
- States: Empty, Uploading, Uploaded, Error
- Validation client:
  - `ERR_PDF_REQUIRED`, `ERR_FILE_TYPE`, `ERR_FILE_SIZE`, `ERR_UPLOAD_INCOMPLETE`
- Continue label: "Tiếp tục nhập answer key" → answer-key route (placeholder)
- **Không** parse PDF; preview metadata optional

### Technical Requirements

**Material roles (server enum constants):**

```
pdf   — required for reading & listening
audio — optional for listening only
cue   — required for speaking (PDF prompt/cue card)
```

**Upload API (multipart):**

```
POST /api/test-templates/{templateId}/materials
Content-Type: multipart/form-data
Fields: role=pdf|audio|cue, file=<binary>

Response 201:
{
  "materialId": "uuid",
  "fileId": "uuid",
  "role": "pdf",
  "originalFileName": "de-reading.pdf",
  "sizeBytes": 123456,
  "contentType": "application/pdf",
  "uploadedAt": "..."
}
```

**List materials:**

```
GET /api/test-templates/{templateId}/materials
→ { "items": [ TestMaterialResponse, ... ] }  // active only
```

**Stream content:**

```
GET /api/files/{fileId}/content
Headers: Range: bytes=0-1023  (optional)
→ 200 or 206, Content-Type, Accept-Ranges: bytes
```

**Replace semantics (AC5):**

1. Trong transaction: set `TestMaterial.IsActive = false`, `ArchivedAt = now` cho row cũ cùng role; set `StoredFile.Status = archived`.
2. Write new bytes via `IFileStorage.WriteAsync`; insert new `StoredFile` + `TestMaterial` active.
3. Template `UpdatedAt` bump.

**Client upload progress:**

```typescript
this.http.post(url, formData, {
  reportProgress: true,
  observe: 'events',
  withCredentials: true,
  headers: xsrfHeaders,
});
// map HttpEventType.UploadProgress → percent
```

**Idempotency:** Không cần Idempotency-Key cho upload MVP; UI disable dropzone khi đang upload; replace là explicit user action.

### File Structure Requirements

**API (new):**

- `Domain/Files/StoredFile.cs`, `StoredFileStatuses.cs`
- `Domain/TestTemplates/TestMaterial.cs`, `MaterialRoles.cs`
- `Infrastructure/Persistence/Configurations/StoredFileConfiguration.cs`
- `Infrastructure/Persistence/Configurations/TestMaterialConfiguration.cs`
- `Infrastructure/Persistence/Migrations/*AddTestMaterialsAndStoredFiles*`
- `Application/Files/IProtectedFileService.cs`, `ProtectedFileService.cs`
- `Application/TestTemplates/ITestTemplateMaterialService.cs`, `TestTemplateMaterialService.cs`
- `Contracts/Files/*`, `Contracts/TestTemplates/TestMaterialResponse.cs`
- `Controllers/FilesController.cs`
- `Controllers/TestTemplateMaterialsController.cs` (hoặc extend `TestTemplatesController`)
- `Infrastructure/Storage/LocalProtectedFileStorage.cs` — extend read
- `Application/Files/IFileStorage.cs` — extend

**API (modify):**

- `Infrastructure/Persistence/EnglishTestWebDbContext.cs`
- `Program.cs` — register new services
- `Infrastructure/Authorization` — file access helper nếu cần

**Client (new):**

- `features/test-template-materials/test-template-materials.component.{ts,html,css,spec.ts}`
- `core/files/files-api.service.ts`

**Client (modify):**

- `app.routes.ts`
- `core/test-templates/test-templates-api.service.ts`
- `core/test-templates/test-templates.models.ts`

**Tests (new/extend):**

- `tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplateMaterialsControllerTests.cs`
- `tests/EnglishTestWeb.Api.Tests/Files/ProtectedFileAccessTests.cs`
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs`

### Testing Requirements

- API: `TestApiFactory` + `AuthTestHelper` + `TestTemplatesTestHelper`; tạo draft template, upload minimal PDF bytes (`%PDF-1.4` header stub).
- Assert `extensions.code` trong ProblemDetails.
- Range test: `HttpRequestMessage` với `Range` header → status 206, content length matches range.
- Angular Vitest: mock `HttpClient` events cho progress; verify continue gating.
- Regression: 2.1 list, 2.2 setup tests vẫn pass.

### Anti-Patterns (từ Epic 1 + 2.1 + 2.2)

- **Không** expose `/wwwroot/uploads/...` hoặc storage key trong URL client.
- **Không** JWT/localStorage.
- **Không** skip server MIME validation (client-only insufficient).
- **Không** cho upload khi template Ready/Archived.
- **Không** leak file existence cross-teacher (404 not 403).
- **Không** parse PDF content.
- Policy handler **scoped** DI.
- **Không** duplicate authorization — centralize file→template ownership query trong `IProtectedFileService`.

### Previous Story Intelligence (2.2)

- Wizard Step 1 đã navigate `Tiếp tục` → `/teacher/library/{id}/materials` sau POST+PUT.
- `saveInFlight` / `loadRequestId` stale guards — áp dụng tương tự cho materials load + parallel upload.
- Error code mapping pattern trong `test-templates.models.ts` — mở rộng cho `files.*` / `materials.*`.
- Review defer: concurrent PUT last-write-wins — upload replace nên dùng transaction, không rely client-only.
- Materials placeholder route đã có `data.title` — thay bằng component thật, giữ `teacherGuard`.

### Git Intelligence Summary

- Commit `0c3e45d` — story 2.1: TestTemplate read, library UI, `CanViewTemplateAsTeacher`.
- Story 2.2 implemented in working tree (chưa commit tại baseline): setup POST/PUT, wizard Step 1, `CanEditTemplateAsTeacher`, materials placeholder.
- Follow commit style: `feat: add protected test material upload and preview (story 2.3)`.

### Latest Tech Information

- **ASP.NET Core 10:** `Results.File(stream, contentType, enableRangeProcessing: true)` hoặc manual `FileStreamResult` + `IHttpRangeHelper` — prefer built-in range processing on `PhysicalFile`/`Stream` results where possible; for custom storage stream, implement range read on `OpenReadAsync` seekable stream.
- **Multipart limits:** Configure `FormOptions.MultipartBodyLengthLimit` ≥ max audio size; align Kestrel `MaxRequestBodySize` if needed in `Program.cs`.
- **Angular 22:** `HttpClient` upload progress via `reportProgress: true`; blob preview via `URL.createObjectURL` — revoke on destroy.
- **EF Core 10:** Filtered unique index `HasIndex(...).IsUnique().HasFilter("[IsActive] = 1")` for one active material per role.

### Project Context Reference

- [AGENTS.md] — stack boundaries, ProblemDetails, no JWT, `IFileStorage`, quality script.
- [docs/setup/development.md] — demo teacher, smoke flows.
- [docs/deploy/storage-and-data-protection.md] — protected storage paths.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Story 2.3]
- [Source: `_bmad-output/C-UX-Scenarios/.../1.5-create-test-upload-materials.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — File/Media, FilesController]
- [Source: `2-2-create-edit-and-save-draft-template-setup.md` — wizard flow, skill checklist]

## Dev Agent Record

### Agent Model Used

Composer

### Debug Log References

### Completion Notes List

- Added `StoredFile` + `TestMaterial` domain, migration `AddTestMaterialsAndStoredFiles`, filtered unique index per active role.
- Extended `IFileStorage` with `OpenReadAsync`/`DeleteAsync`; upload persists SHA256 checksum.
- API: materials CRUD (`TestTemplateMaterialsController`), authorized file stream (`FilesController`) with range support.
- Angular wizard Step 2 at `/teacher/library/{id}/materials` with progress upload, preview modal, skill-aware slots.
- Tests: 11 new API tests; 5 Vitest specs; quality gate green.

### File List

- src/EnglishTestWeb.Api/Domain/Files/StoredFile.cs
- src/EnglishTestWeb.Api/Domain/Files/StoredFileStatuses.cs
- src/EnglishTestWeb.Api/Domain/TestTemplates/TestMaterial.cs
- src/EnglishTestWeb.Api/Domain/TestTemplates/MaterialRoles.cs
- src/EnglishTestWeb.Api/Application/Files/IFileStorage.cs
- src/EnglishTestWeb.Api/Application/Files/IProtectedFileService.cs
- src/EnglishTestWeb.Api/Application/TestTemplates/ITestTemplateMaterialService.cs
- src/EnglishTestWeb.Api/Application/TestTemplates/MaterialUploadValidation.cs
- src/EnglishTestWeb.Api/Contracts/TestTemplates/TestMaterialResponse.cs
- src/EnglishTestWeb.Api/Controllers/TestTemplateMaterialsController.cs
- src/EnglishTestWeb.Api/Controllers/FilesController.cs
- src/EnglishTestWeb.Api/Infrastructure/Files/ProtectedFileService.cs
- src/EnglishTestWeb.Api/Infrastructure/TestTemplates/TestTemplateMaterialService.cs
- src/EnglishTestWeb.Api/Infrastructure/Storage/LocalProtectedFileStorage.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/StoredFileConfiguration.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/TestMaterialConfiguration.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/20260610084856_AddTestMaterialsAndStoredFiles.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/20260610084856_AddTestMaterialsAndStoredFiles.Designer.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/EnglishTestWebDbContextModelSnapshot.cs
- src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs
- src/EnglishTestWeb.Api/Program.cs
- src/EnglishTestWeb.Client/src/app/core/files/files-api.service.ts
- src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates-api.service.ts
- src/EnglishTestWeb.Client/src/app/core/test-templates/test-templates.models.ts
- src/EnglishTestWeb.Client/src/app/features/test-template-materials/test-template-materials.component.ts
- src/EnglishTestWeb.Client/src/app/features/test-template-materials/test-template-materials.component.html
- src/EnglishTestWeb.Client/src/app/features/test-template-materials/test-template-materials.component.css
- src/EnglishTestWeb.Client/src/app/features/test-template-materials/test-template-materials.component.spec.ts
- src/EnglishTestWeb.Client/src/app/app.routes.ts
- tests/EnglishTestWeb.Api.Tests/Auth/AuthTestHelper.cs
- tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplateMaterialsTestHelper.cs
- tests/EnglishTestWeb.Api.Tests/TestTemplates/TestTemplateMaterialsControllerTests.cs
- tests/EnglishTestWeb.Api.Tests/Files/ProtectedFileAccessTests.cs
- tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs
- docs/setup/development.md

## Change Log

- 2026-06-10: Story 2.3 created from epics + UX 01.5 + architecture + 2.2 learnings; ready-for-dev.
- 2026-06-10: Story 2.3 implemented — protected TestMaterial upload/preview, API + Angular wizard Step 2; status → review.
- 2026-06-10: Code review — 11 patch findings fixed; status → done.
