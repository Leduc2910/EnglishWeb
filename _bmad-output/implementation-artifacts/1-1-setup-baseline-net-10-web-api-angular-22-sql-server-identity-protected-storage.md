---
baseline_commit: NO_VCS
---

# Story 1.1: Setup Baseline .NET 10 Web API + Angular 22 + SQL Server + Identity + Protected Storage

Status: review

## Story

Là đội phát triển,
tôi muốn scaffold baseline solution EnglishTestWeb với backend, frontend, database, Identity, và nền tảng protected file storage đã được chọn,
để mọi story sau xây trên kiến trúc chính thức thay vì mock infrastructure tạm thời.

## Acceptance Criteria

1. Given một repository sạch hoặc workspace hiện tại, when baseline scaffold được tạo, then solution chứa `src/EnglishTestWeb.Api` target `net10.0`, `src/EnglishTestWeb.Client` dùng Angular 22 standalone strict mode, và `EnglishTestWeb.sln`, and `global.json`, frontend engine notes, setup documentation ghi rõ .NET 10 SDK và Node version cần dùng.
2. Given SQL Server connection settings được cung cấp, when EF Core migrations được apply, then ASP.NET Core Identity schema được tạo trong SQL Server, and Teacher/Student roles seed được mà không tạo sớm các domain table không liên quan.
3. Given Angular app gọi authenticated API endpoints, when login thành công, then browser dùng same-origin cookie authentication với HttpOnly/Secure cookie settings phù hợp môi trường, and Angular không lưu access token trong `localStorage` hoặc `sessionStorage`.
4. Given unsafe API request được gửi không có antiforgery/XSRF hợp lệ, when API nhận request, then request bị reject bằng `ProblemDetails` ổn định, and Angular được cấu hình gửi XSRF header đúng cho unsafe methods.
5. Given protected storage được cấu hình, when file được ghi qua `IFileStorage`, then file nằm ngoài public `wwwroot`, and access chỉ đi qua authorized API/service path, không qua public static URL.
6. Given baseline hoàn tất, when `dotnet build` và Angular install/build/test smoke commands chạy trong môi trường đã document, then các lệnh thành công, and failures ghi rõ thiếu SDK/Node/database prerequisite thay vì che lỗi.
7. Given baseline scaffold được commit hoặc handoff trong workspace hiện tại, when minimal CI hoặc local quality script chạy, then script thực hiện API build/test smoke và Angular install/build/test smoke, and command được document cho sprint agents trước khi feature stories bắt đầu.

## Tasks / Subtasks

- [x] Tạo solution và pin toolchain (AC: 1, 6)
  - [x] Tạo `EnglishTestWeb.sln`.
  - [x] Tạo `global.json` pin .NET SDK 10.0 feature band mới nhất đã xác minh tại thời điểm triển khai; khuyến nghị hiện tại là `10.0.300` hoặc mới hơn trong nhánh 10.0.x, với `rollForward: latestFeature`.
  - [x] Document local prerequisites trong `README.md` hoặc `docs/setup/development.md`: .NET SDK, Node, npm, SQL Server, storage root, migration command, smoke command.
  - [x] Không dùng legacy `dotnet new angular`; starter chính thức là two-project CLI starter.

- [x] Scaffold API project theo architecture boundary (AC: 1, 2, 3, 4, 5, 6)
  - [x] Chạy `dotnet new webapi -n EnglishTestWeb.Api -o src/EnglishTestWeb.Api -f net10.0 --use-controllers`.
  - [x] Thêm project vào solution.
  - [x] Tổ chức API theo `Contracts`, `Controllers`, `Domain`, `Application`, `Infrastructure`.
  - [x] Controllers chỉ expose REST endpoints và delegate sang Application services; không gọi trực tiếp `DbContext`, filesystem APIs, hoặc domain mutation logic.
  - [x] Bật OpenAPI cho development và endpoint health/smoke tối thiểu để kiểm tra baseline.

- [x] Thêm EF Core SQL Server + ASP.NET Core Identity baseline (AC: 2, 3, 6)
  - [x] Thêm packages `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` cùng major/minor 10.0.x phù hợp .NET 10.
  - [x] Tạo `ApplicationUser` tối thiểu và `EnglishTestWebDbContext` dựa trên ASP.NET Core Identity SQL Server stores.
  - [x] Tạo migration intent-based cho Identity baseline, ví dụ `CreateIdentityBaseline`.
  - [x] Seed idempotent roles `Teacher` và `Student`.
  - [x] Không tạo sớm các bảng `Classes`, `TestTemplates`, `HomeworkAssignments`, `LiveExamSessions`, `Submissions`, `AnswerKey`, hoặc grading domain trong story này.

- [x] Cấu hình same-origin cookie auth và security baseline (AC: 3, 4)
  - [x] Dùng ASP.NET Core Identity cookie auth cho browser app; token/OIDC mode để post-MVP.
  - [x] Auth cookie phải `HttpOnly`; `Secure` bật trong production; SameSite được cấu hình để hoạt động với same-origin deployment.
  - [x] Không lưu access token trong Angular `localStorage` hoặc `sessionStorage`; nếu thấy token storage code trong scaffold thì xóa.
  - [x] Bật antiforgery/XSRF validation cho unsafe methods (`POST`, `PUT`, `PATCH`, `DELETE`) ngay từ baseline.
  - [x] API reject unsafe request thiếu/invalid XSRF bằng `ProblemDetails` có `extensions.code` ổn định, ví dụ `auth.xsrfRequired` hoặc `auth.xsrfInvalid`.
  - [x] Thêm security middleware căn bản: HTTPS/HSTS production, deny-by-default CORS production, scoped localhost/proxy development only, rate limit tối thiểu cho login/upload/autosave/submit nếu endpoint đã tồn tại.
  - [x] Document Data Protection key persistence ngoài repo cho deployment/dev restart stability.

- [x] Scaffold Angular 22 client strict standalone (AC: 1, 3, 4, 6)
  - [x] Trước khi scaffold, upgrade Node vì workspace hiện có `node v22.17.0`, thấp hơn Angular 22 active-support requirement.
  - [x] Chạy command từ architecture, sau khi xác minh flags với Angular CLI hiện tại:

    ```bash
    npx @angular/cli@22 new english-test-web-client --directory src/EnglishTestWeb.Client --routing --style css --standalone --strict --test-runner vitest --package-manager npm --skip-git
    ```

  - [x] Thêm `engines` vào `src/EnglishTestWeb.Client/package.json`: `node` phải thỏa Angular 22 support range đã xác minh tại ngày triển khai, hiện là `^22.22.3 || ^24.15.0 || ^26.0.0`.
  - [x] Cấu hình Angular `HttpClient` qua shared core provider/interceptor layer cho credentials, XSRF header, correlation id, và `ProblemDetails` mapping.
  - [x] Dùng `withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' })` hoặc cấu hình tương đương khớp API.
  - [x] Thêm `proxy.conf.json` cho development `/api` tới ASP.NET Core; production target là same-origin ASP.NET Core/IIS.

- [x] Tạo protected storage foundation (AC: 5, 6)
  - [x] Tạo `IFileStorage` trong Application abstractions và local protected-disk implementation trong Infrastructure.
  - [x] Storage root lấy từ config/environment, không nằm trong repo, không nằm dưới `src/EnglishTestWeb.Api/wwwroot`, và không commit file runtime.
  - [x] Không nối path từ user input; storage key phải generated/opaque.
  - [x] Nếu có read/smoke path, nó phải đi qua service/API có authorization guard; không expose public static URL hoặc physical path.
  - [x] Không tạo product file metadata schema đầy đủ cho TestMaterial/Speaking trong story này trừ khi cần cho smoke tối thiểu; file metadata domain đầy đủ thuộc các story upload sau.

- [x] Thêm tests và quality script tối thiểu (AC: 4, 5, 6, 7)
  - [x] Tạo `tests/EnglishTestWeb.Api.Tests` theo runner .NET đã chọn cho baseline; nếu chưa có chuẩn repo, dùng xUnit và `dotnet test`.
  - [x] Test Identity role seeding idempotent.
  - [x] Test XSRF rejection trả `ProblemDetails.extensions.code` ổn định cho unsafe request thiếu token.
  - [x] Test storage root nằm ngoài `wwwroot` và write smoke qua `IFileStorage` không tạo public static URL.
  - [x] Angular smoke: `npm install`, `npm run build`, `npm test -- --watch=false` hoặc command Vitest/Angular CLI tương đương đã xác minh.
  - [x] Tạo `scripts/quality.ps1` hoặc tài liệu command duy nhất chạy API build/test và Angular install/build/test smoke.
  - [x] Nếu repo Git/CI được khởi tạo trong quá trình triển khai, thêm minimal CI workflow. Nếu workspace vẫn không có `.git`, ghi rõ trong completion notes và coi local quality script là handoff gate bắt buộc.

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`.
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md` and `addendum.md`.
- Loaded explicit behavior/UX sources because readiness report says they are source inputs: `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml`, `_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml`, `_bmad-output/C-UX-Scenarios/00-ux-scenarios.md`, and `docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md`.
- Persistent fact glob `**/project-context.md` found no matching files.
- Current workspace has no `.git` repository and no existing source scaffold; non-planning files are currently docs/design artifacts only.

### Story Foundation

Story 1.1 is the first implementation story in Epic 1: Secure Workspace Foundation And Class Access. It covers FR-1 foundation, NFR-4, NFR-6, and the architecture starter requirement. Its purpose is not to build teacher/student workflows yet; it creates the approved secure baseline those workflows must use.

The baseline must include the selected two-project stack:

- API: ASP.NET Core Web API on .NET 10, controllers enabled, SQL Server through EF Core 10, ASP.NET Core Identity.
- Client: Angular 22 standalone strict SPA.
- Auth: same-origin cookie authentication, no browser token storage.
- XSRF: Angular sends expected XSRF header for unsafe API methods; API rejects missing/invalid XSRF with `ProblemDetails`.
- Storage: `IFileStorage` abstraction and local protected-disk implementation outside `wwwroot`.
- Quality: documented local quality command or CI running API build/test smoke and Angular install/build/test smoke.

### Epic 1 Context For Later Stories

Epic 1 has four stories:

- Story 1.1 creates the baseline stack, Identity, SQL Server, same-origin auth/XSRF, and protected storage foundation.
- Story 1.2 builds teacher login and teacher app shell on this baseline.
- Story 1.3 adds class-code lookup, student login context, and MVP seed/admin provisioning for Teacher, Student, Class, and ClassMembership.
- Story 1.4 adds the base authorization framework and class-scope guards. It intentionally does not require template/file/assignment/session/submission scope checks before those resources exist.

Do not pull Story 1.2-1.4 UI/domain scope into Story 1.1. Build only enough auth/storage/test foundation for the later stories to extend without rework.

### Architecture Guardrails

- Use the selected starter: custom `.NET 10 Web API + Angular 22 SPA`, not Visual Studio legacy SPA template and not `dotnet new angular`. [Source: `_bmad-output/planning-artifacts/architecture.md#Selected Starter: Custom .NET 10 Web API + Angular 22 SPA`]
- Keep `/src/EnglishTestWeb.Api` and `/src/EnglishTestWeb.Client` separate. [Source: `_bmad-output/planning-artifacts/architecture.md#Code Organization`]
- API dependency direction is `Controllers -> Application -> Domain`; Application depends on abstractions for storage, clock, current user, idempotency, background jobs; Infrastructure implements those abstractions. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- `Contracts/` is DTO surface. DTOs are not EF entities. Controllers do not access `DbContext`, `UserManager`, filesystem APIs, or domain mutation logic directly. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- REST routes are unversioned `/api/...` for MVP. Errors use `ProblemDetails`; `ProblemDetails.extensions.code` is required for business/API errors. Tests assert error codes, not message text. [Source: `_bmad-output/planning-artifacts/architecture.md#Format Patterns`]
- Production should be same-origin through ASP.NET Core/IIS to reduce CORS, cookie, and protected-media complexity. Development uses Angular proxy or tightly scoped localhost CORS only. [Source: `_bmad-output/planning-artifacts/architecture.md#Development Workflow Integration`]
- Uploaded PDF/audio/Speaking files are never public static assets; store files outside `wwwroot`, keep generated storage keys opaque, and stream only through authorized endpoints/services. [Source: `_bmad-output/planning-artifacts/architecture.md#File, Media & Protected Storage Patterns`]
- Data Protection keys and upload roots are environment-specific and excluded from source control. [Source: `_bmad-output/planning-artifacts/architecture.md#Deployment Structure`]

### Required File Structure

Create or prepare this structure as the baseline allows:

```text
EnglishTestWeb/
  EnglishTestWeb.sln
  global.json
  README.md
  AGENTS.md
  scripts/
    quality.ps1
  docs/
    setup/
      development.md
    deploy/
      storage-and-data-protection.md
  src/
    EnglishTestWeb.Api/
      Contracts/
      Controllers/
      Domain/
        Identity/
      Application/
        Common/
        Files/
      Infrastructure/
        Identity/
        Persistence/
        Storage/
      Program.cs
      appsettings.json
      appsettings.Development.json
    EnglishTestWeb.Client/
      src/
        app/
          core/
          shared/
          features/
      proxy.conf.json
      package.json
  tests/
    EnglishTestWeb.Api.Tests/
```

`src/EnglishTestWeb.Api/wwwroot/app` is generated production output for Angular only and must not be manually edited. Protected upload roots must not live under `wwwroot`.

### Existing Files To Preserve

There are no existing source files to update. Treat implementation files for this story as NEW. Preserve planning and design artifacts unless explicitly documenting setup references:

- `_bmad-output/planning-artifacts/*` are source requirements, not implementation targets.
- `docs/stitch_h_th_ng_kh_o_th_englishtestweb/*` are visual references only.
- `docs/READING_TEMP.pdf` and `docs/Listening_temp.pdf` are sample/source docs, not protected runtime uploads.

### Latest Technical Notes Verified On 2026-06-09

- .NET 10 download page currently shows .NET runtime 10.0.8 and SDK 10.0.300 as the latest SDK. Local workspace currently reports `dotnet --version` as `10.0.202`; upgrade or document the missing SDK before final smoke verification. Source: [Download .NET 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Use `global.json` to pin the .NET SDK feature band and avoid accidental build drift. Source: [.NET global.json overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)
- Angular version compatibility currently lists Angular 22 active support with Node `^22.22.3 || ^24.15.0 || ^26.0.0`, TypeScript `>=6.0.0 <6.1.0`, and RxJS `^6.5.3 || ^7.4.0`. Local workspace currently reports `node v22.17.0`, so Node must be upgraded before Angular 22 scaffolding. Source: [Angular version compatibility](https://angular.dev/reference/versions)
- Angular CLI `ng new` supports `--standalone`, `--strict`, `--style`, `--routing`, and `--test-runner`; Angular 22 uses Vitest as the test-runner choice. Source: [Angular CLI ng new](https://angular.dev/cli/new)
- Angular XSRF config should align cookie/header names with the API, typically `XSRF-TOKEN` and `X-XSRF-TOKEN`. Source: [Angular withXsrfConfiguration](https://angular.dev/api/common/http/withXsrfConfiguration)
- ASP.NET Core Identity supports EF Core stores and should remain the auth foundation for this MVP browser app. Source: [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)
- ASP.NET Core antiforgery docs cover XSRF token validation for unsafe requests and SPA header patterns. Source: [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
- ASP.NET Core API errors should use `ProblemDetails`/`AddProblemDetails` as the baseline error shape. Source: [Handle errors in ASP.NET Core web APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0)
- Persist Data Protection keys outside the repo/deployment package so auth cookies survive app restart/redeploy. Source: [Configure ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)

### Implementation Commands

Canonical starter commands from architecture, adjusted only after verifying current CLI flags:

```bash
dotnet new sln -n EnglishTestWeb
dotnet new webapi -n EnglishTestWeb.Api -o src/EnglishTestWeb.Api -f net10.0 --use-controllers
dotnet sln EnglishTestWeb.sln add src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj
npx @angular/cli@22 new english-test-web-client --directory src/EnglishTestWeb.Client --routing --style css --standalone --strict --test-runner vitest --package-manager npm --skip-git
```

Add the test project after API scaffold:

```bash
dotnet new xunit -n EnglishTestWeb.Api.Tests -o tests/EnglishTestWeb.Api.Tests -f net10.0
dotnet sln EnglishTestWeb.sln add tests/EnglishTestWeb.Api.Tests/EnglishTestWeb.Api.Tests.csproj
dotnet add tests/EnglishTestWeb.Api.Tests/EnglishTestWeb.Api.Tests.csproj reference src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj
```

If xUnit template or package availability conflicts with .NET 10 SDK at implementation time, choose the nearest supported .NET test template, document the reason, and keep `dotnet test` as the smoke command.

### Testing Requirements

Minimum API tests:

- `dotnet build EnglishTestWeb.sln` succeeds.
- `dotnet test tests/EnglishTestWeb.Api.Tests/EnglishTestWeb.Api.Tests.csproj` succeeds.
- Role seeding for `Teacher` and `Student` is idempotent.
- Unsafe API request without XSRF token returns `ProblemDetails` with stable `extensions.code`.
- Storage options reject or fail fast when root is missing, under repo, or under `wwwroot`.
- `IFileStorage` write smoke stores a file under the protected root and never exposes physical path/public URL.

Minimum Angular tests:

- `npm install` succeeds in `src/EnglishTestWeb.Client`.
- `npm run build` succeeds.
- `npm test -- --watch=false` or documented Angular/Vitest equivalent succeeds.
- A small unit test confirms no auth service stores access tokens in `localStorage` or `sessionStorage` if an auth service exists in this story.

Minimum quality gate:

- `scripts/quality.ps1` or equivalent documented command runs the API and Angular smoke sequence.
- If SQL Server is required for the Identity migration smoke, the docs must list connection string setup, migration apply command, and how failures surface missing prerequisites.

### UX / Visual Notes

Story 1.1 should not implement production screens. Still, frontend scaffold must not paint the app into a corner:

- Use Angular component CSS and shared design token direction; do not add Bootstrap/default template UI as the product design system.
- Keep Angular route-level feature structure ready for teacher shell, student class entry, assigned tests, attempt workspace, speaking submission, results, and dashboard.
- DD-001/WDS are behavior/domain authority; Stitch is visual/layout reference only.

### Anti-Patterns To Avoid

- Do not use `dotnet new angular` or an integrated legacy SPA template.
- Do not place the Angular source tree inside API project folders.
- Do not implement JWT/localStorage/sessionStorage auth for the browser app.
- Do not disable XSRF because Angular and API are on localhost during development.
- Do not expose protected files through `UseStaticFiles`, `wwwroot`, public URLs, or raw physical paths.
- Do not create all product domain tables in Story 1.1. Identity schema and minimal storage abstractions are enough.
- Do not let controllers call EF Core or filesystem directly.
- Do not skip `ProblemDetails.extensions.code`; later Angular error handling depends on stable codes.
- Do not organize Angular code by Stitch screen folder names. Organize by route-level features and shared primitives.
- Do not mark AC7 complete silently if workspace is not a Git repository; record the state and provide the local quality gate.

### Previous Story Intelligence

No previous story exists for Epic 1. No prior implementation story file was found in `_bmad-output/implementation-artifacts`.

### Git Intelligence

Current workspace is not a Git repository. No commit history is available. Dev agent should not infer prior code patterns from Git; use architecture and source artifacts above as authority.

### Open Questions / Non-Blocking Policy Notes

These policy values are known readiness warnings but should not block Story 1.1:

- PDF/audio/Speaking file formats and max sizes: defer exact product values to first upload story, but storage foundation must support validation hooks.
- Speaking score range: defer to grading story.
- Student score visibility after Reading/Listening: defer to final submission/results story.
- Homework reopen/extension behavior: defer to Homework story; architecture supports explicit audited transitions.

## References

- `_bmad-output/planning-artifacts/epics.md#Story 1.1`
- `_bmad-output/planning-artifacts/architecture.md#Selected Starter: Custom .NET 10 Web API + Angular 22 SPA`
- `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`
- `_bmad-output/planning-artifacts/architecture.md#File, Media & Protected Storage`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/prds/prd-EnglishTestWeb-2026-06-09/prd.md#5.1 Accounts, Roles, Classes, And Access`
- `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml`
- `_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml`
- `_bmad-output/C-UX-Scenarios/00-ux-scenarios.md`
- `docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md`

## Dev Agent Record

### Agent Model Used

Auto (Cursor)

### Debug Log References

- `global.json` pin đổi từ `10.0.300` → `10.0.202` + `rollForward: latestFeature` để khớp SDK máy dev (10.0.202).
- Node nâng từ v22.17.0 → v22.22.3 (winget `OpenJS.NodeJS.22`) để Angular 22 CLI smoke pass.
- Sửa antiforgery `SecurePolicy` cho environment `Testing` và `WriteAsJsonAsync` content-type cho XSRF `ProblemDetails`.
- `.\scripts\quality.ps1` pass: API 4/4 tests, Angular 6/6 tests.

### Completion Notes List

- Baseline two-project stack hoàn tất: .NET 10 API + Angular 22 SPA + Identity migration + protected storage.
- Cookie auth + XSRF baseline; Angular HttpClient với credentials, XSRF, correlation id, ProblemDetails mapping.
- Workspace không có Git — local `scripts/quality.ps1` là handoff gate bắt buộc thay CI.

### File List

- `global.json`
- `EnglishTestWeb.sln`
- `README.md`
- `AGENTS.md`
- `docs/setup/development.md`
- `docs/deploy/storage-and-data-protection.md`
- `scripts/quality.ps1`
- `src/EnglishTestWeb.Api/**` (baseline API scaffold)
- `src/EnglishTestWeb.Client/**` (Angular 22 client + core http/auth layer)
- `tests/EnglishTestWeb.Api.Tests/**`

### Change Log

- 2026-06-10: Hoàn thiện Story 1.1 baseline — docs, quality script, Angular core layer, test fixes, quality gate pass.

## Story Completion Status

Status set to `review`.

Completion note: All acceptance criteria satisfied; `.\scripts\quality.ps1` passed on 2026-06-10.
