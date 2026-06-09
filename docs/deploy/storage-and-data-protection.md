# Storage & Data Protection

## Protected file storage

- Abstraction: `IFileStorage` (`Application/Files`)
- Implementation: `LocalProtectedFileStorage` (`Infrastructure/Storage`)
- Root path: cấu hình `ProtectedStorage:RootPath` trong `appsettings.json`
- Development mặc định: `%LOCALAPPDATA%\EnglishTestWeb\protected-storage`

Quy tắc:

- Không lưu upload runtime trong repo
- Không lưu dưới `src/EnglishTestWeb.Api/wwwroot`
- Storage key opaque — không ghép path từ user input
- Truy cập file chỉ qua API/service có authorization (không public static URL)

## Data Protection keys

- Cấu hình: `DataProtection:KeysPath`
- Development mặc định: `%LOCALAPPDATA%\EnglishTestWeb\data-protection-keys`
- Production: persist keys ngoài deployment package (filesystem, registry, hoặc secret store phù hợp host)

Mục tiêu: auth cookies và antiforgery tokens vẫn hợp lệ sau app restart/redeploy.

## Environment variables

Override bằng environment-specific settings hoặc biến môi trường ASP.NET Core:

- `ConnectionStrings__DefaultConnection`
- `ProtectedStorage__RootPath`
- `DataProtection__KeysPath`

Không commit secrets hoặc runtime upload paths vào source control.
