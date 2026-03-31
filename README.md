# SmartSure — Insurance Management Platform

A full-stack microservices-based insurance management system built with Angular 19, ASP.NET Core 10, SQL Server, RabbitMQ, and MassTransit.

---

## Architecture Overview

```
Frontend (Angular 19) → API Gateway (Ocelot) → Microservices
                                                 ├── Identity API   (Auth, Users, JWT)
                                                 ├── Policy API     (Policies, Payments)
                                                 ├── Claims API     (Claims Management)
                                                 └── Admin API      (Dashboard, Reports, Audit)

Message Bus: RabbitMQ + MassTransit (event-driven communication between services)
```

---

## Prerequisites

Make sure the following are installed on your machine:

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org |
| SQL Server | 2019+ | https://www.microsoft.com/en-us/sql-server/sql-server-downloads |
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop |

---

## Service Ports

| Service | Port |
|---------|------|
| Frontend | http://localhost:4200 |
| API Gateway | http://localhost:5083 |
| Identity API | http://localhost:5001 |
| Claims API | http://localhost:5008 |
| Policy API | http://localhost:5152 |
| Admin API | http://localhost:5113 |
| RabbitMQ Management | http://localhost:15672 |

---

## Setup Guide

### Step 1 — Install Frontend Dependencies

```bash
cd frontend
npm install
cd ..
```

### Step 2 — Start RabbitMQ

Make sure Docker Desktop is running, then:

```bash
docker-compose up -d
```

This starts RabbitMQ with the management UI at http://localhost:15672
- Username: `smartsure`
- Password: `smartsure_dev`

### Step 3 — Create Databases

Open SQL Server Management Studio (SSMS) and create these 4 databases:

```sql
CREATE DATABASE SmartSure_IdentityDB;
CREATE DATABASE SmartSure_PolicyDB;
CREATE DATABASE SmartSure_ClaimsDB;
CREATE DATABASE SmartSure_AdminDB;
```

> Tables and schema are created automatically when services start (via EF Core migrations).

### Step 4 — Configure Credentials

Copy `.env.example` to `.env` in the repo root:

```bash
copy .env.example .env
```

Fill in your own credentials in `.env`:

```env
# RabbitMQ (keep as-is if using docker-compose defaults)
RabbitMQ__Host=localhost
RabbitMQ__Username=smartsure
RabbitMQ__Password=smartsure_dev

# JWT RSA Keys — generate using the GenKeys tool (see below)
JwtSettings__PrivateKeyContent=...
JwtSettings__PublicKeyContent=...

# Google OAuth (create at https://console.cloud.google.com)
GoogleAuth__ClientId=...
GoogleAuth__ClientSecret=...
GoogleAuth__RedirectUri=http://localhost:5001/api/auth/google/callback

# Gmail SMTP (use an App Password, not your real password)
MailSettings__UserName=your@gmail.com
MailSettings__Password=your-app-password

# MEGA Storage (https://mega.nz)
MegaOptions__Email=your@email.com
MegaOptions__Password=yourpassword
```

#### Generating RSA Keys

A key generation tool is included. Run it once:

```bash
dotnet run --project backend/tools/GenKeys/GenKeys.csproj
```

Copy the output private and public keys into your `.env` file.

### Step 5 — Run the Project

**Start all services:**

```powershell
.\start-all.ps1
```

If you get a script execution error, run this once first:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

**Stop all services:**

```powershell
.\stop-all.ps1
```

---

## Default Admin Account

After first run, a default admin account is seeded:

| Field | Value |
|-------|-------|
| Email | admin@smartsure.com |
| Password | Admin@123 |

---

## Project Structure

```
SmartSure/
├── frontend/                        # Angular 19 app
├── backend/
│   ├── gateway/                     # Ocelot API Gateway
│   ├── services/
│   │   ├── identity/                # Auth & user management
│   │   ├── claims/                  # Claims processing
│   │   ├── policy/                  # Policy & payments
│   │   └── admin/                   # Admin portal backend
│   ├── shared/                      # Shared libraries (JWT, MassTransit, Serilog)
│   └── tools/
│       └── GenKeys/                 # RSA key pair generator
├── .env.example                     # Credentials template
├── docker-compose.yml               # RabbitMQ container
├── start-all.ps1                    # Start all services
├── stop-all.ps1                     # Stop all services
└── create-zip.ps1                   # Create distributable zip
```

---

## Swagger / API Docs

Each service exposes Swagger UI in Development mode:

- Identity: http://localhost:5001/swagger
- Claims: http://localhost:5008/swagger
- Policy: http://localhost:5152/swagger
- Admin: http://localhost:5113/swagger

---

## Notes

- All services load credentials from the `.env` file at startup — never commit `.env` to version control.
- The `.env` file is excluded from the zip created by `create-zip.ps1`.
- RabbitMQ must be running before starting any backend service.
- SQL Server must be running and the 4 databases must exist before starting services.
