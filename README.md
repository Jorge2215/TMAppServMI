# TMAppServMI — Task Manager on Azure App Service with Managed Identity

A **Razor Pages** web application built with **.NET 10** that provides full CRUD task management backed by **Azure SQL Database**, with authentication via **ASP.NET Core Identity** and secure database connectivity demonstrated through **SQL Server Managed Identity** (`SUSER_SNAME()`).

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Data Model](#data-model)
- [Pages & Features](#pages--features)
- [Database & Migrations](#database--migrations)
- [Authentication](#authentication)
- [Configuration](#configuration)
- [Local Development Setup](#local-development-setup)
- [Deployment](#deployment)

---

## Overview

TMAppServMI (**T**ask **M**anager on **App Serv**ice with **M**anaged **I**dentity) is a reference application demonstrating:

- Secure access to **Azure SQL** using **Managed Identity** (no passwords stored in config).
- Task management (create, read, update, delete) via a clean Razor Pages UI.
- ASP.NET Core Identity for user registration and login.
- Entity Framework Core code-first migrations for schema management.

---

## Architecture

```
┌─────────────────────────────────────────────┐
│            Azure App Service                │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │   ASP.NET Core 10 Razor Pages App    │   │
│  │                                      │   │
│  │  Pages: Index | Create | Edit |      │   │
│  │         Delete | Privacy | Error     │   │
│  │                                      │   │
│  │  Identity: ASP.NET Core Identity     │   │
│  │  ORM:      Entity Framework Core 10  │   │
│  └────────────────┬─────────────────────┘   │
│                   │ Managed Identity         │
└───────────────────┼─────────────────────────┘
                    │
          ┌─────────▼──────────┐
          │   Azure SQL Server  │
          │   (AppDbContext)    │
          │   Tables:           │
          │   - Tasks           │
          └────────────────────┘
```

---

## Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET | 10.0 |
| Web Framework | ASP.NET Core Razor Pages | 10.0 |
| ORM | Entity Framework Core (SQL Server) | 10.0.5 |
| Identity | ASP.NET Core Identity + EF Store | 10.0.5 |
| Database | SQL Server / Azure SQL | — |
| Identity UI | Microsoft.AspNetCore.Identity.UI | 10.0.5 |
| EF Diagnostics | Microsoft.AspNetCore.Diagnostics.EF | 10.0.5 |

---

## Project Structure

```
TMAppServMI/
├── Data/
│   ├── ApplicationDbContext.cs       # EF Core DbContext (AppDbContext)
│   ├── TaskItem.cs                   # Task entity model
│   └── Migrations/
│       ├── 00000000000000_CreateIdentitySchema.cs   # Identity tables migration
│       └── 20260408010854_UpdateTaskModel.cs        # Tasks table migration
├── Pages/
│   ├── Index.cshtml(.cs)             # Task list + SQL identity query
│   ├── Create.cshtml(.cs)            # New task form
│   ├── Edit.cshtml(.cs)              # Edit existing task
│   ├── Delete.cshtml(.cs)            # Delete confirmation
│   ├── Privacy.cshtml(.cs)           # Privacy policy page
│   └── Error.cshtml(.cs)             # Error handling page
├── Program.cs                        # App bootstrap & DI configuration
├── TMAppServMI.csproj                # SDK-style project file (.NET 10)
└── appsettings.json                  # App configuration (connection string)
```

---

## Data Model

### `TaskItem`

```csharp
public class TaskItem
{
    public int Id { get; set; }           // Primary key (auto-increment)
    public string Title { get; set; }     // Task title (required)
    public bool IsCompleted { get; set; } // Completion status
    public DateTime CreatedAt { get; set; }// UTC creation timestamp
}
```

### `AppDbContext`

Extends `DbContext` and exposes:

```csharp
public DbSet<TaskItem> Tasks { get; set; }
```

> **Note:** Identity tables (`AspNetUsers`, `AspNetRoles`, etc.) were removed via migration `20260408010854_UpdateTaskModel`, simplifying the schema to task data only while retaining the `IdentityUser`-based authentication pipeline in `Program.cs`.

---

## Pages & Features

| Page | Route | HTTP Methods | Description |
|---|---|---|---|
| **Index** | `/` | GET | Lists all tasks ordered by creation date (descending). Also executes `SELECT SUSER_SNAME()` to display the current SQL identity (demonstrates Managed Identity). |
| **Create** | `/Create` | GET / POST | Form to create a new `TaskItem`. Sets `CreatedAt` to `DateTime.UtcNow` on submit. |
| **Edit** | `/Edit?id={id}` | GET / POST | Loads an existing task by ID and updates it. Handles `DbUpdateConcurrencyException`. |
| **Delete** | `/Delete?id={id}` | GET / POST | Confirmation page; removes the task from the database on POST. |
| **Privacy** | `/Privacy` | GET | Static privacy policy page. |
| **Error** | `/Error` | GET | Global error handler page. |

---

## Database & Migrations

Migrations are located in `Data/Migrations/` and managed with EF Core Tools.

### Apply migrations

```powershell
dotnet ef database update
```

### Add a new migration

```powershell
dotnet ef migrations add <MigrationName>
```

### Migration history

| Migration | Description |
|---|---|
| `00000000000000_CreateIdentitySchema` | Creates ASP.NET Core Identity tables |
| `20260408010854_UpdateTaskModel` | Adds `Tasks` table; removes unused Identity tables |

---

## Authentication

Authentication is configured in `Program.cs` using **ASP.NET Core Identity** with `IdentityUser`:

```csharp
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();
```

- Email confirmation is **required** before sign-in.
- Identity scaffolding is provided by `Microsoft.AspNetCore.Identity.UI`.

---

## Configuration

The application reads its SQL connection string from `appsettings.json` (or environment overrides):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<your-connection-string>"
  }
}
```

### Managed Identity (Azure)

When deployed to **Azure App Service** with a system-assigned or user-assigned Managed Identity, configure the connection string without a password:

```
Server=<sql-server>.database.windows.net;Database=<db>;Authentication=Active Directory Default;
```

No credentials need to be stored; the App Service Managed Identity is granted access on the SQL Server side with:

```sql
CREATE USER [<app-service-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<app-service-name>];
ALTER ROLE db_datawriter ADD MEMBER [<app-service-name>];
```

### User Secrets (Local Development)

The project uses **User Secrets** (`UserSecretsId: aspnet-TaskManager-04194df2-b567-43e0-a2ab-4e1e0ec3b8ce`) to store the local connection string safely:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local-connection-string>"
```

---

## Local Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local instance or Azure SQL)
- Visual Studio 2026 or VS Code

### Steps

1. **Clone the repository**

   ```powershell
   git clone https://github.com/Jorge2215/TMAppServMI
   cd TMAppServMI
   ```

2. **Set the connection string via User Secrets**

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=TaskManagerDb;Trusted_Connection=True;"
   ```

3. **Apply database migrations**

   ```powershell
   dotnet ef database update
   ```

4. **Run the application**

   ```powershell
   dotnet run
   ```

   The app will be available at `https://localhost:<port>`.

---

## Deployment

1. **Provision** an Azure App Service (Linux or Windows, .NET 10 stack).
2. **Provision** an Azure SQL Database and server.
3. **Enable Managed Identity** on the App Service.
4. **Grant SQL access** to the Managed Identity (see [Configuration](#configuration)).
5. **Set the connection string** in App Service → Configuration → Connection strings using `Active Directory Default` authentication.
6. **Deploy** via GitHub Actions, Visual Studio Publish, or `az webapp deploy`.

---
## Add App Service Managed Identity to SQL Server

CREATE USER TMAppServMI FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER TMAppServMI;
ALTER ROLE db_datawriter ADD MEMBER TMAppServMI;

## Verify SQL Identity
    SELECT SUSER_SNAME();



*Built with ❤️ by Jorgito — targeting .NET 10 on Azure App Service with Managed Identity.*
