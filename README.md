# LanguageLearning Backend

Production-oriented ASP.NET Core backend for a language learning service with JWT auth, RBAC, refresh token rotation, health checks, rate limiting, JSON logging, and EF Core migrations.

## What is implemented

- JWT access tokens plus persisted refresh tokens with rotation and revocation
- Roles and claims: `Admin`, `Editor`, `User`
- Auth endpoints: `register`, `login`, `refresh-token`, `logout`, `me`, `change-password`
- PBKDF2 password hashing with legacy SHA256 rehash support
- Login lockout and auth audit log storage
- Protected API endpoints with role-based authorization
- Health endpoints: `/health/live`, `/health/ready`
- CORS, fixed-window rate limiting, request telemetry, JSON console logging
- Environment-specific config for development, staging, and production

## Required configuration

The API now expects secrets through environment variables or user secrets.

Required values:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`

Recommended bootstrap values:

- `Auth__BootstrapFirstUserAsAdmin=true`
  Use this only for the initial admin bootstrap if production starts with an empty database.

Development example with user secrets:

```powershell
dotnet user-secrets --project API/API.csproj set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=LanguageLearningDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets --project API/API.csproj set "Jwt:Issuer" "LanguageLearning.API"
dotnet user-secrets --project API/API.csproj set "Jwt:Audience" "LanguageLearning.Client"
dotnet user-secrets --project API/API.csproj set "Jwt:SigningKey" "change-this-to-a-long-random-32-char-key"
```

## Run locally

```powershell
dotnet restore LanguageLerning.slnx
dotnet build LanguageLerning.slnx
dotnet run --project API/API.csproj
```

Development startup applies migrations and seeds demo content automatically. Production and staging do not.

## Database and migrations

Create a new migration:

```powershell
$env:ConnectionStrings__DefaultConnection='Server=(localdb)\\mssqllocaldb;Database=LanguageLearningDesignTime;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet ef migrations add <MigrationName> --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj --context LanguageLearningDbContext
```

Apply migrations manually:

```powershell
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj --context LanguageLearningDbContext
```

## API workflow

1. Register the first user.
2. If bootstrap is enabled and the database is empty, that user becomes `Admin`.
3. Login to get `accessToken` and `refreshToken`.
4. Use `Authorization: Bearer <accessToken>` for protected endpoints.
5. Rotate sessions with `/api/auth/refresh-token`.
6. Revoke the current session with `/api/auth/logout`.

Sample requests live in [API/API.http](/C:/Users/Roman/.codex/worktrees/7ae1/LanguageLerning/API/API.http).

## Verification

Build:

```powershell
dotnet build LanguageLerning.slnx -p:UseSharedCompilation=false
```

Run regression tests:

```powershell
dotnet run --project LanguageLerning.Tests/LanguageLerning.Tests.csproj
```

## Docker

Build:

```powershell
docker build -t languagelearning-api .
```

Run:

```powershell
docker run -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=LanguageLearningDb;User Id=sa;Password=StrongPwd123!;TrustServerCertificate=True;" `
  -e Jwt__Issuer="LanguageLearning.API" `
  -e Jwt__Audience="LanguageLearning.Client" `
  -e Jwt__SigningKey="change-this-to-a-long-random-32-char-key" `
  languagelearning-api
```

## Current gaps

- Email verification and password reset are scaffolded at the domain level but not exposed as full flows yet.
- Pagination/filtering/sorting, transactional idempotency, and richer SRS/content lifecycle remain follow-up work.
