# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TVS-App is a service order management system for TVS Eletrônica. It consists of:
- **`Src/TVSApp.Api`** — ASP.NET Core 9 Web API (Minimal APIs + SignalR)
- **`Src/TVSApp.Web`** — Blazor WebAssembly frontend (MudBlazor + Tailwind CSS)
- **`Src/TVSApp.Application`** — CQRS handlers and requests via MediatR
- **`Src/TVSApp.Domain`** — Entities, repository interfaces, domain exceptions
- **`Src/TVSApp.Infrastructure`** — EF Core (SQL Server), repositories, external services
- **`Src/TVSApp.Tests`** — MSTest unit tests

## Commands

### .NET (build, run, test, migrations)

```bash
# Build entire solution
dotnet build

# Run the API
dotnet run --project Src/TVSApp.Api

# Run the Blazor frontend
dotnet run --project Src/TVSApp.Web

# Run tests
dotnet test

# Add EF migration
dotnet ef migrations add <MigrationName> --project Src/TVSApp.Infrastructure --startup-project Src/TVSApp.Api

# Apply migrations
dotnet ef database update --project Src/TVSApp.Infrastructure --startup-project Src/TVSApp.Api
```

### Frontend CSS (Tailwind — run from `Src/TVSApp.Web`)

```bash
npm install
npm run build   # One-time build → wwwroot/css/theme.min.css
npm run watch   # Watch mode → wwwroot/css/theme.css
```

### Docker

```bash
docker-compose up   # Starts SQL Server (1433) + API (5001)
```

## Architecture

### CQRS with MediatR

All business logic flows through MediatR. Each operation has a **Request** (`Src/TVSApp.Application/Requests/`) and a corresponding **Handler** (`Src/TVSApp.Application/Handlers/`). Repositories are split into command repos (writes) and query repos (reads) under `Src/TVSApp.Infrastructure/Repositories/`.

### API Layer (Minimal APIs)

`Src/TVSApp.Api/Endpoints/` contains endpoint registration files (e.g. `ServiceOrderEndpoints.cs`). Dependency injection is wired in `Common/BuilderExtensions.cs` and middleware/routing in `Common/AppExtensions.cs`.

### Blazor Frontend

Pages live in `Src/TVSApp.Web/Pages/`. HTTP calls to the API go through handler classes in `Src/TVSApp.Web/Handlers/`. Auth is managed via `CustomAuthStateProvider.cs` (JWT stored in Blazored.LocalStorage) with `AuthorizationMessageHandler.cs` attaching tokens to requests. Real-time updates come via SignalR hub at `/osHub`.

### Background Services & Queues

WhatsApp (Evolution API) and email notifications are dispatched asynchronously via in-memory queues in `Src/TVSApp.Infrastructure/Messaging/Queue/` processed by background workers in `Messaging/BackgroundWorker/`.

### External Services

- **PDF generation** — QuestPDF (`Infrastructure/Services/QuestPDFs/`)
- **QR codes** — QRCoder (`Infrastructure/Services/GenerateQrCodeService.cs`)
- **Address lookup** — zipcode API (`Infrastructure/Services/AddressService/`)
- **WhatsApp** — Evolution API (`Infrastructure/Services/Whatsapp/`)
- **Email** — SMTP (`Infrastructure/Services/Email/`)

## Key Configuration

`Src/TVSApp.Api/appsettings.json` contains JWT settings, CORS origins, database connection string, SMTP, and Evolution API (WhatsApp) credentials. Development overrides go in `appsettings.Development.json`.

Production API: `https://gerenciamentotvsapi-f7e9dyhyfbhrbpa8.brazilsouth-01.azurewebsites.net/`  
Swagger (local): `http://localhost:5001/swagger`
