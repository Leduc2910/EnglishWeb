# EnglishTestWeb

Nền tảng khảo thí tiếng Anh cho giáo viên và học sinh — baseline .NET 10 Web API + Angular 22 SPA.

## Yêu cầu nhanh

| Thành phần | Phiên bản |
|------------|-----------|
| .NET SDK | 10.0.x (`global.json` pin `10.0.202`, `rollForward: latestFeature`) |
| Node.js | `^22.22.3 \|\| ^24.15.0 \|\| ^26.0.0` (Angular 22) |
| SQL Server | LocalDB hoặc SQL Server instance |
| npm | Đi kèm Node |

Chi tiết setup, migration, storage, và smoke commands: [docs/setup/development.md](docs/setup/development.md)

## Quality gate

```powershell
.\scripts\quality.ps1
```

Chạy API build/test smoke và Angular build/test smoke. Workspace hiện chưa có Git/CI — script local là handoff gate bắt buộc trước feature stories.

## Cấu trúc

- `src/EnglishTestWeb.Api` — ASP.NET Core Web API (.NET 10)
- `src/EnglishTestWeb.Client` — Angular 22 standalone strict SPA
- `tests/EnglishTestWeb.Api.Tests` — API integration/unit tests
- `_bmad-output/` — BMad planning & sprint artifacts (không phải runtime)
