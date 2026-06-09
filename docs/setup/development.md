# Development Setup — EnglishTestWeb

## Prerequisites

### .NET SDK

- Cài [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- Repo pin qua `global.json`: `10.0.202` với `rollForward: latestFeature`
- Khuyến nghị: SDK `10.0.300+` khi có sẵn trên máy dev
- Kiểm tra: `dotnet --version`

Nếu build báo thiếu SDK, cài đúng feature band hoặc nâng `global.json` cho khớp SDK đã cài.

### Node.js & npm

- Angular 22 yêu cầu Node `^22.22.3 || ^24.15.0 || ^26.0.0`
- Kiểm tra: `node --version` và `npm --version`
- Workspace hiện tại có thể báo cảnh báo engines nếu Node < `22.22.3` — nâng Node trước khi chạy Angular smoke

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

## Local run

```powershell
# Terminal 1 — API (https://localhost:7204)
dotnet run --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj

# Terminal 2 — Angular dev server (proxy /api → API)
cd src/EnglishTestWeb.Client
npm start
```

Angular `proxy.conf.json` forward `/api` tới `https://localhost:7204`.

## Smoke / quality gate

```powershell
.\scripts\quality.ps1
```

Script sẽ dừng với thông báo rõ nếu thiếu SDK, Node, hoặc SQL Server (khi test cần DB).

## Auth & security baseline

- Same-origin cookie auth (ASP.NET Core Identity) — không lưu token trong `localStorage`/`sessionStorage`
- XSRF: cookie `XSRF-TOKEN`, header `X-XSRF-TOKEN` cho unsafe API methods
- Lỗi API: `ProblemDetails` với `code` / `extensions.code` ổn định
