# KSS Service Auth – Claude Code Instructions

## Architecture Rules

### No Unauthorized Commits (CRITICAL)
- **NEVER create git commits unless the user explicitly asks**
- Do not commit after completing tasks, fixing bugs, or making changes
- Wait for the user to say "commit" before running any git commit command

### No Unauthorized Installations (CRITICAL)
- **NEVER install any application, package, or tool without explicit user confirmation first**
- Always ask before running `npm install`, `dotnet add`, `pip install`, or any package manager command
- Always ask before downloading binaries, browser engines, or system tools

### Database Connection String Convention (CRITICAL)
- **ALL services MUST use `ConnectionStrings["DefaultConnection"]`** to read the database connection string
- Code: `configuration.GetSection("ConnectionStrings")["DefaultConnection"]`
- Do NOT use service-specific keys like `KSSPerson`, `KSSAuth`, `KSSCompany` — each service should be a reusable template
- `appsettings.json` (prod) → `DefaultConnection` points to `*_Prod` database
- `appsettings.Development.json` (dev) → `DefaultConnection` points to `*_Dev` database
- This ensures dev mode always uses dev DB and prod mode always uses prod DB

### Backend-First Business Logic (CRITICAL)
- **ALL business logic MUST be implemented in the C# Service layer**
- The Next.js frontend (`KSS.Client.Web`) is a thin API caller only — it must NOT contain business logic
- Business logic such as syncing related tables, cascade operations, upsert decisions, etc. must live here in the Service classes

### Service Naming Convention (CRITICAL)

**Single-table services** — named `{TableName}Service.cs`:
- One service per database table, handles CRUD for that table only

**Multi-table orchestration services** — named `{Feature}ManagementService.cs`:
- When a task requires working with **more than one table**, do NOT put the logic in a single-table service
- Instead, create a new **ManagementService** that orchestrates across multiple tables
- The `ManagementService` suffix tells developers: "this service works with multiple tables"
- ManagementServices inject multiple repositories and/or other services

### Business Rule Responses (CRITICAL)
- **NEVER use `InvalidOperationException` for business rules** — business rules are NOT errors
- Use `throw new BusinessRuleException("message")` from `KSS.Helper` namespace
- The middleware catches `BusinessRuleException` and returns HTTP **400** with a clean JSON message
- The user sees the message as a notification, not as a scary error
- `InvalidOperationException` falls to the middleware default → HTTP **500** "unexpected error" — WRONG for business rules

### Service Layer Pattern
- Services inherit from `BaseService<TEntity, TAddDto, TUpdateDto, TListDto>`
- Override `AddDtoAsync`, `UpdateDto`, `Remove` to add custom business logic
- Inject additional repositories via constructor when cross-table operations are needed
- Use `SingleOrDefault` to check existence before add/update decisions

### CLAUDE.md Sync Rule (CRITICAL)
- **When this CLAUDE.md file is updated, the same changes MUST be copied to ALL other CLAUDE.md files** across the workspace
- All 8 repos share the same architecture rules: `KSS.Service.Company`, `KSS.Service.Auth`, `KSS.Service.Person`, `KSS.Service.Common`, `KSS.Service.SEBA_ERP_Members`, `KSS.Service.Exchange`, `KSS.Common`, `KSS.Client.Web`
- Only the `## Database` section and the title may differ per repo — all other rules must stay identical

## Database
- SQL Server: `KSS_Auth_Prod` / `KSS_Auth_Dev`
- Entity Framework Core with code-first approach
- `varchar` columns require `[Unicode(false)]` attribute on entity properties
- Always keep entities in sync with database schema

### Database Access via sqlcmd
Connection details (server, user, password) are in `appsettings.json` under `ConnectionStrings.DefaultConnection`. Read the password from there.

**IMPORTANT — password escaping**: The DB password contains special characters (`!`, `@`, `%`). Use **double quotes** around the `-P` value. Some queries intermittently fail with "Login failed" due to shell escaping — retry the same command.

```bash
"/c/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/sqlcmd" -S <server> -U <user> -P "<password>" -d <database> -I -Q "YOUR QUERY"
```

Always use `-I` flag (QUOTED_IDENTIFIER on) — required for tables with indexed views or computed columns.
