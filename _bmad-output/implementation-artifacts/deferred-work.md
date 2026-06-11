# Deferred Work

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
