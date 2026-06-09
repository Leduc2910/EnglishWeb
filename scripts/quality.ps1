# EnglishTestWeb local quality gate — API + Angular smoke
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Fail([string]$Message) {
    Write-Error $Message
    exit 1
}

Write-Host "== EnglishTestWeb quality gate ==" -ForegroundColor Cyan

Push-Location $repoRoot
try {
    Write-Host "`n[1/4] Checking .NET SDK..." -ForegroundColor Yellow
    $dotnetVersion = (& dotnet --version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        Fail "dotnet CLI not found. Install .NET SDK 10.0.x from https://dotnet.microsoft.com/download/dotnet/10.0"
    }
    Write-Host "dotnet --version => $dotnetVersion"

    Write-Host "`n[2/4] API build + test..." -ForegroundColor Yellow
    dotnet build "$repoRoot\EnglishTestWeb.sln"
    if ($LASTEXITCODE -ne 0) {
        Fail "dotnet build failed. Verify global.json SDK pin and installed SDK feature band."
    }

    dotnet test "$repoRoot\tests\EnglishTestWeb.Api.Tests\EnglishTestWeb.Api.Tests.csproj" --no-build
    if ($LASTEXITCODE -ne 0) {
        Fail "dotnet test failed."
    }

    Write-Host "`n[3/4] Checking Node.js..." -ForegroundColor Yellow
    $nodeVersion = (& node --version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        Fail "node not found. Install Node.js ^22.22.3 || ^24.15.0 || ^26.0.0 for Angular 22."
    }
    Write-Host "node --version => $nodeVersion"

    $clientRoot = Join-Path $repoRoot "src\EnglishTestWeb.Client"
    Push-Location $clientRoot
    try {
        Write-Host "`n[4/4] Angular install + build + test..." -ForegroundColor Yellow
        npm install
        if ($LASTEXITCODE -ne 0) {
            Fail "npm install failed. Check Node version against package.json engines."
        }

        npm run build
        if ($LASTEXITCODE -ne 0) {
            Fail "npm run build failed."
        }

        npm test -- --watch=false
        if ($LASTEXITCODE -ne 0) {
            Fail "npm test failed."
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
