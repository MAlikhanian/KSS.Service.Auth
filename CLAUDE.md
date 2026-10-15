# KSS Service Auth – Claude Code Instructions

## Architecture Rules

### No Unauthorized Commits (CRITICAL)
- **NEVER create git commits unless the user explicitly asks**
- Do not commit after completing tasks, fixing bugs, or making changes
- Wait for the user to say "commit" before running any git commit command

### No Commits, No Image Builds — Compile-Check Only (CRITICAL — TOP PRIORITY)
These are hard guardrails and override any inferred follow-up, "while I'm here" cleanup, or perceived readiness.
- **Do NOT commit any change to git.** Never run `git commit` or create a commit — only the user commits.
- **Do NOT build or push any container/Docker image on changes, and do NOT deploy.** Never run `docker build` / `docker buildx build` / `docker push`, the `Helper/deployment/*` build scripts, or `kubectl rollout restart`/`apply` — even when the work looks ready.
- **You may build ONLY to verify there are no code/compile errors** (e.g. `dotnet build`, `npm run build` / `next build`, `tsc`). Such a build is strictly to confirm the code compiles — it must NEVER produce or push an image, commit, or deploy.

### No Unauthorized Installations (CRITICAL)
- **NEVER install any application, package, or tool without explicit user confirmation first**
- Always ask before running `npm install`, `dotnet add`, `pip install`, or any package manager command
- Always ask before downloading binaries, browser engines, or system tools

### Show Plan, Get Approval, Then Act (CRITICAL)
- **NEVER make code changes, file edits, installs, or run commands without first showing a plan and receiving explicit approval**
- Workflow: (1) propose the plan, (2) wait for user approval ("yes" / "go" / "do it"), (3) only then execute
- Showing a sample of code or output is NOT a request to implement it — assume it's information unless the user says to act on it
- This applies to proactive improvements, inferred follow-ups, and "while I'm here" cleanups — they all need approval first
- Once approved, scope is exactly what was approved — no extra changes

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

### Timestamp Convention (CRITICAL)
- **`CreatedAt` and `UpdatedAt` MUST be set in C# backend code, NEVER by the frontend or SQL defaults**
- `MainDbContext` overrides `SaveChanges`/`SaveChangesAsync` with a `SetTimestamps()` method that automatically:
  - On **Add**: sets `CreatedAt = DateTime.UtcNow` and `UpdatedAt = DateTime.UtcNow`
  - On **Update**: sets `UpdatedAt = DateTime.UtcNow`
- This applies to ALL entities that have `CreatedAt`/`UpdatedAt` properties — no per-entity code needed
- Do NOT use `HasDefaultValueSql("GETUTCDATE()")` in EF configurations for timestamps
- Do NOT send `createdAt`/`updatedAt` from the frontend — the backend handles it
- All timestamps are stored as UTC (`DateTime.UtcNow`)

### CLAUDE.md Sync Rule (CRITICAL)
- **When this CLAUDE.md file is updated, the same changes MUST be copied to ALL other CLAUDE.md files** across the workspace
- All 13 repos share the same architecture rules: `KSS.Service.Company`, `KSS.Service.Auth`, `KSS.Service.Person`, `KSS.Service.Common`, `KSS.Service.SEBA_ERP_Members`, `KSS.Service.Exchange`, `KSS.Service.FileOrchestrator`, `KSS.Service.FileStorage`, `KSS.Service.MarketData`, `KSS.Service.MarketHistory`, `KSS.Service.Portfolio`, `KSS.Common`, `KSS.Client.Web`
- Only the `## Database` section and the title may differ per repo — all other rules must stay identical

### GUID Generation (CRITICAL)
- **ALL GUIDs MUST be version 7 (UUIDv7)** — time-ordered for index locality.
- **NEVER generate a GUID on the frontend.** No `uuidv4()`, no `crypto.randomUUID()` in any page, component, BFF route, or `services/*` helper. The frontend MUST NOT send entity ids when creating records.
- **ALL id generation happens in the C# backend.** Preferred: leave the entity `Id` as `Guid.Empty`; the shared `MainDbContext` `SaveChanges`/`SaveChangesAsync` override (`ApplyEntityDefaults()`) stamps any empty `Id` with `Guid.CreateVersion7()`.
- When the id is needed *before* save (e.g. to link child rows in the same transaction), generate it explicitly with **`Guid.CreateVersion7()`**.
- **NEVER use `Guid.NewGuid()`** — it produces a v4 GUID. Replace every occurrence with `Guid.CreateVersion7()` (.NET 9+).
- Entities keep `[DatabaseGenerated(DatabaseGeneratedOption.None)]` — the app supplies the v7 id, not SQL Server.

### Non-GUID Primary Keys / Lookup Tables (CRITICAL)
- Lookup / reference tables (small fixed value sets — e.g. `*Type`, `*Label`, `*Status`) do **NOT** use a GUID primary key. They keep a small integer key (`tinyint` / `int`).
- For these tables the `Id` is owned by the **database side**, never the backend. The backend MUST NOT set or generate the `Id` — no `Guid.CreateVersion7()`, no manual value. Map the entity `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` so EF leaves the `Id` to the database.
- The backend generates **GUIDs only** (v7, for entities whose primary key is a GUID — see GUID Generation above). It never produces the id for a non-GUID lookup table.

### DTO Per Operation (CRITICAL)
- **One DTO per operation** — never reuse a single DTO for create + update + read.
- **`*InsertDto`** — create. Contains NO backend-generated GUID (no own id, no FK to a row created in the same call). Reference GUIDs to existing rows are allowed, in the body.
- **`*UpdateDto`** — update. The id + only editable fields — never audit/computed fields (the backend owns `CreatedAt`/`UpdatedAt`).
- **`*ViewDto`** — read. The full shape (id, audit dates, computed/nested fields).
- **Delete** takes only the key (id), not a full DTO.
- Fill the `BaseService<TEntity, TAddDto, TUpdateDto, TListDto>` slots with DISTINCT DTOs — never the same DTO in all three.

### Service-to-Service URL Configuration (CRITICAL)
- Cross-service HTTP calls MUST read the target base URL from a **nested** config key: `Services:<TargetName>:BaseUrl` (e.g. `Services:Person:BaseUrl`, `Services:Company:BaseUrl`). Do NOT use the flat `Services:<Name>Url` form.
- The value MUST point to the target’s **Kubernetes Service** DNS, not the Deployment/pod name: `http://<target>-service-service.<target-namespace>.svc.cluster.local` (default port 80 — do NOT append `:8000`, that is the pod’s container port). Dev: `http://localhost:<targetDevPort>`.
- Wire it with a typed client — `AddHttpClient<IXxxApiClient, XxxApiClient>(c => c.BaseAddress = new Uri(configuration["Services:Xxx:BaseUrl"]))` — that forwards the caller’s Bearer token via a DelegatingHandler. Cross-service calls MUST degrade gracefully (do NOT use `EnsureSuccessStatusCode()` that turns a downstream error into an HTTP 500).

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
