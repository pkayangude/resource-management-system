# ─────────────────────────────────────────────────────────────────────────────
# git-setup.ps1
# Initialises Git, regenerates EF migration, stages everything, commits,
# creates GitHub repo (via gh CLI) and pushes.
#
# Usage (from project root PowerShell):
#   .\git-setup.ps1
# ─────────────────────────────────────────────────────────────────────────────

$GithubUser  = "pkayangude"
$RepoName    = "resource-management-system"
$BranchName  = "main"
$GitEmail    = "prashant.kayangude@gmail.com"
$GitName     = "pkayangude"
$PrivateRepo = $false   # set to $true for a private repo

$git = "C:\Program Files\Git\cmd\git.exe"
$ef  = "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe"

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "`n=== Resource Management System — Git & GitHub Setup ===" -ForegroundColor Cyan
Write-Host "User    : $GithubUser"
Write-Host "Repo    : $RepoName"
Write-Host "Email   : $GitEmail"
Write-Host "Branch  : $BranchName"
Write-Host ""

# ── 0. Pre-flight checks ──────────────────────────────────────────────────────
if (-not (Test-Path $git))  { throw "Git not found at $git. Install from https://git-scm.com" }
if (-not (Test-Path $ef))   { throw "dotnet-ef not found. Run: dotnet tool install -g dotnet-ef" }

# ── 1. Build solution ─────────────────────────────────────────────────────────
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build ResourceManagement.sln -c Debug --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "Build OK" -ForegroundColor Green

# ── 2. Regenerate EF migration (OneDrive deletes .cs files) ──────────────────
Write-Host "`nRegenerating EF migration..." -ForegroundColor Yellow
Remove-Item "src\ResourceManagement.Infrastructure\Migrations\*_InitialCreate.cs"       -Force -ErrorAction SilentlyContinue
Remove-Item "src\ResourceManagement.Infrastructure\Migrations\*_InitialCreate.Designer.cs" -Force -ErrorAction SilentlyContinue

& $ef migrations add InitialCreate `
    --project       src/ResourceManagement.Infrastructure `
    --startup-project src/ResourceManagement.API `
    --output-dir    Migrations `
    2>&1 | Where-Object { $_ -notmatch "^\[" }

if ($LASTEXITCODE -ne 0) { throw "EF migration generation failed" }

# Mark migration as already applied (DB tables exist from previous run)
$migId  = (Get-ChildItem "src\ResourceManagement.Infrastructure\Migrations\*_InitialCreate.cs" |
           Where-Object { $_.Name -notlike "*.Designer.cs" } | Select-Object -First 1).BaseName
$sqlcmd = (Get-ChildItem "C:\Program Files\Microsoft SQL Server" -Recurse -Filter "sqlcmd.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
$sql    = "IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId]='$migId') INSERT INTO [__EFMigrationsHistory]([MigrationId],[ProductVersion]) VALUES('$migId','8.0.11')"
& $sqlcmd -S ".\SQLEXPRESS" -d "ResourceManagement_Dev" -Q $sql -E -C | Out-Null
Write-Host "Migration $migId registered in DB." -ForegroundColor Green

# ── 3. Git init / identity ────────────────────────────────────────────────────
Write-Host "`nConfiguring git..." -ForegroundColor Yellow
if (-not (Test-Path ".git")) {
    & $git init -b $BranchName
}
& $git config user.email $GitEmail
& $git config user.name  $GitName
Write-Host "Identity: $GitName <$GitEmail>" -ForegroundColor Green

# ── 4. Stage all files ────────────────────────────────────────────────────────
Write-Host "`nStaging files..." -ForegroundColor Yellow
& $git add .
$staged = (& $git diff --cached --name-only).Count
Write-Host "$staged files staged." -ForegroundColor Green

# ── 5. Initial commit ─────────────────────────────────────────────────────────
Write-Host "`nCommitting..." -ForegroundColor Yellow
$msg = @"
feat: initial Resource Management System (v1.0)

Backend  : .NET 8 ASP.NET Core Web API
Frontend : Blazor WebAssembly (MudBlazor 7)
Database : SQL Server + EF Core 8 (11 tables)
CI/CD    : GitHub Actions (build, test, CodeQL, Docker, EF check)
Container: Red Hat UBI9, non-root USER 1001

Modules
- Onboarding / Offboarding (single form + bulk xlsx column-map wizard)
- Forecast Allocation (9 h/day x working days x FTE fraction)
- ILC Labour Claim Validation (weekly xlsx, forecast threshold, 45 h/week cap)
- Long Leave Management (overlap detection, forecast impact)
- Project & Demand Allocation (budget tracking, over-budget alerts)
- Band Mix Calculator (weighted FTE by band)
- Skill Matrix (proficiency 1-4, certifications, cross-team search)

Data seeded from IBM templates:
- 20 resources (PIR Report template)
- 4 projects  (P6TYQ, RARV6, RARV5, Cloud Migration Demand)
- 20 holidays (Holiday List 2026.xlsx, IBM India locations)
"@

& $git commit -m $msg
if ($LASTEXITCODE -ne 0) { throw "Commit failed" }
Write-Host "Commit created." -ForegroundColor Green

# ── 6. Create GitHub repo + push ──────────────────────────────────────────────
$ghAvailable = $null -ne (Get-Command "gh" -ErrorAction SilentlyContinue)

if ($ghAvailable) {
    Write-Host "`nCreating GitHub repo '$GithubUser/$RepoName'..." -ForegroundColor Yellow
    $vis = if ($PrivateRepo) { "--private" } else { "--public" }
    gh auth status 2>&1 | Select-Object -First 2
    gh repo create "$GithubUser/$RepoName" $vis `
        --source . `
        --remote origin `
        --push `
        --description "IBM GDC Resource Management System — .NET 8 + Blazor WASM"
    Write-Host "Pushed to https://github.com/$GithubUser/$RepoName" -ForegroundColor Green
} else {
    Write-Host "`n[!] GitHub CLI (gh) not found." -ForegroundColor Yellow
    Write-Host "    Option A — install gh CLI then re-run this script:" -ForegroundColor White
    Write-Host "      winget install --id GitHub.cli" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "    Option B — push manually:" -ForegroundColor White
    Write-Host "      1. Create repo at: https://github.com/new" -ForegroundColor White
    Write-Host "         Name: $RepoName   Visibility: $(if ($PrivateRepo){'Private'}else{'Public'})" -ForegroundColor White
    Write-Host "      2. Run these commands:" -ForegroundColor White
    Write-Host ""
    Write-Host "         & '$git' remote add origin https://github.com/$GithubUser/$RepoName.git" -ForegroundColor Cyan
    Write-Host "         & '$git' branch -M $BranchName" -ForegroundColor Cyan
    Write-Host "         & '$git' push -u origin $BranchName" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  After push, GitHub Actions CI/CD will trigger automatically." -ForegroundColor Green
}

Write-Host "`n=== Setup complete! ===" -ForegroundColor Green
Write-Host "Repo URL  : https://github.com/$GithubUser/$RepoName"
Write-Host "Actions   : https://github.com/$GithubUser/$RepoName/actions"
Write-Host "Tag release: & '$git' tag v1.0.0 ; & '$git' push origin v1.0.0"
