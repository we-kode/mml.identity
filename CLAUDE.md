# Identity Service

## Purpose

.NET 10 service responsible for admins, registered clients, authentication, authorization, and identity-related client administration. It is deployed as a Docker image and depends on the shared backend infrastructure.

## Solution structure

- `Identity.API` exposes ASP.NET Core controllers, contracts, middleware, filters, sockets, and configuration boundaries.
- `Identity.Application` contains application services, contracts, models, constants, and business types.
- `Identity.Infrastructure` contains external service integrations.
- `Identity.DBContext` owns EF Core models, extensions, and migrations.
- `Identity.CLI` provides administrative commands such as creating and managing admin users/clients.
- `IdentityService.Test` contains service tests.

Keep dependencies flowing toward application contracts. Database migrations belong in `Identity.DBContext`; do not modify an existing migration to change an already-deployed schema.

## Local commands

```bash
dotnet restore Identity.sln
dotnet build Identity.sln
dotnet test Identity.sln
docker compose --env-file dev.env up --build -d
```

Create local appsettings from the provided template and keep `dev.env`, secrets, certificates, and generated output out of commits. Preserve OpenIddict, token, authorization, and HTTPS behavior unless the change explicitly updates the documented security contract.

Update `mml.project/docs/setup/backend.mdx`, concept pages, or architecture documentation when identity configuration, endpoints, registration, authorization, or CLI operations change.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
