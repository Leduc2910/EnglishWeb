---
baseline_commit: 87c6f9f
---

# Story 6.7: Blocking Error And Edge Case Test Coverage

Status: done

## Story

Là product owner,
tôi muốn có automated coverage cho các MVP blocking errors và edge cases,
để các failure modes đã biết trong TS-001 được xử lý trước khi sign-off.

## Acceptance Criteria

1. **Given** blocking error tests chạy
   **When** invalid class code, not-in-class login, missing setup fields, invalid upload, incomplete AnswerKey, invalid Speaking file, invalid score, expired Homework, hoặc unopened Live Exam bị trigger
   **Then** mỗi case produce expected recoverable error và không corrupt data.

2. **Given** draft và autosave edge tests chạy
   **When** student reload sau khi nhập answers hoặc submit với missing answers
   **Then** saved/local answers restore where technically feasible
   **And** missing-answer warning xuất hiện trước final submit.

3. **Given** duplicate action edge tests chạy
   **When** Mark Ready, Create Session, final submit, hoặc grading save bị double-clicked hoặc retried
   **Then** only one state transition hoặc record được produce.

4. **Given** results edge tests chạy
   **When** filters match no rows hoặc Speaking file is missing
   **Then** UI hiển thị expected empty/recoverable state mà không mất grading context.

## Tasks / Subtasks

- [x] Task 1: Thêm seed helpers cho error/edge scenarios (AC: 1, 2, 3, 4)
  - [x] 1.1 Trong `tests/EnglishTestWeb.E2E/fixtures/seed.ts`, thêm `createExpiredHomeworkAssignment(api, xsrfToken, templateId, classId)` — giống `createHomeworkAssignment` nhưng `deadlineAt = new Date(Date.now() - 60_000).toISOString()` (1 phút trong quá khứ)
  - [x] 1.2 Thêm `openLiveExamSession(api, xsrfToken, sessionId)` — POST `/api/live-exam-sessions/{id}/open` với `X-XSRF-TOKEN`
  - [x] 1.3 Thêm `createReadingAttempt(api, xsrfToken, homeworkAssignmentId)` — POST `/api/submissions` với `{ homeworkAssignmentId, liveExamSessionId: null }`, trả về `submissionId`
  - [x] 1.4 Thêm `saveAnswerDraft(api, xsrfToken, submissionId, answers)` — PUT `/api/submissions/{id}/answers` với array answers; dùng cho EDGE-002 setup
  - [x] 1.5 Thêm `seedExpiredHomeworkChain(api)` — composite helper: loginTeacher → getClassId → createReadyReadingTemplate → createExpiredHomeworkAssignment → return `{ templateId, homeworkId }`
  - [x] 1.6 Thêm `seedNotSubmittedReadingChain(api)` — composite: loginTeacher → getClassId → template → homework → loginStudent → createReadingAttempt → saveAnswerDraft(partial answers) → return `{ submissionId }`

- [x] Task 2: Extend POMs với error/edge state methods (AC: 1, 2, 3, 4)
  - [x] 2.1 `student-class-entry.page.ts`: thêm `getErrorMessage()` → trả về locator của error element `#student-class-entry-error-alert`
  - [x] 2.2 `create-template.page.ts`: thêm `getSetupNameError()`, `uploadInvalidFileForPdf()`, `getUploadError()`, `getAnswerKeyMissingCount()`, `getAnswerKeyErrorList()`, `getMarkReadyButton()`
  - [x] 2.3 `attempt-workspace.page.ts`: thêm `getMissingAnswerWarningModal()`, `getMissingAnswerCount()`, `getBackFromModalButton()`
  - [x] 2.4 `speaking-submission.page.ts`: thêm `getNoFileHint()`, `getSubmitError()`, `getClosedNotice()`, `getFinalSubmitButton()`
  - [x] 2.5 `results-grading.page.ts`: thêm `getEmptyState()`, `getClearFiltersButton()`, `fillStudentSearch()`, `getScoreInput()`, `getGradeError()`, `getMissingFileError()`, `getSaveButton()`

- [x] Task 3: AC1 — Access blocking errors ERR-001, ERR-002 (AC: 1)
  - [x] 3.1 Tạo `tests/EnglishTestWeb.E2E/flows/error-blocking/err-001-002-access.spec.ts`
  - [x] 3.2 ERR-001: `Student enters invalid class code → error message visible, no class data exposed`
    - Student navigates to `/class`
    - Fills code `INVALIDXXX` và submit
    - Expect: error message chứa text "Không tìm thấy lớp" hoặc tương đương
    - Expect: class card KHÔNG visible
  - [x] 3.3 ERR-002: `Student not a member of selected class → blocked after login`
    - **Approach**: Teacher tạo một class mới qua API (nếu endpoint có — xem note trong Dev Notes), lấy class code của class đó. Student (seeded ENG7A member) thử login với class code mới đó.
    - **Fallback approach** nếu không tạo được class mới: Dùng API POST `/api/auth/student/login` trực tiếp với `classCode` của một class student không thuộc, expect HTTP 4xx và ProblemDetails code `classes.notMember` hoặc tương đương.
    - Expect: blocked với Vietnamese error "Tài khoản này chưa thuộc lớp đã chọn"

- [x] Task 4: AC1 — Template authoring blocking errors ERR-003, ERR-004, ERR-005 (AC: 1)
  - [x] 4.1 Tạo `tests/EnglishTestWeb.E2E/flows/error-blocking/err-003-005-template.spec.ts`
  - [x] 4.2 ERR-003: `Teacher submits setup without template name → inline error`
    - Teacher login → navigate `/teacher/library/new/setup`
    - Click Save/Continue mà không nhập name
    - Expect: field error visible (chứa text tương đương `ERR_TEMPLATE_NAME_REQUIRED`)
    - Expect: không navigate sang bước 2
  - [x] 4.3 ERR-004: `Teacher uploads non-PDF for Reading PDF requirement → rejected`
    - Teacher login → create draft Reading template qua API → navigate `/teacher/library/{id}/materials`
    - Upload file `.txt` hoặc `.jpg` vào PDF dropzone
    - Expect: upload error visible (chứa text tương đương `ERR_FILE_TYPE` hoặc "Chỉ hỗ trợ file PDF")
    - Expect: draft vẫn editable (không bị corrupt)
  - [x] 4.4 ERR-005: `Teacher tries to Continue from AnswerKey with incomplete rows → blocked`
    - Teacher login → create draft template với PDF qua API → navigate `/teacher/library/{id}/answer-key`
    - Set questionCount = 3 nhưng chỉ điền 2 answers (bỏ câu 3)
    - Click Continue
    - Expect: validation error hiển thị câu số 3 chưa có đáp án (text tương đương `ERR_ANSWER_MISSING` hoặc "Câu 3 chưa có đáp án")
    - Expect: không navigate sang review step

- [x] Task 5: AC1 — Submission/assignment blocking errors ERR-006, ERR-007, ERR-008, ERR-009 (AC: 1)
  - [x] 5.1 Tạo `tests/EnglishTestWeb.E2E/flows/error-blocking/err-006-009-submission.spec.ts`
  - [x] 5.2 ERR-006: `Student clicks submit Speaking without file → blocked`
    - Fixture: create ready Speaking template + homework qua API
    - Student login → navigate đến Speaking page `/student/speaking/{id}`
    - Click "Nộp bài Speaking" mà KHÔNG upload file
    - Expect: submit blocked, error message chứa text tương đương `ERR_SPEAKING_FILE_REQUIRED` hoặc "Chọn file nói trước khi nộp"
  - [x] 5.3 ERR-007: `Teacher enters invalid score (out of range) → blocked`
    - Fixture: `seedSubmittedSpeakingChain(api)` (đã có từ 6.6)
    - Teacher login → navigate `/teacher/results` → filter để thấy submission → mở detail panel
    - Nhập score = -1 hoặc 999 (out of range) → click Save
    - Expect: save blocked với error "Nhập điểm Speaking hợp lệ" (tương đương `ERR_SCORE_INVALID`)
    - Expect: feedback draft KHÔNG bị mất
  - [x] 5.4 ERR-008: `Student opens Homework after deadline → expired/blocked state`
    - Fixture: `seedExpiredHomeworkChain(api)` (Task 1.5)
    - Student login (through /class flow) → Assigned Tests
    - Tìm item với trạng thái expired/past deadline
    - Expect: status badge/text hiển thị "Đã hết hạn" hoặc tương đương
    - Expect: "Bắt đầu" button không còn clickable hoặc bị disabled/hidden
  - [x] 5.5 ERR-009: `Student opens Live Exam before teacher opens session → blocked`
    - Fixture: Teacher tạo LiveExamSession (NOT opened) + homework via API → student sees it in list
    - Student login → Assigned Tests → tab "Thi trực tiếp"
    - Tìm session ở trạng thái "Not open"
    - Expect: "Bắt đầu" disabled hoặc error "Bài thi trực tiếp chưa mở" (tương đương `ERR_LIVE_EXAM_NOT_OPEN`)

- [x] Task 6: AC2 — Autosave và missing-answer edge cases EDGE-002, EDGE-003 (AC: 2)
  - [x] 6.1 Tạo `tests/EnglishTestWeb.E2E/flows/edge-cases/edge-autosave.spec.ts`
  - [x] 6.2 EDGE-002: `Student enters answers → reloads → answers restore`
    - Fixture: `seedNotSubmittedReadingChain(api)` (Task 1.6) — creates attempt với partial answers đã saved server-side
    - Student login → navigate trực tiếp đến `/student/attempts/{submissionId}` (không cần qua class entry nếu đã có session cookie)
    - Verify answers đang hiển thị (restored từ server draft)
    - Tùy optional: reload page và verify answers vẫn hiển thị sau reload
    - Expect: autosave status region visible và không ở trạng thái error
  - [x] 6.3 EDGE-003: `Student submits with missing answers → warning modal shows count`
    - Fixture: create ready Reading template (3 questions) + homework qua API
    - Student login → open homework → điền CHỈ 1/3 answers
    - Click "Nộp bài"
    - Expect: `[data-testid="submit-confirm-modal"]` visible
    - Expect: modal chứa text về số answers còn thiếu (ví dụ "2 câu chưa trả lời" hoặc "2 câu còn thiếu")
    - Verify: student có thể click "Quay lại" để tiếp tục chỉnh sửa

- [x] Task 7: AC3 — Duplicate action edge cases EDGE-004 (AC: 3)
  - [x] 7.1 Tạo `tests/EnglishTestWeb.E2E/flows/edge-cases/edge-duplicate-actions.spec.ts`
  - [x] 7.2 Mark Ready double-click: Teacher navigates đến review step của draft template → click "Mark Ready" → verify button bị disabled/loading → verify chỉ có 1 lần transition (kiểm tra badge = "Ready" và không có error conflict)
  - [x] 7.3 Final submit double-click (UI): Student trong exam workspace → điền answers → click "Nộp bài" → confirm modal → click "Xác nhận" → verify submit success state → verify submit button không thể click lại
  - [x] 7.4 Grading save double-click (UI): Teacher grading Speaking → nhập valid score/feedback → click Save → verify row status updates → verify Save button disables sau khi saved (hoặc shows saved state)
  - [x] 7.5 Note: Idempotency tại API level đã được cover trong story 6.5. Story này chỉ verify UI protection (button disable/loading state sau action).

- [x] Task 8: AC4 — Results edge cases EDGE-005, EDGE-006 (AC: 4)
  - [x] 8.1 Tạo `tests/EnglishTestWeb.E2E/flows/edge-cases/edge-results.spec.ts`
  - [x] 8.2 EDGE-005: `Teacher applies filters matching no rows → empty state + clear filters`
    - Teacher login → navigate `/teacher/results`
    - Nhập student search term không tồn tại (ví dụ `ZZZNOBODYZZZXXX`)
    - Expect: table/list empty state visible
    - Expect: "Xóa bộ lọc" hoặc "Clear filters" button visible trong empty state
    - Click clear filters → expect filters reset và rows reappear
  - [x] 8.3 EDGE-006: Intentionally skipped — no delete-file API exists to manufacture missing-file state in E2E. Selector `getMissingFileError()` added to POM; server guard covered by API tests.
    - **Approach**: Cần tạo một SpeakingSubmission với fileId trỏ tới file không tồn tại trong storage. Xem "EDGE-006 Implementation Approach" trong Dev Notes.
    - Teacher login → navigate đến detail của submission này
    - Expect: recoverable file error message visible (không crash/500)
    - Expect: score input và feedback textarea vẫn hiển thị và có thể nhập
    - Teacher nhập score + feedback → save → verify grading saved thành công dù file missing

## Dev Notes

### Bối cảnh và mục đích

Story 6.7 EXTENDS E2E infrastructure từ story 6.6. **Không tạo lại bất cứ gì đã có.** Tất cả test infrastructure đã tồn tại:
- `tests/EnglishTestWeb.E2E/` — scaffolded project
- `fixtures/test-fixtures.ts` — `test` extend với `apiContext`
- `fixtures/seed.ts` — existing helpers (loginTeacher, loginStudentWithClass, createReadyReadingTemplate, createHomeworkAssignment, createLiveExamSession, seedSubmittedSpeakingChain, v.v.)
- `fixtures/test-files.ts` — MINIMAL_PDF_BYTES, MINIMAL_WEBM_BYTES
- 8 POMs trong `pages/`
- 4 happy path spec files trong `flows/`

Nhiệm vụ chính: **thêm** seed helpers mới, **extend** POMs với methods còn thiếu, **tạo** 7 spec files mới trong 2 folder mới.

### Cấu trúc file cần tạo

```
tests/EnglishTestWeb.E2E/
├── fixtures/
│   └── seed.ts           ← EXTEND (thêm helpers mới, không xóa gì)
├── pages/                ← EXTEND các POMs (thêm methods, không thay đổi methods cũ)
│   ├── student-class-entry.page.ts
│   ├── create-template.page.ts
│   ├── attempt-workspace.page.ts
│   ├── speaking-submission.page.ts
│   └── results-grading.page.ts
└── flows/
    ├── error-blocking/       ← MỚI
    │   ├── err-001-002-access.spec.ts
    │   ├── err-003-005-template.spec.ts
    │   └── err-006-009-submission.spec.ts
    └── edge-cases/           ← MỚI
        ├── edge-autosave.spec.ts
        ├── edge-duplicate-actions.spec.ts
        └── edge-results.spec.ts
```

### Xử lý ERR-002: Student not in class

Seeded setup chỉ có 1 class (ENG7A) và student là member. Để test ERR-002 (not in class), options:

**Option A (preferred)**: Tạo class mới qua teacher API (nếu `POST /api/classes` hoặc `POST /api/teacher/classes` endpoint tồn tại). Teacher tạo new class → lấy `classCode` của class mới → student dùng code đó để login.

**Option B (fallback)**: Test trực tiếp qua `apiContext` (không cần browser):
```typescript
test('ERR-002: student login fails when not a class member', async ({ apiContext }) => {
  const xsrfToken = await getXsrfToken(apiContext);
  // Tạo class mới qua API
  const classRes = await apiContext.post('/api/classes', {
    data: { name: 'ERR-002 Test Class', code: `ERR${Date.now()}` },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  // Hoặc: dùng một class code giả mà biết student không trong đó
  const loginRes = await apiContext.post('/api/auth/student/login', {
    data: { identifier: STUDENT_IDENTIFIER, password: STUDENT_PASSWORD, classCode: 'NONEXIST99', rememberMe: false },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  expect(loginRes.status()).toBe(400); // hoặc 403/404 tùy implementation
  const body = await loginRes.json();
  expect(body.extensions?.code).toMatch(/class|member/i);
});
```

**Lưu ý**: Trước khi implement, kiểm tra xem API `/api/classes` POST có expose hay không bằng cách grep routes trong `src/EnglishTestWeb.Api/Controllers/`.

### EDGE-006 Implementation Approach

Test "Speaking file unavailable" là tricky vì file được lưu trong protected storage. Options:

**Option A (preferred - API fabricate)**: Tạo SpeakingSubmission bình thường → sau khi final submit, gọi API để delete/orphan file. Check nếu có DELETE `/api/files/{fileId}` hoặc admin endpoint. Nếu có, teacher xóa file sau đó mở grading.

**Option B (seed trực tiếp)**: Nếu không có delete API, viết seed helper tạo submission với một `fileId` fake (random UUID không tồn tại trong storage). Cần kiểm tra xem API cho phép không.

**Option C (skip EDGE-006 nếu không feasible)**: EDGE-006 là "nice to have" nếu không có cách manufacture missing-file state trong E2E. Document trong completion notes tại sao skip.

Ưu tiên implement Option A trước. Nếu không feasible, skip với note rõ ràng.

### EDGE-002 Setup approach

`seedNotSubmittedReadingChain` cần tạo một attempt đã có answers để test restore. Flow:
1. Teacher: create template + homework (API)
2. Student: login → `POST /api/submissions { homeworkAssignmentId }` → lấy submissionId
3. Student: `PUT /api/submissions/{submissionId}/answers` với 2/3 answers → này là server-side autosave
4. Return submissionId để test navigate trực tiếp vào workspace

Sau đó test reload bằng cách `page.reload()` và verify answers vẫn hiển thị.

### ERR-007 Score range

Max valid score cần biết từ AnswerKey config. Với Speaking template không có AnswerKey (speaking chỉ manual grade), max score được define ở chỗ nào đó trong domain. Check `SpeakingSubmission` hoặc `GradingService` để biết valid score range. Điển hình: 0-10 hoặc 0-100. Test với `-1` hoặc `999` để trigger `ERR_SCORE_INVALID`.

### Bẫy cần tránh (từ story 6.6 + new)

1. **KHÔNG break existing tests** — chỉ EXTEND POMs, không xóa/đổi methods cũ
2. **KHÔNG hardcode sleeps** — dùng `await expect(locator).toBeVisible()` và `page.waitForURL()`
3. **XSRF trong API fixtures** — mọi POST/PUT/DELETE phải có `X-XSRF-TOKEN` header (dùng `getXsrfToken()` đã có)
4. **fullyParallel: false vẫn giữ nguyên** — các tests share SQL Server database
5. **Unique names** — mọi entity tạo trong tests dùng `Date.now()` hoặc unique identifier
6. **Student login flow** — luôn đi qua `/class` → confirm → `/student/login` để có class context; HOẶC dùng `loginStudentWithClass` qua apiContext rồi dùng cookies
7. **API base URL**: `apiContext` → `http://localhost:5124`; `page.goto()` → `http://localhost:4200`
8. **File upload trong UI tests** — dùng `fileChooser` event, không path filesystem
9. **ERR-008 expired homework** — `deadlineAt` trong quá khứ cần đủ xa để server không còn accept (vài phút trước, không phải 1ms)

### Existing POMs và selectors đã biết

Từ `attempt-workspace.page.ts`:
- `[data-testid="workspace-header"]` — header load indicator
- `[data-testid="pdf-viewer"]` — PDF viewer
- `[data-testid="answer-input-{n}"]` — answer input cho câu n
- `.autosave-saved` — autosave success state
- `[data-testid="submit-button"]` — submit button
- `[data-testid="submit-confirm-modal"]` — confirmation modal
- `[data-testid="confirm-submit-btn"]` — confirm button trong modal
- `[data-testid="submit-success"]` — success state
- `.mode-badge` — mode badge

Từ `student-class-entry.page.ts`:
- `#student-class-entry-form`
- `#student-class-entry-code-input`
- `#student-class-entry-submit-button`
- `#student-class-entry-confirmation`
- `#student-class-entry-class-card`
- `#student-class-entry-confirm-button`

**Selectors cho error states chưa có trong POMs** — dev agent cần inspect Angular component để tìm đúng data-testid hoặc CSS class. Nguyên tắc: ưu tiên `data-testid` > `role` > CSS class.

### API Endpoints liên quan

```
# Auth
POST /api/security/xsrf-token    ← GET request để lấy token
POST /api/auth/login              ← Teacher login
POST /api/auth/student/login      ← Student login với classCode

# Templates
POST /api/test-templates          ← Tạo draft
POST /api/test-templates/{id}/materials   ← Upload material
PUT  /api/test-templates/{id}/answer-key  ← Set answer key
POST /api/test-templates/{id}/mark-ready  ← Mark ready

# Assignments
POST /api/homework-assignments    ← body: { templateId, classId, deadlineAt }
POST /api/live-exam-sessions      ← body: { templateId, classId }
POST /api/live-exam-sessions/{id}/open    ← Open session

# Submissions
POST /api/submissions             ← body: { homeworkAssignmentId, liveExamSessionId: null }
PUT  /api/submissions/{id}/answers        ← Autosave answers
POST /api/submissions/{id}/final-submit   ← Final submit

# Speaking
POST /api/speaking-submissions    ← body: { homeworkAssignmentId, liveExamSessionId: null }
POST /api/speaking-submissions/{id}/upload-draft  ← Upload file
POST /api/speaking-submissions/{id}/final-submit  ← Final submit

# Grading
PUT  /api/speaking-submissions/{id}/grading       ← body: { score, feedback }

# Classes
GET  /api/classes/by-code/{code}  ← Lookup class by code → { classId }
```

### References

- E2E project: `tests/EnglishTestWeb.E2E/` [Source: Story 6.6 dev notes]
- Existing seed helpers: `tests/EnglishTestWeb.E2E/fixtures/seed.ts`
- Existing POMs: `tests/EnglishTestWeb.E2E/pages/*.page.ts`
- TS-001 error cases: `_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml#error_state_tests`
- TS-001 edge cases: `_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml#edge_case_tests`
- Error codes: `CLAUDE.md#Architecture` — ProblemDetails extensions.code strings
- Story 6.5 dev notes: `_bmad-output/implementation-artifacts/6-5-api-security-and-contract-test-coverage.md` — API test patterns
- Story 6.6 dev notes: `_bmad-output/implementation-artifacts/6-6-playwright-happy-path-e2e-coverage.md` — POM patterns, fixture patterns, bẫy cần tránh

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- ERR-002: Implemented via wrong-credentials browser flow (no POST /api/classes endpoint to create a second class). The API returns `auth.loginInvalid` for both wrong credentials and not-in-class cases (security by obscurity). Test verifies browser `#student-login-error-alert` appears and page stays on login.
- ERR-006: `getNoFileHint()` selector `[data-testid="no-file-hint"]` is verified; if the component disables the submit button instead of showing a hint, the test checks button behavior after click.
- EDGE-006: Skipped — no delete-file API endpoint exists to manufacture a missing-file state in E2E. `getMissingFileError()` POM selector is wired; server guard is covered by API integration tests.
- `saveAnswerDraft` payload confirmed as `{ rows: [...] }` (not `{ answers: [...] }`) by reviewing `SubmissionsAutosaveTests.cs`.
- `createReadySpeakingTemplate` was already present in seed.ts — no duplication needed.

## Senior Developer Review (AI)

**Review Date:** 2026-06-14
**Outcome:** Changes Requested
**Dismissed:** 6 | **Deferred:** 3 | **Patch:** 6

### Action Items

- [x] [Review][Patch] ERR-008/ERR-009 conditional `if(isVisible)` makes tests non-falsifiable — replace with unconditional status-text assertion + `not.toBeEnabled()` [err-006-009-submission.spec.ts:508,541]
- [x] [Review][Patch] ERR-009 live-exam tab guard uses bare `isVisible()` without waiting — replace with `await expect(liveExamTab).toBeVisible()` [err-006-009-submission.spec.ts:530]
- [x] [Review][Patch] EDGE-002 `getByRole('button').first().click()` is unqualified — add name filter `/bắt đầu|tiếp tục/i` [edge-autosave.spec.ts:601]
- [x] [Review][Patch] EDGE-002 missing `page.reload()` — AC2 says "student reload after entering answers"; add reload + re-assert answers [edge-autosave.spec.ts:606]
- [x] [Review][Patch] ERR-005 materials upload response not checked with `.ok()` — silent failure would produce misleading timeout [err-003-005-template.spec.ts:358]
- [x] [Review][Patch] EDGE-003 answer not verified after modal Back — add assertion `answer-input-1` still has value 'A' after closing modal [edge-autosave.spec.ts:94]
- [x] [Review][Defer] ERR-002 tests credential blocking not class membership — no POST /api/classes endpoint; documented design decision in dev notes [err-001-002-access.spec.ts] — deferred, documented intentional fallback
- [x] [Review][Defer] `loginStudentViaClassEntry` helper duplicated in 3 spec files — maintenance hazard; move to shared fixture in future refactor [multiple spec files] — deferred, pre-existing pattern
- [x] [Review][Defer] `apiContext` is student-authenticated after `seedSubmittedSpeakingChain` — latent trap for future tests adding apiContext calls post-seed [seed.ts] — deferred, pre-existing

### Review Follow-ups (AI)

- [x] [AI-Review] Fix ERR-008/ERR-009 conditional assertions
- [x] [AI-Review] Fix ERR-009 tab guard
- [x] [AI-Review] Fix EDGE-002 button click and reload
- [x] [AI-Review] Fix ERR-005 upload ok() check
- [x] [AI-Review] Fix EDGE-003 answer preserved after Back

### File List

- tests/EnglishTestWeb.E2E/fixtures/seed.ts (modified — added 6 helpers)
- tests/EnglishTestWeb.E2E/pages/student-class-entry.page.ts (modified — getErrorMessage)
- tests/EnglishTestWeb.E2E/pages/create-template.page.ts (modified — 6 new methods)
- tests/EnglishTestWeb.E2E/pages/attempt-workspace.page.ts (modified — 3 new methods)
- tests/EnglishTestWeb.E2E/pages/speaking-submission.page.ts (modified — 4 new methods)
- tests/EnglishTestWeb.E2E/pages/results-grading.page.ts (modified — 7 new methods)
- tests/EnglishTestWeb.E2E/flows/error-blocking/err-001-002-access.spec.ts (new)
- tests/EnglishTestWeb.E2E/flows/error-blocking/err-003-005-template.spec.ts (new)
- tests/EnglishTestWeb.E2E/flows/error-blocking/err-006-009-submission.spec.ts (new)
- tests/EnglishTestWeb.E2E/flows/edge-cases/edge-autosave.spec.ts (new)
- tests/EnglishTestWeb.E2E/flows/edge-cases/edge-duplicate-actions.spec.ts (new)
- tests/EnglishTestWeb.E2E/flows/edge-cases/edge-results.spec.ts (new)

---

## Senior Developer Review (AI) — Round 2

**Review Date:** 2026-06-14
**Outcome:** Approve (1 patch applied)
**Dismissed:** 5 | **Deferred:** 3 | **Patch:** 1

### Action Items

- [x] [Review][Patch] EDGE-004a materials upload and answer-key PUT responses not checked with `.ok()` — silent failure causes `clickMarkReady()` to timeout with no diagnostic [edge-duplicate-actions.spec.ts:51-71]
- [x] [Review][Dismiss] `not.toBeEnabled()` on possibly-absent button (ERR-008/009) — Playwright `not.toBeEnabled()` passes when element is absent from DOM; Angular `@if` removes button, so assertion correctly captures disabled/absent as equivalent blocking states
- [x] [Review][Dismiss] `waitForSelector('.item-list')` after tab click may resolve stale (ERR-009) — Angular `@if (activeTab() === 'live-exam')` removes previous panel content synchronously; not a flake risk
- [x] [Review][Dismiss] `not.toHaveCount(0)` before rows populate (EDGE-005) — Playwright retry semantics on `expect(locator)` handle the async load; default 5s timeout is sufficient
- [x] [Review][Dismiss] Multiple 'E2E Reading' items causing `.first()` to resolve wrong card — known limitation per CLAUDE.md ("tests do not clean up"); tests are designed for fresh-DB CI runs; `.first()` picks oldest card which is the one created in this test on a clean DB
- [x] [Review][Dismiss] EDGE-002 no URL assertion after `page.reload()` — diagnostic improvement only, not a functional gap; `workspace.waitForLoad()` timeout already provides signal if redirect occurs
- [x] [Review][Defer] URL assertion `waitForURL('**/student/attempts/**')` after reload — nice diagnostic hardening; defer to future hardening pass
- [x] [Review][Defer] `getBackFromModalButton()` not scoped inside modal — structurally acceptable since `toBeVisible()` precondition guards the modal; defer to future POM refactor
- [x] [Review][Defer] EDGE-003 comment says "in-memory answer not cleared" but could imply persistence — wording improvement only; defer

### Review Follow-ups (AI)

- [x] [AI-Review] Fix EDGE-004a: add `ok()` checks for upload and answer-key PUT
