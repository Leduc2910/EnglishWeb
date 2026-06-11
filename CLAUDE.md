# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Quality gate (run before committing)
```powershell
.\scripts\quality.ps1
```
Validates SDK/Node versions, builds the solution, runs all API tests, and runs all Angular tests. Does **not** require SQL Server.

### API (backend)
```powershell
# Build
dotnet build EnglishTestWeb.sln

# Run all tests (uses in-memory DB)
dotnet test tests\EnglishTestWeb.Api.Tests\EnglishTestWeb.Api.Tests.csproj

# Run a single test class or method
dotnet test tests\EnglishTestWeb.Api.Tests\EnglishTestWeb.Api.Tests.csproj --filter "FullyQualifiedName~TestTemplatesControllerTests"

# Start API (http://localhost:5124) — use http profile when developing with Angular proxy
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj --launch-profile http
```

### Angular (frontend)
```powershell
cd src\EnglishTestWeb.Client
npm install
npm start           # dev server at http://localhost:4200 (proxies /api → :5124)
npm test            # runs vitest once
npm test -- --watch # vitest watch mode
npm run build
```

### Database / seeding
```powershell
dotnet tool restore
dotnet ef database update --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj

# One-shot seed commands (idempotent)
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj -- --seed-identity-roles
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj -- --seed-dev-teacher
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj -- --seed-mvp-demo
```

Default dev connection: `Server=localhost;Database=EnglishTestWeb_Dev;Trusted_Connection=True;TrustServerCertificate=True`

## Architecture

### Overview

```
EnglishTestWeb/
├── src/
│   ├── EnglishTestWeb.Api/       # ASP.NET Core 10 backend
│   └── EnglishTestWeb.Client/    # Angular 22 SPA
└── tests/
    └── EnglishTestWeb.Api.Tests/ # xUnit integration tests (in-memory DB)
```

### Backend — layered architecture

```
Application/    # Interfaces + DTOs + pure domain logic (no EF, no HTTP)
Domain/         # Entities: SchoolClass, ClassMembership, TestTemplate, StoredFile, TestMaterial
Infrastructure/ # EF Core implementations, Identity, file storage, authorization handlers
Controllers/    # Thin HTTP layer; delegates to Application services
```

The test project uses `WebApplicationFactory<Program>` with an in-memory database. Tests call `AuthTestHelper.SignInUserAsync()` to establish a cookie session before calling protected endpoints.

### Frontend — Angular 22

```
core/
  auth/          # AuthSessionService (session state), AuthApiService (HTTP)
  classes/       # ClassContextService, ClassesApiService, class-code normalizer
  test-templates/ # TestTemplatesApiService, models
  files/         # FilesApiService (authorized file preview)
  http/          # Interceptors: credentials, XSRF header, problem-details, correlation-id
  route-access/  # teacherGuard, guestGuard, rootRedirectGuard, studentGuard, studentLoginGuard

features/        # One component per route — lazy-loaded via loadComponent()
shared/
  layouts/       # TeacherShellComponent (wraps all /teacher/* routes)
```

All HTTP calls include credentials (`withCredentials: true`) via `credentials.interceptor.ts`. The XSRF header interceptor reads the token from `XsrfTokenStore` and attaches `X-XSRF-TOKEN` on unsafe methods.

### Auth & security

- **Cookie-based** (ASP.NET Core Identity). No JWT, no localStorage.
- **XSRF**: cookie `XSRF-TOKEN` + request header `X-XSRF-TOKEN` for all mutating endpoints.
- **Roles**: `Teacher` and `Student` (seeded into Identity). Role names in `IdentityRoleNames`.
- **Resource authorization**: custom `IAuthorizationHandler` implementations for class and template ownership. A teacher trying to access another teacher's resource gets 404 (hidden), not 403.
- **Student class context**: `etw:active_class_id` claim injected at login; server-revalidates membership on each important request via `IClassAuthorizationService`.
- **API errors**: `ProblemDetails` with stable `extensions.code` strings (e.g. `templates.notFound`, `classes.notFound`, `auth.forbidden`).

### Authorization policies (in `AuthorizationPolicies`)

| Policy | Requires |
|---|---|
| `CanViewClassAsTeacher` | Role=Teacher + owns class |
| `CanViewClassAsStudent` | Role=Student + active membership |
| `CanViewTemplateAsTeacher` | Role=Teacher + owns template |
| `CanEditTemplateAsTeacher` | Role=Teacher + owns template + status=Draft |

### Key domain rules

- `TestTemplate.Status`: `Draft` → `Ready` → `Archived`. Only `Draft` templates are editable (409 `templates.notEditable` otherwise).
- Protected file storage: files stored outside `wwwroot` under `%LOCALAPPDATA%\EnglishTestWeb\protected-storage`. Served only through authorized `GET /api/files/{fileId}/content` (supports `Range`).
- `LocalProtectedFileStorage` encrypts file paths with ASP.NET Data Protection.

### Dev seed accounts

| User | Email | Password |
|---|---|---|
| Teacher | `teacher@englishtestweb.local` | `Teacher123!` |
| Student | `student@englishtestweb.local` | `Student123!` |
| Class code | `ENG7A` | — |

Enabled by default in `appsettings.Development.json` via `Identity:SeedDevTeacherOnStartup` and `Identity:SeedMvpDemoOnStartup`.

### Important dev notes

- **Always use `--launch-profile http`** when running the API alongside the Angular dev server. The `https` profile triggers 307 redirects that break cookie/XSRF same-origin behavior.
- **Angular proxy**: `proxy.conf.json` forwards `/api/*` to `http://localhost:5124`. Do not call the API port directly from the browser during development.
- API tests use a **separate in-memory database per `TestApiFactory` instance** — parallelism is safe.
- Angular tests use **Vitest** (not Karma/Jasmine). Test files are `*.spec.ts`.
