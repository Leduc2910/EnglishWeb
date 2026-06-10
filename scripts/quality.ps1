# EnglishTestWeb local quality gate — API + Angular smoke
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Fail([string]$Message) {
    Write-Error $Message
    exit 1
}

function Invoke-CheckedCommand {
    param(
        [string]$Label,
        [scriptblock]$Command
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        Fail $Label
    }
}

function Test-DotNetSdkVersion {
    param([string]$Version)

    if ($Version -notmatch '^10\.0\.') {
        Fail "dotnet SDK must be 10.0.x (global.json pins 10.0.202 with rollForward: latestFeature). Installed: $Version"
    }
}

function Test-NodeEnginesVersion {
    param([string]$Version)

    if ($Version -notmatch '^v(\d+)\.(\d+)\.(\d+)$') {
        Fail "Unable to parse node version '$Version'. Expected format v22.22.3."
    }

    $major = [int]$Matches[1]
    $minor = [int]$Matches[2]
    $patch = [int]$Matches[3]

    $supported = (
        ($major -eq 22 -and ($minor -gt 22 -or ($minor -eq 22 -and $patch -ge 3))) -or
        ($major -eq 24 -and ($minor -gt 15 -or ($minor -eq 15 -and $patch -ge 0))) -or
        ($major -ge 26)
    )

    if (-not $supported) {
        Fail "Node $Version is outside package.json engines (^22.22.3 || ^24.15.0 || ^26.0.0)."
    }
}

Write-Host "== EnglishTestWeb quality gate ==" -ForegroundColor Cyan

Push-Location $repoRoot
try {
    Write-Host "`n[1/4] Checking .NET SDK..." -ForegroundColor Yellow
    try {
        $dotnetVersion = (dotnet --version).Trim()
    }
    catch {
        Fail "dotnet CLI not found. Install .NET SDK 10.0.x from https://dotnet.microsoft.com/download/dotnet/10.0"
    }

    Test-DotNetSdkVersion -Version $dotnetVersion
    Write-Host "dotnet --version => $dotnetVersion"

    Write-Host "`n[2/4] API build + test..." -ForegroundColor Yellow
    Invoke-CheckedCommand "dotnet build failed. Verify global.json SDK pin and installed SDK feature band." {
        dotnet build "$repoRoot\EnglishTestWeb.sln"
    }

    Invoke-CheckedCommand "dotnet test failed. API tests use an in-memory database and do not require SQL Server. Check build output above for compile or test assertion failures." {
        dotnet test "$repoRoot\tests\EnglishTestWeb.Api.Tests\EnglishTestWeb.Api.Tests.csproj" --no-build
    }

    Write-Host "`n[3/4] Checking Node.js..." -ForegroundColor Yellow
    $clientRoot = Join-Path $repoRoot "src\EnglishTestWeb.Client"
    if (-not (Test-Path $clientRoot)) {
        Fail "Client project path not found: src/EnglishTestWeb.Client"
    }

    try {
        $nodeVersion = (node --version).Trim()
    }
    catch {
        Fail "node not found. Install Node.js ^22.22.3 || ^24.15.0 || ^26.0.0 for Angular 22."
    }

    Test-NodeEnginesVersion -Version $nodeVersion
    Write-Host "node --version => $nodeVersion"

    try {
        $npmVersion = (npm --version).Trim()
    }
    catch {
        Fail "npm not found. Install npm with Node.js."
    }

    Write-Host "npm --version => $npmVersion"

    Push-Location $clientRoot
    try {
        Write-Host "`n[4/4] Angular install + build + test..." -ForegroundColor Yellow
        Invoke-CheckedCommand "npm install failed. Check Node version against package.json engines." {
            npm install
        }

        Invoke-CheckedCommand "npm run build failed." {
            npm run build
        }

        Invoke-CheckedCommand "npm test failed." {
            npm test -- --watch=false
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "`nQuality gate passed." -ForegroundColor Green
}
finally {
    Pop-Location
}
