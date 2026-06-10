# Development Setup — EnglishTestWeb

## Prerequisites

### .NET SDK

- Cài [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- Repo pin qua `global.json`: `10.0.202` với `rollForward: latestFeature`
- Khuyến nghị: SDK `10.0.300+` khi có sẵn trên máy dev
- Kiểm tra: `dotnet --version` (phải là `10.0.x`)

Nếu build báo thiếu SDK, cài đúng feature band hoặc nâng `global.json` cho khớp SDK đã cài.

### Node.js & npm

- Angular 22 yêu cầu Node `^22.22.3 || ^24.15.0 || ^26.0.0`
- Kiểm tra: `node --version` và `npm --version`
- `.\scripts\quality.ps1` fail sớm nếu Node nằm ngoài engines range

### SQL Server

- Development connection string: `src/EnglishTestWeb.Api/appsettings.Development.json`
- Mặc định: `Server=localhost;Database=EnglishTestWeb_Dev;Trusted_Connection=True;TrustServerCertificate=True`

### Protected storage & Data Protection

- Protected files: `%LOCALAPPDATA%\EnglishTestWeb\protected-storage` (ngoài repo, ngoài `wwwroot`)
- Data Protection keys: `%LOCALAPPDATA%\EnglishTestWeb\data-protection-keys`
- Chi tiết deploy: [../deploy/storage-and-data-protection.md](../deploy/storage-and-data-protection.md)

## First-time setup

```powershell
# API
dotnet restore EnglishTestWeb.sln
dotnet build EnglishTestWeb.sln

# EF CLI (local tool manifest)
dotnet tool restore

# Apply Identity baseline migration (cần SQL Server)
dotnet ef database update --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj

# Seed roles (tùy chọn, idempotent)
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj -- --seed-identity-roles

# Angular
cd src/EnglishTestWeb.Client
npm install
npm run build
npm test -- --watch=false
```

### Migration troubleshooting

| Triệu chứng | Nguyên nhân thường gặp | Cách xử lý |
|-------------|------------------------|------------|
| `dotnet-ef` command not found | Chưa restore local tool | Chạy `dotnet tool restore` |
| Cannot open database / login failed | SQL Server chưa chạy hoặc connection string sai | Kiểm tra instance `localhost`, bật SQL Server, sửa `appsettings.Development.json` |
| A network-related error occurred | Instance name/port không đúng | Xác nhận SQL Server Browser/TCP; thử `(localdb)\MSSQLLocalDB` nếu dùng LocalDB |
| Build succeeded nhưng migration timeout | Firewall hoặc quyền DB user | Tạo database `EnglishTestWeb_Dev` trước hoặc dùng account có quyền `dbcreator` |

`.\scripts\quality.ps1` **không** cần SQL Server — API tests dùng in-memory database.

## Local run

```powershell
# Terminal 1 — API (http://localhost:5124)
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj --launch-profile http

# Terminal 2 — Angular dev server (proxy /api → API)
cd src/EnglishTestWeb.Client
npm start
```

Angular `proxy.conf.json` forward `/api` tới `http://localhost:5124`.

**Quan trọng:** Dùng launch profile `http` khi dev với Angular proxy. Profile `https` bật redirect 307 sang `https://localhost:7204` — browser sẽ gọi thẳng HTTPS, mất cookie/XSRF same-origin và login trả `400`.

Nếu bắt buộc chạy HTTPS profile, cập nhật `proxy.conf.json` target thành `https://localhost:7204` và `secure: true`.

## Smoke / quality gate

```powershell
.\scripts\quality.ps1
```

Script sẽ dừng với thông báo rõ nếu thiếu SDK 10.0.x, Node engines không hợp lệ, hoặc build/test smoke fail.

## CI

GitHub Actions workflow: `.github/workflows/quality.yml` — chạy cùng smoke sequence trên push/PR.

## Auth & security baseline

- Same-origin cookie auth (ASP.NET Core Identity) — không lưu token trong `localStorage`/`sessionStorage`
- XSRF: cookie `XSRF-TOKEN`, header `X-XSRF-TOKEN` cho unsafe API methods
- Lỗi API: `ProblemDetails` với `code` / `extensions.code` ổn định

## Dev teacher login (Story 1.2)

Development seed tạo teacher user idempotent khi `Identity:SeedDevTeacherOnStartup` = `true` (mặc định trong `appsettings.Development.json`).

| Field | Value |
|-------|-------|
| Email | `teacher@englishtestweb.local` |
| Username | `teacher` |
| Password | `Teacher123!` |

Seed thủ công (sau migration + roles):

```powershell
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj -- --seed-dev-teacher
```

### Teacher routes (Angular)

| Route | Mô tả |
|-------|--------|
| `/login` | Teacher login |
| `/forgot-password` | Placeholder quên mật khẩu |
| `/teacher/dashboard` | Dashboard placeholder |
| `/teacher/library` | Placeholder Thư viện đề |
| `/teacher/classes` | Roster lớp + danh sách học sinh (Story 1.3) |
| `/teacher/results` | Placeholder Kết quả |

## Dev student class flow (Story 1.3)

MVP demo seed tạo teacher + student + class + membership idempotent khi `Identity:SeedMvpDemoOnStartup` = `true`.

| Field | Value |
|-------|-------|
| Student email | `student@englishtestweb.local` |
| Student username | `student` |
| Student password | `Student123!` |
| Class name | `English 7A` |
| Class code | `ENG7A` |

Seed thủ công (sau migration + roles):

```powershell
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj -- --seed-mvp-demo
```

### Student routes (Angular)

| Route | Mô tả |
|-------|--------|
| `/class` | Nhập mã lớp + xác nhận |
| `/student/login` | Đăng nhập học sinh với class context |
| `/student/tests` | Assigned tests placeholder |

### Class flow smoke (local)

1. Chạy API + Angular dev server.
2. Mở `http://localhost:4200/class`.
3. Nhập mã `ENG7A`, xác nhận lớp, đăng nhập student credentials.
4. Xác nhận redirect tới `/student/tests` với tên lớp hiển thị.
5. Teacher: đăng nhập `/login`, mở `/teacher/classes` — thấy mã lớp và học sinh seeded.

API endpoints: `GET /api/classes/by-code/{code}`, `GET /api/classes`, `GET /api/classes/{id}`, `GET /api/classes/current`, `POST /api/auth/student/login`.

## Server-side class context (Story 1.4)

Student session gắn claim `etw:active_class_id` trong cookie (session-only, không persist DB). Server revalidate membership trên mỗi request quan trọng:

| Endpoint | Hành vi |
|----------|---------|
| `GET /api/auth/me` | Trả `activeClass` chỉ khi Student có claim hợp lệ **và** membership còn `active` |
| `GET /api/classes/current` | Student-only; 404 `classes.notFound` nếu claim missing/stale/revoked |

### Authorization smoke (local)

1. Login student qua flow `/class` → `/student/login` → `/student/tests`.
2. `GET /api/auth/me` (cookie session) — thấy `activeClass` với `ENG7A`.
3. Teacher khác truy cập `GET /api/classes/{id}` lớp không sở hữu — nhận 404 `classes.notFound` (hidden).
4. Student gọi `GET /api/auth/teacher/ping` — 403 `auth.forbidden`.
