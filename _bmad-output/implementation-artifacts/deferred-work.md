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

## Deferred from: code review chunk 4 — docs & quality (2026-06-10)

- Deploy doc registry keys không ghi Windows-only caveat — acceptable MVP doc.
- `quality.ps1` dùng `npm install` thay `npm ci` — acceptable local smoke gate.
