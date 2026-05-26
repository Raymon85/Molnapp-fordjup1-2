# ☁️ CloudSoft Recruitment Portal

> **Inlämningsuppgift 1 & 2 – Molnapplikationer fördjupningskurs**  
> Rayan Monfared | Maj 2026

En containerbaserad rekryteringsapplikation byggd med ASP.NET Core 10, driftsatt på **Azure Container Apps** via en fullständig CI/CD-pipeline med GitHub Actions.

---

## 🚀 Funktioner

- 📋 **Jobblistning** – arbetsgivare publicerar annonser, kandidater söker jobb
- 📎 **CV-uppladdning** – via `multipart/form-data`, lagrat i Azure Blob Storage
- 🔐 **Rollbaserad autentisering** – ASP.NET Core Identity (Admin / Kandidat)
- 🛡️ **REST API** – skyddat med API-nyckel i `X-Api-Key`-header
- 📖 **Swagger UI** – OpenAPI-dokumentation på `/swagger` via Scalar
- 💓 **Djupa health probes** – databas + blob storage kontrolleras på `/healthz`
- 📊 **Strukturerad loggning** – Application Insights med sökbara log-properties

---

## 🏗️ Teknisk stack

| Komponent | Teknologi |
|-----------|-----------|
| Web-ramverk | ASP.NET Core 10 MVC + Razor Pages |
| Databas (dev) | SQLite – ingen installation behövs |
| Databas (prod) | Azure SQL / SQL Server 2022 |
| Fillagring (dev) | Azurite (Azure Storage-emulator) |
| Fillagring (prod) | Azure Blob Storage via Managed Identity |
| Autentisering | ASP.NET Core Identity, cookie-baserad |
| ORM | Entity Framework Core 10 |
| API-dokumentation | OpenAPI + Scalar UI |
| Loggning | Application Insights |
| Container | Docker – multi-stage build |
| CI/CD | GitHub Actions + Azure Container Registry |
| Hosting | Azure Container Apps |

---

## 🔄 Miljöer

### 🖥️ Inner loop – lokal körning utan Docker

```bash
cd RecruitmentPortal
dotnet run
```

Använder **SQLite** och **Azurite** automatiskt via `ASPNETCORE_ENVIRONMENT=Development`. Ingen Docker behövs.

### 🐳 Docker Compose – komplett lokal miljö

```bash
docker compose up --build
```

Startar tre containers: applikationen, **SQL Server 2022** och **Azurite**. Applikationen finns på `http://localhost:8080`.

### ☁️ Outer loop – Azure Container Apps

Varje push till `master` triggar GitHub Actions-pipelinen:

```
git push → build → test → docker push (ACR) → deploy (Container Apps) → healthz verify
```

---

## 📁 Projektstruktur

```
├── .github/
│   └── workflows/
│       └── deploy.yml          # CI/CD pipeline
├── RecruitmentPortal/
│   ├── Controllers/
│   │   ├── Api/
│   │   │   └── JobsApiController.cs   # REST API
│   │   ├── AdminController.cs
│   │   ├── HomeController.cs
│   │   └── JobsController.cs
│   ├── DTOs/                   # JobPostingDto, CreateJobDto m.fl.
│   ├── HealthChecks/
│   │   └── BlobStorageHealthCheck.cs
│   ├── Middleware/
│   │   └── ApiKeyMiddleware.cs
│   ├── Services/
│   │   └── BlobStorageService.cs
│   ├── Repositories/           # Repository-pattern för databas
│   ├── Data/                   # EF Core DbContext + Migrations
│   ├── Models/                 # Domänmodeller
│   ├── Program.cs
│   ├── Dockerfile
│   └── appsettings.json
├── docker-compose.yml
└── README.md
```

---

## 🌐 API-endpoints

Alla `/api/*`-rutter kräver headern `X-Api-Key`.

| Metod | URL | Beskrivning |
|-------|-----|-------------|
| `GET` | `/api/jobs` | Hämta alla aktiva jobbannonser |
| `GET` | `/api/jobs/{id}` | Hämta en specifik jobbannons |
| `POST` | `/api/jobs` | Skapa ny jobbannons |
| `PUT` | `/api/jobs/{id}` | Uppdatera jobbannons |
| `DELETE` | `/api/jobs/{id}` | Ta bort jobbannons |

📖 Fullständig dokumentation finns på `/swagger` i driftsatt miljö.

---

## 💓 Health Check

```bash
curl https://<app-url>/healthz
```

```json
{
  "status": "Healthy",
  "entries": {
    "database":     { "status": "Healthy" },
    "blob-storage": { "status": "Healthy" }
  }
}
```

---

## 🔐 Säkerhet

- ✅ **OIDC** – GitHub Actions loggar in till Azure utan lagrade lösenord
- ✅ **Managed Identity** – Container App kommunicerar med Blob Storage utan credentials i koden
- ✅ **API-nyckel** – middleware skyddar alla `/api/*`-rutter
- ✅ **Private blob container** – inga CV-filer är publikt åtkomliga
- ✅ **Non-root container** – `aspnet:10.0`-imagen kör som användare `app` (uid 1654)

---

## ⚙️ GitHub Secrets som krävs

| Secret | Syfte |
|--------|-------|
| `AZURE_CLIENT_ID` | App Registration för OIDC |
| `AZURE_TENANT_ID` | Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | Azure-prenumeration |
| `ACR_LOGIN_SERVER` | Container Registry URL |
| `CONTAINER_APP` | Namn på Container App-resursen |
| `RESOURCE_GROUP` | Namn på Resource Group |

---

## 👤 Författare

**Rayan Monfared** – Molnapplikationer fördjupningskurs, Maj 2026
