# KSS Service Auth – Claude Code Instructions

## Architecture Rules

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
