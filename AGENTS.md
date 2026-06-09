# Agent Notes — EnglishTestWeb

## Stack

- API: `src/EnglishTestWeb.Api` — .NET 10, controllers, EF Core SQL Server, Identity cookie auth
- Client: `src/EnglishTestWeb.Client` — Angular 22 standalone strict, Vitest
- Tests: `tests/EnglishTestWeb.Api.Tests`

## Boundaries

- Controllers → Application → Domain; Infrastructure implements abstractions
- Không JWT/localStorage auth cho browser app
- Protected files qua `IFileStorage`, không public `wwwroot`
- Errors: `ProblemDetails` với stable `code`

## Before feature work

1. Đọc story file trong `_bmad-output/implementation-artifacts/`
2. Chạy `.\scripts\quality.ps1` — baseline phải pass
3. Setup chi tiết: `docs/setup/development.md`

## Sprint tracking

- Status: `_bmad-output/implementation-artifacts/sprint-status.yaml`
- Planning: `_bmad-output/planning-artifacts/`
