# Deferred Work

## Deferred from: code review of 4-3-draft-answer-autosave-and-restore (2026-06-12)

- `SubmissionService.AutosaveAnswersAsync`: Concurrent PUT requests racing trên cùng QuestionNumber mới có thể gây unique index violation (DbUpdateException 500) — MVP single-session assumption che khuất nhưng cần xử lý khi mở rộng multi-device.
- `SubmissionsController` / `AutosaveAnswersRequest`: Không có upper bound trên số lượng Rows trong một request — security hardening khi cần kiểm soát tải.
- `SubmissionService.AutosaveAnswersAsync`: QuestionNumber <= 0 không được validate — spec nói không bắt buộc; cần validate khi question bounds trở thành requirement.
- `SubmissionsController.AutosaveAnswers`: Ownership không verify assignment còn active/chưa hết hạn — ngoài phạm vi story 4.3.
- `SubmissionService.AutosaveAnswersAsync`: TOCTOU giữa status check và SaveChangesAsync có thể cho phép autosave sau khi submission được nộp đồng thời — MVP single-session assumption.
- `SubmissionService.AutosaveAnswersAsync` line 278: Surrogate pair split khi truncate string > 500 chars (`row.Answer[..500]`) — chỉ xảy ra qua direct API bypass (HTML maxlength="500" che khuất normal path). Fix khi cần: dùng `StringInfo.LengthInTextElements` hoặc check `char.IsHighSurrogate`.

## Deferred from: code review round 2 of 4-1-student-assigned-tests-list (2026-06-12)

- `AssignedTestService`: 14-arg positional record constructor in LINQ projection — fragile to field-order changes; extract factory method when adding more DTO fields.
- Angular `onStartItem('available')`: clears `blockedItemMessage` but performs no navigation (intentional Story 4.2 placeholder); add "not yet implemented" visual guard if UX regression becomes noticeable before 4.2.
- Angular `logout()`: async errors swallowed by template `(click)` binding — add `.catch()` when standardizing error-handling patterns across components.
- Angular CSS class `status-{{ item.studentStatus }}`: string interpolation from backend-controlled union; add explicit CSS class map if new statuses are introduced outside the TypeScript union.
- Test: `SeedRolesAndUsersAsync` called redundantly inside each `SeedHomework*` / `SeedLiveExam*` helper — acceptable due to idempotency; refactor to `IAsyncLifetime` fixture when test suite grows.

## Deferred from: code review of 1-1-setup-baseline-net-10-web-api-angular-22-sql-server-identity-protected-storage (2026-06-10)

_Chunk 1 — API core only._

- `HttpContext` trong Application layer (`IXsrfTokenService`) — vi phạm boundary nhẹ, acceptable cho baseline; refactor khi tách security abstraction.
- Path validator không xử lý symlink escape — baseline heuristic đủ cho local dev; harden khi deploy production storage.
- `IFileStorage` write-only — read/delete thuộc upload stories sau; by design per story scope.

## Deferred from: code review chunk 2 — API tests (2026-06-10)

- Protected storage public-URL inaccessibility chưa HTTP-assert — cần static file middleware setup; baseline không expose static protected path.
- Positive XSRF path (valid token allows POST) — integration phức tạp hơn smoke scope; add khi có auth flow E2E.
- `IClassFixture<TestApiFactory>` perf optimization — smoke suite nhỏ, không blocking.

## Deferred from: code review chunk 3 — Angular client (2026-06-10)

- `AuthSessionService` chưa có session read/logout — Story 1.2 scope; baseline foundation only.
- Empty routes không có wildcard 404 — Story 1.2 teacher/student shell.
- Correlation ID luôn mới mỗi request — acceptable baseline; propagate later if needed.

## Deferred from: code review of 1-2-teacher-login-and-teacher-app-shell (2026-06-10)

- Rate limiting cho `POST /api/auth/login` — architecture khuyến nghị; story 1.2 ưu tiên auth correctness; thêm middleware scoped khi có infrastructure.

## Deferred from: code review of 1-3-class-roster-class-code-lookup-and-student-login (2026-06-10)

- ~~Post-login membership revalidation trên mỗi student API call~~ — **Resolved Story 1.4** (`/me`, `/classes/current` live membership check).
- Rate limiting cho `POST /api/auth/student/login` và `GET /api/classes/by-code/{code}` — cùng lý do defer 1.2.
- Teacher roster chỉ load chi tiết lớp đầu tiên khi teacher sở hữu nhiều lớp — MVP demo một lớp; mở rộng khi multi-class UX có spec.

## Deferred from: code review chunk 4 — docs & quality (2026-06-10)

- Deploy doc registry keys không ghi Windows-only caveat — acceptable MVP doc.
- `quality.ps1` dùng `npm install` thay `npm ci` — acceptable local smoke gate.

## Deferred from: code review of 2-1-thu-vien-de-list-search-filter-and-template-inspection (2026-06-10)

- Triple authorization check trên `GET /api/test-templates/{id}` — mirror `ClassesController`; refactor chung khi extract shared helper.

## Deferred from: code review of 1-4-base-authorization-pattern-and-class-scope-guards (2026-06-10)

- `GetClassContextByIdAsync` không có auth riêng — caller phải guard trước; document khi thêm caller mới.
- Triple authorization trên `GET /api/classes/{id}` — redundant DB round-trips; defense-in-depth acceptable MVP.
- Stale `active_class_id` cookie không bị xóa khi deny — live revalidation on read là design lock.
- `CanViewClassAsStudent` policy chưa gắn endpoint — inline check tương đương; handler foundation cho Epic 2+.
- Audit MVP chỉ `ILogger` — không DB persistence; per spec AC 4 scope.
- Correlation ID chỉ đọc client header — per spec E2 MVP.
- `/me` `activeClass` không có `status`; `/classes/current` có — summary vs detail intentional.

## Deferred from: code review of 2-2-create-edit-and-save-draft-template-setup (2026-06-10)

- Concurrent PUT cùng draft last-write-wins — không có concurrency AC trong story MVP; thêm row version khi multi-tab edit có spec.
- TagsJson corrupt deserialize silently → `[]` — edge case hiếm; log/surface error khi có persistence audit story.
- Tags chip UI — comma-only input đủ MVP story 2.2; chip add/remove UX defer polish Epic 2.
- DbUpdateException catch-all → `templates.tagLimit` — acceptable MVP; refine per constraint type khi có ops telemetry.

## Deferred from: code review of 2-3-protected-testmaterial-upload-and-preview (2026-06-10)

- Triple ownership DB round-trips per materials request — mirror 2.1/2.2 controller pattern; refactor khi extract shared auth helper.
- Edit auth handler không enforce draft; service layer là gate duy nhất — defense-in-depth acceptable MVP.
- Archived physical files không xóa khỏi disk — Implementation Note defer physical GC; sweeper story sau.
- File access chỉ check template owner, không check `StoredFile.OwnerUserId` — template ownership là auth model hiện tại.
- Non-seekable upload stream bỏ qua pre-write size check — edge case hiếm; post-write validation + MaxWriteBytes cap đủ MVP.

## Deferred from: code review of 2-4-answerkey-and-scoring-configuration (2026-06-11)

- Race condition first-upsert (2 concurrent PUT trên template mới) → unique-index violation → 409 bị mis-label — low probability MVP; fix khi implement upsert-or-retry pattern hoặc SQL MERGE.
- `template.UpdatedAt = now` side effect trực tiếp trong `AnswerKeyService` — service ghi vào unrelated entity qua shared DbContext; design smell, không gây bug hiện tại; tách ra khi extract unit-of-work.
- `CorrectAnswer` max length không capped ở service level → DoS via large RowsJson blob — thêm max-length guard khi có input validation middleware hoặc storage quota story.
- Tests: Boundary questionCount=1 và questionCount=200 chưa có test case riêng — off-by-one gap; add khi có test expansion.
- Tests: DbUpdateConcurrencyException và DbUpdateException paths không có test — in-memory DB không support rowversion; cần mock hoặc real DB integration test.
- Tests: Corrupt RowsJson graceful fallback không có test — cần direct DB write trong test helper; add khi có test infrastructure story.
- Tests: GET trên Ready/Archived template có existing answer key chưa test — deferred đến story 2.5 khi có promotion flow.
- Angular: `confirm()` dialog trong `applyQuestionCount` không testable trong Vitest/jsdom — cancel/confirm paths untested; refactor sang ConfirmService khi có modal infrastructure.
- Angular: Inner catch `getAnswerKey` áp dụng defaults cho ALL errors (không chỉ 404) — acceptable MVP pattern; tighten khi có error-telemetry story.
- Angular: Missing unit tests: Back button navigation (AC9), non-draft `loadError` (AC1), `goToReview()` speaking button — add khi có test expansion story.
- Angular: `ERR_ANSWER_MISSING` key trong `TEMPLATE_ERROR_MESSAGES` là dead code — cleanup cosmetic khi có refactor pass.
- Angular: `body.code` primary path trong `mapAnswerKeyApiError` là dead code (ProblemDetails luôn dùng `extensions.code`) — cleanup khi refactor error handling.

## Deferred from: code review pass 3 of 2-4-answerkey-and-scoring-configuration (2026-06-11)

_Patched in this pass: `continueErrors` stale-state UX bugs (Scenarios A/B/D), API tests for per-question score round-trip / zero-row partial save / updatedAt advancement._

- Backend: Race condition first-INSERT → `DbUpdateException` returns HTTP 500 (`answerKey.saveFailed`) instead of 409 — correct to reclassify + retry when adopting MERGE/upsert-or-retry; low priority MVP.
- Backend: `teacherId` param in `GetAsync` is unused — ownership enforced by policy at controller; service not self-defending; document when adding new callers.
- Backend: `QuestionCount`/`RowsJson` partial-save contract undocumented in domain — add `IsComplete()` domain method or XML doc when scoring pipeline is built.
- Backend: No cross-field validation between `ScoringMode` and `TotalScore` (equal mode with totalScore=null accepted) — frontend validates this; backend validation tightening deferred to scoring/submission story.
- Backend: `OnDelete(DeleteBehavior.Cascade)` on `AnswerKeyVersion` will become a data-loss cliff when TestSubmission entities arrive — change to Restrict and archive answer key data with submission when submissions are added.
- Backend: Empty `CorrectAnswer` indistinguishable from "not answered" in stored `RowsJson` — grading engine must treat both as wrong; document invariant when building auto-grader.

## Deferred from: code review of 2-5-review-template-mark-ready-and-next-actions (2026-06-11)

- Backend: `MarkReadyAsync` — race condition with concurrent mark-ready requests (no concurrency token on TestTemplate) → double-transition without error; add `[ConcurrencyCheck]` or row-version when building submission pipeline.
- Backend: `MarkReadyAsync` — `JsonException` during RowsJson deserialization → rows=[] → returns `answerKeyIncomplete` instead of a distinct `answerKeyCorrupt` code; add distinct code + warning log when adding data-integrity monitoring.
- Backend: `MarkReadyAsync` — speaking material check accepts any active material regardless of role; a stale `pdf` material (leftover from skill change) passes the speaking check; add role filter when implementing skill-change cleanup.
- Backend: `MarkReadyAsync` — `OperationCanceledException` from `SaveChangesAsync(cancellationToken)` propagates as unhandled 500; add explicit catch when adding standardized cancellation handling.
- Backend: `MarkReadyAsync` — service does not verify `teacherId` internally; ownership enforced only at controller/policy layer; add internal ownership check when refactoring to support direct service calls.
- Backend: `AnswerKeyVersions.FirstOrDefaultAsync` — no ordering → non-deterministic when multiple AnswerKey versions exist; add `.OrderByDescending(a => a.UpdatedAt)` when implementing AnswerKey re-versioning.
- Angular: `readinessChecks` computed swallows transient 5xx from `getAnswerKey` — `answerKey=null` makes check appear as "incomplete" when data may exist; tighten catch to re-throw on 5xx when adding error telemetry.
- Angular: Archived template on load shows mark-ready panel (→ 409 error on submit); add `archived` viewState to show non-editable state without confusing user.
- Angular: AC2 focus behavior — no scroll/focus to first failing checklist item when errors occur; add `scrollIntoView` when implementing UX polish pass.
- Angular: AC1 confirmation modal — mark-ready fires immediately without confirmation step per UX spec 01.7; add confirmation dialog when implementing UX polish pass.
- Angular: `loadPage` does not reset template/materials/answerKey signals at start of new load → stale data visible during rapid navigation; pre-existing pattern, fix in global UX hardening story.
- Architecture: AC5 — structured log only (no durable audit table); add `TemplateAuditLog` entity when building audit trail story (Epic 6).

## Deferred from: code review of 3-1-create-homeworkassignment-from-a-ready-template (2026-06-11)

- Backend: TOCTOU giữa auth check và DB load template/class — 2 query riêng, established pattern toàn codebase; cải thiện khi refactor shared auth helper.
- Backend: Không có unique constraint trên (TestTemplateId, ClassId) — server-side idempotency explicitly deferred trong Dev Notes; thêm khi implement X-Idempotency-Key hoặc deduplicate story.
- Backend: DbUpdateException catch-all → homework.createFailed 500 — consistent với project pattern; tách FK violation thành 409 khi có ops telemetry story.
- Backend: Không có index đơn trên ClassId/TestTemplateId — student-facing query pattern chưa có spec; thêm khi có Epic 4 student tests flow.
- Backend: HomeworkAssignment.Status không có DB check constraint — consistent với TestTemplate.Status; thêm khi standardize constraint layer.
- Angular: parseInt("5.9abc") truncates silently cho time limit input — server validates [1,600]; thêm Number.isInteger check khi có input validation story.
- Angular: Negative timeLimitMinutes bypass HTML min=1 → server rejects — inline field validation improvement; add khi có form validation refactor.
- Angular: Form signals (selectedClassId, deadlineAt, timeLimitMinutes) không reset khi templateId thay đổi — flow thực tế không trigger; fix khi có multi-template navigation UX.
- Angular: isFormValid() dùng stale template signal (không refresh) — server rejects if template archived; add auto-refresh khi có staleness detection story.
- Angular: Không có UX message khi teacher không có active class — form block đúng nhưng không có guidance; add empty-state message khi có UX polish pass.

## Deferred from: code review pass 3 of 3-1-create-homeworkassignment-from-a-ready-template (2026-06-11)

_Patched in this pass: inactive class guard (`homework.classNotActive` 400) — API was relying on Angular-only filter._

- Backend: `CreateHomeworkAssignmentRequest` không có `[Required]` data annotations trên `TemplateId`, `ClassId`, `DeadlineAt` — model binding validation không bắt zero-value Guids/default DateTimeOffset; add khi có input validation middleware story.

## Deferred from: code review pass 2 of 3-1-create-homeworkassignment-from-a-ready-template (2026-06-11)

- Backend: `StatusCode(201)` không set `Location` header — REST best practice cho 201 Created; thêm `CreatedAtAction` khi có GET by-id endpoint.
- Tests: `Create_TemplateNotOwned_Returns404` dùng `Guid.NewGuid()` (non-existent) thay vì existing-but-foreign template — test coverage gap cho hidden-404 auth path; add second-teacher seed khi có test infrastructure story.
- Tests: `Create_ClassNotOwned_Returns404` dùng `Guid.NewGuid()` (non-existent) thay vì existing-but-foreign class — same gap; add khi có second-teacher test helper.
- Tests: Deadline boundary test thiếu `now + 30s` và `now + 61s` — off-by-one coverage; add khi có test expansion story.
- Backend: `HomeworkAssignment` không có navigation properties — EF shadow navigation pattern; thêm nav props khi có list endpoint cần `.Include()`.

## Deferred from: code review of 3-3-usage-mode-contract-across-delivery-surfaces (2026-06-11)

- `HomeworkAssignment.AllowedActions` hardcoded `Array.Empty<string>()` — no `AllowedActionsFor` helper unlike live exam; correct for MVP (published is only status), but asymmetric; add helper when homework gains state transitions.
- `mode` values ("homework", "live-exam") are bare string literals — no shared constant; consistent with project string-status convention; add constants when API version or multi-client expansion warrants.
- Concurrent `OpenAsync` race condition on `LiveExamSession.Status` (see also 3-2 defer) — add `[ConcurrencyCheck]` on Status when building submission pipeline.
- No GET list/detail endpoints for HomeworkAssignment/LiveExamSession — when built, must include `Mode`/`AllowedActions` in mapping; no structural enforcement today.

## Deferred from: code review of 4-1-student-assigned-tests-list (2026-06-12)

- `HomeworkAssignment` with `default(DateTimeOffset)` deadline (epoch) evaluates to `"expired"` — data integrity concern predating this story; surface when adding schema validation or migration with DEFAULT constraint.
- `studentId` parameter in `IAssignedTestService.GetForStudentAsync` is accepted but not used in queries (filtering is classId-only by spec design); document when per-student item filtering is introduced.
- Unknown `LiveExamSession.Status` value silently collapses to `"closed"` — acceptable most-restrictive fallback; add explicit logging/mapping when new statuses are introduced.
- Angular `onStartItem`: unknown `studentStatus` falls through to `router.navigate` — values are backend-controlled contract; add explicit default guard when adding new student statuses.
- Two sequential DB round-trips in `AssignedTestService` (homework + live exams) instead of a UNION — optimize when profiling shows query latency is a concern.
- `AssignedTestItem.Status` exposes raw internal domain status strings without a stable API contract — acceptable by spec design; add mapping layer when API versioning is introduced.
- Non-deterministic sort order on `OrderByDescending(i => i.CreatedAt)` when multiple items share identical timestamps — add secondary sort key (`i.Id`) when list stability under load matters.
- Orphaned FK rows (deleted TestTemplate or Class) silently dropped by INNER JOIN — FK constraints prevent this in production; document when adding cascade/archive policy.
- Invalid `studentId` string (non-existent Identity user) passes whitespace guard and reaches service (which ignores it) — Identity session management upstream rejects invalid tokens.
- Angular: concurrent rapid reload requests can overwrite list signal with stale response — no pagination/cancellation scope in this story; address when implementing pull-to-refresh or streaming.

## Deferred from: code review pass 1 of 3-2-create-and-control-liveexamsession (2026-06-11)

- Backend: TOCTOU double-lookup auth pattern (template auth check → re-fetch template) — project-wide established pattern; refactor khi extract shared auth helper.
- Backend: Không có optimistic concurrency token trên `LiveExamSession.Status` — concurrent open/close có thể double-transition; add `[ConcurrencyCheck]` khi build submission pipeline.
- Backend: Cross-teacher Open/Close test thiếu real second-teacher fixture — consistent với 3-1 defer pattern; add khi có second-teacher test helper infrastructure.
- Backend: Multiple open sessions per class không bị chặn bởi DB/service constraint — design decision; clarify khi có student exam-taking story (Epic 4).
- Angular: datetime-local input parsed qua `new Date(rawString)` không có explicit timezone — browser behavior; low practical risk cho single-timezone MVP; standardize khi có i18n story.
- Angular: Session signals không refresh sau transition error (alreadyOpen/alreadyClosed) → Open button vẫn hiện mặc dù server đã chuyển trạng thái; add re-fetch on conflict error khi có UX polish pass.
- Angular: `scheduledEndAt < scheduledStartAt` không được validate client hoặc server — spec không yêu cầu temporal ordering cho MVP; add validation khi có scheduling story.

## Deferred from: code review of 5-3-teacher-speaking-playback-and-manual-grading (2026-06-13)

- `TeacherSpeakingGradingService`: `Feedback` và `GraderId` không có `HasMaxLength` trong EF config — không nhất quán với project convention (`nvarchar(max)`); thêm khi có input validation middleware story.
- `TeacherSpeakingGradingService.GradeAsync`: Re-grade bởi teacher khác (cùng template ownership) ghi đè `GraderId` gốc mà không có audit trail — `UpdatedAt` được cập nhật nhưng original grader bị mất; thêm grading history hoặc immutable grade record khi có audit story.

## Deferred from: code review of 6-1-results-filtering-table (2026-06-13)

- `ResultsService`: Full result set loaded into memory trước khi sort/paginate — cả hai queries dùng `ToListAsync()` không có DB-level Skip/Take. MVP trade-off đã ghi nhận; optimize khi profiling cho thấy latency.
- `ResultsService.GetResultsForTeacherAsync` Step 0: `studentIdFilter` IN clause không có upper bound — large Q match có thể tạo IN(thousands). Cùng scope với in-memory pagination; thêm limit khi có performance story.
- `ResultsService.GetResultsForTeacherAsync` Step 0: Q search queries `db.Users` (all users, not scoped to teacher's students) — teacher scope vẫn apply ở Step 1/2, không leak data nhưng chậm hơn cần thiết; thêm JOIN về teacher's classes khi có performance story.
- `ResultsService.cs` Step 1/2: Skill `""` (empty string) asymmetry — `filter.Skill=""` không thỏa `null || "speaking"` → speaking rows bị bỏ; Angular gửi `undefined` nên masked in practice; thêm backend normalization khi mở rộng API clients.
- `ResultsService.cs`: Status "graded" trả 0 RL results — RL không có "graded" status; by design; thêm documentation trong filter bar khi có UX polish.
- `ResultsService.cs` line 80: `Guid.Empty` sentinel khi cả hai navigations null → hiển thị `"00000000-0000-..."` làm class name; chỉ xảy ra khi data corrupt; add null-class guard khi có data integrity story.
- `ResultsService.cs` line 199: Sort tiebreaker dùng `Guid` order (random v4) — inconsistent pagination khi nhiều rows có cùng `submittedAt`; thêm `CreatedAt` tiebreaker khi có sort stability requirement.
- `ResultsService.cs`: Template navigation null → `TemplateTitle=""`, `TemplateId=Guid.Empty` — chỉ khi template bị orphan/xóa; thêm null-template guard khi có data integrity story.
- `teacher-results.component.ts` line 83: `selectedRowId` cleared unconditionally trên mỗi load — spec nói "nếu không còn khớp"; Story 6.2 sẽ implement fine-grained selection với detail panel.
- **AC1 Missing class filter (lớp):** Angular component không có class filter; backend hỗ trợ `classId` Guid. Dropdown cần populate từ `/api/teacher/classes` — out of scope 6.1; defer sang Story 6.2 khi redesign filter bar.
- **AC1 Missing template filter (template/đề):** Angular component không có template filter; backend hỗ trợ `templateId` Guid. Cần `/api/teacher/library` dropdown — defer sang Story 6.2.

## Deferred from: code review pass 2 of 6-1-results-filtering-table (2026-06-13)

- `ResultsService`: `needsGrading` là filter-dependent count — khi `status=graded` active, badge hiện `0 cần chấm` mặc dù có ungraded submissions ngoài filter. By design MVP; thay đổi khi có global pending-work dashboard (Story 6.3+).
- `ResultsService` Step 0: `.ToLower()` trên EF query có thể trigger client-side eval trên non-SQL providers (in-memory tests mask this). Production SQL Server dịch sang LOWER() nên OK; fix khi standardize collation-aware search hoặc dùng `EF.Functions.Like`.
- `ResultsService` Step 4: `SubmittedAt` nullable sort — drafts (null SubmittedAt) có unstable relative order cross-page. Thêm secondary sort by `CreatedAt` khi cần stable pagination.
- `teacher-results.component.spec.ts`: thiếu assertion verify filter signals = '' trong onClearFilters test; thiếu `onPageChange` test. Thêm khi có test expansion story.

## Deferred from: code review of 6-2-master-detail-results-and-grading-workspace (2026-06-13)

- `TeacherSubmissionDetailService.cs`: `Guid.Empty` classId fallback — unreachable in practice because the sourceTeacherId null check returns notFound first; latent concern if ownership check is ever refactored.
- `teacher-results.component.ts` `updateResultRow`: no-ops silently if row not present (e.g., user changed filter/page while grade API was in-flight). Acceptable MVP tradeoff; show toast notification when full notification system is built.
- `teacher-results.component.ts` `onGradeSubmit`: in-flight grade can leave `gradeState` at 'success' in a closed panel if `loadResults()` fires before the grade resolves. No data corruption; cosmetic artifact. Fix when adding request cancellation tokens to async workflows.
- `teacher-results.component.ts` `audioUrl` computed: briefly returns null when `results()` is replaced by a filter reload while the speaking panel is still open. Cosmetic flash only; fix by storing the file URL in a dedicated signal at `loadDetail` time.
- `teacher-results.component.ts` `loadFilterDropdowns`: class/template load failures silently produce empty dropdowns (`.catch(() => [])`). By design — dropdowns are non-critical auxiliary UI. Add error indicator when designing global error-notification system.

## Deferred from: code review round 2 of 6-2-master-detail-results-and-grading-workspace (2026-06-13)

- `TeacherSubmissionDetailService.cs`: silent `catch (JsonException) { }` leaves correct-answer column blank with no user-visible indicator. Add structured log when logging infrastructure is in place.
- `teacher-results.component.ts` `loadFilterDropdowns`: `listTemplates({ skill: '', status: '', q: '' })` returns Archived templates in the filter dropdown. Product decision needed — filter to `status: 'ready'` only, or keep all for historical data access.
- `TeacherSubmissionDetailService.cs`: `mode` defaults to `"live-exam"` when both HomeworkAssignmentId and LiveExamSessionId are null — unreachable due to prior ownership check; pre-existing.
- `teacher-results.component.spec.ts`: no test for non-null feedback path through `onGradeSubmit`; acceptable coverage gap for MVP.
- `teacher-results.component.spec.ts`: no test verifying `onSelectRow` retry triggers `loadDetail` when `detailState === 'error'`; add when expanding test coverage.
