# Testing CineVault

## Unit and integration

```bash
./scripts/test.sh
```

```powershell
.\scripts\test.ps1
```

- `api/tests/CineVault.Api.Tests` boots the API with `WebApplicationFactory` on a temporary SQLite file. Migrations and the FTS5 index run for real. Tests cover auth, role checks, validation problems, search, paging, sorting, one-rating-per-member, and that edits keep ratings.
- `web/src/test` uses Vitest and Testing Library: the fetch client, the browse and movie pages, and the admin route guard.

CI also runs `dotnet ef migrations has-pending-model-changes`.

## End to end

Needs Docker and the Playwright CLI (`npm install -g playwright`).

```bash
./scripts/e2e.sh
```

It brings up `infra/docker-compose.yml` and runs `e2e/ui.spec.ts` against http://localhost:5173: guest search, a new member rating a movie, and an admin adding, editing, and deleting one.
