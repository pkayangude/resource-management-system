# Resource Management System

[![CI/CD](https://github.com/YOUR-ORG/resource-management/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/YOUR-ORG/resource-management/actions/workflows/ci-cd.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Frontend-Blazor%20WASM-512BD4)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)

A complete **Resource Management System** built with .NET 8 (ASP.NET Core API + Blazor WebAssembly), featuring all modules required for IBM GDC operations.

---

## 📦 Solution Structure

```
ResourceManagement/
├── src/
│   ├── ResourceManagement.Core/          # Domain entities, interfaces, DTOs
│   ├── ResourceManagement.Infrastructure/ # EF Core, repositories, services
│   ├── ResourceManagement.API/           # ASP.NET Core 8 REST API
│   └── ResourceManagement.Web/           # Blazor WebAssembly frontend
├── tests/
│   └── ResourceManagement.Tests/         # xUnit unit tests
├── Template/                             # Excel templates (existing)
├── .github/
│   └── workflows/
│       ├── ci-cd.yml                     # CI/CD pipeline
│       └── release.yml                   # GitHub Release workflow
└── ResourceManagement.sln
```

---

## 🏗️ Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core 8 Web API |
| Frontend | Blazor WebAssembly (.NET 8) + MudBlazor |
| Database | SQL Server (EF Core 8 Code-First) |
| Excel Processing | ClosedXML + ExcelDataReader |
| Logging | Serilog (structured JSON) |
| CI/CD | GitHub Actions |
| Container | Red Hat UBI9 (IBM security compliant) |
| Testing | xUnit + Moq + FluentAssertions |

---

## 🚀 Features

### 1. Resource Onboarding & Offboarding
- Single resource onboarding form
- One-time bulk upload via xlsx with **column mapping wizard** and **data preview**
- Offboarding with date tracking
- Resource movement history (Roll Off, Attrition, Transfer)

### 2. Forecast Allocation
- Auto-calculates: `Forecast Hours = Working Days × 9 hrs × FTE Fraction`
- Import annual Holiday List xlsx once per year → drives working day calculation
- Supports partial FTE (0.25, 0.5, 1.0)
- Monthly cost = Forecast Hours × Cost Rate (CAD)
- Forecast vs Actual variance report

### 3. ILC Labour Claim Validation
- Weekly xlsx upload with column mapping
- Validates:
  - Claims vs monthly forecast (warns if > 10% over)
  - Weekly claims vs 45-hr limit
  - Project/Demand budget not exceeded
- Per-claim status: ✅ Valid | ⚠️ Warning | ❌ Invalid
- Batch tracking per upload

### 4. Long Leave Management
- Leave types: Maternity, Paternity, Medical, Long Leave, Sabbatical
- Overlap detection
- Auto-calculates forecast impact hours (days × 9 hrs)
- Approve / Cancel workflow

### 5. Project & Demand Allocation
- Project budget hours tracking
- Per-resource project allocation with weekly hours + total budget
- Real-time budget consumption from ILC claims
- Over-budget alerts

### 6. Band Mix Calculator
- Weighted band mix: `BandMix = Σ(Weightage × FTE) / Total FTE`
- Band weightages: 4→4.5, 5→5.0, 6G→5.5, 6A→6.0, 6B→6.5, 7A→7.0, 7B→7.5, 8→8.0, 9→9.0
- Month-by-month calculation with persistence

### 7. Skill Matrix
- Per-resource skills with category, proficiency (1–4), years of experience, certifications
- Search by skill across the team

---

## 🛠️ Setup & Local Development

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server (LocalDB or full)
- Git

### 1. Clone Repository
```bash
git clone https://github.com/YOUR-ORG/resource-management.git
cd resource-management
```

### 2. Configure Database
Update `src/ResourceManagement.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ResourceManagement_Dev;Trusted_Connection=True"
  }
}
```

### 3. Run Migrations
```bash
cd src/ResourceManagement.API
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project ../ResourceManagement.Infrastructure
dotnet ef database update
```

### 4. Run the API
```bash
dotnet run --project src/ResourceManagement.API
# API available at https://localhost:7001
# Swagger UI at https://localhost:7001/swagger
```

### 5. Run the Web App
```bash
dotnet run --project src/ResourceManagement.Web
# App available at https://localhost:7174
```

---

## 📋 API Endpoints

| Module | Method | Endpoint | Description |
|--------|--------|----------|-------------|
| Resources | GET | `/api/resources` | List all resources |
| Resources | POST | `/api/resources` | Onboard resource |
| Resources | POST | `/api/resources/{id}/offboard` | Offboard resource |
| Import | POST | `/api/import/preview` | Preview xlsx for mapping |
| Import | POST | `/api/import/resources` | Bulk resource import |
| Forecast | GET | `/api/forecast/{year}/{month}` | Get forecast |
| Forecast | POST | `/api/forecast/generate/{year}/{month}` | Generate forecasts |
| Forecast | POST | `/api/forecast/import-holidays` | Import holiday xlsx |
| ILC | POST | `/api/ilc/upload` | Upload weekly ILC xlsx |
| ILC | POST | `/api/ilc/validate/{batchId}` | Validate ILC batch |
| Leave | POST | `/api/leave` | Create leave record |
| Leave | GET | `/api/leave/active` | Active leaves |
| Projects | GET | `/api/projects` | List projects/demands |
| Projects | POST | `/api/projects/{id}/allocate` | Allocate resource to project |
| BandMix | GET | `/api/bandmix/{year}/{month}` | Calculate band mix |
| SkillMatrix | GET | `/api/skillmatrix/skill/{name}` | Find by skill |
| Dashboard | GET | `/api/dashboard/summary` | KPI summary |

---

## 🧪 Running Tests
```bash
dotnet test tests/ResourceManagement.Tests --logger console
```

---

## 🔄 CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/ci-cd.yml`) runs:
1. **Build** – `dotnet build` (Release)
2. **Test** – xUnit with coverage
3. **Security** – CodeQL + `dotnet list package --vulnerable`
4. **Publish** – API + Web artifacts
5. **Docker** – Build & Trivy vulnerability scan
6. **Migration** – EF migration validation

On `git tag v*.*.*`, the release workflow creates a GitHub Release with deployment archives.

---

## 📊 Excel Template Reference

| Template | Purpose |
|----------|---------|
| `Template/Corporate-Persistent-BUD Team Forecast File_June_2026 V1.0.xlsx` | Annual forecast with monthly FTE allocations |
| `Template/Holiday List 2026.xlsx` | Annual holidays by location (import once/year) |
| `Template/Bandmix Calculator.xlsx` | Band mix reference |
| `Template/Resource Movements - Q22026 v0.xlsx` | Resource movement template |
| `Template/PIR Report - Template.xlsx` | ILC labour claim export format |

---

## 🔒 Security

- All secrets via environment variables (never hardcoded)
- Docker images from Red Hat UBI9 (non-root user)
- TLS enforced for API communication
- Structured logging without sensitive data exposure
- Input validation via FluentValidation
- Parameterized EF Core queries (no SQL injection)

---

## 📁 GitHub Repository Setup

```bash
git init
git add .
git commit -m "feat: initial Resource Management System"
git remote add origin https://github.com/YOUR-ORG/resource-management.git
git push -u origin main
```

Required GitHub Secrets (for production deployment):
- `SQL_CONNECTION_STRING` – production connection string
- `AZURE_WEBAPP_PUBLISH_PROFILE` – (if deploying to Azure App Service)
