# Database Artifacts

Per SRS §2.3 (C-02), schema is managed **exclusively by EF Core migrations** (`backend/Migrations/`, C#) — that's the source of truth, and `dotnet ef database update` is the only supported way to apply schema changes.

The files in this folder are **generated exports** for readability/review, not a second migration mechanism:

- `db/migrations/NNNN_description.sql` — one numbered, idempotent SQL script per EF Core migration.
- `db/schema.sql` — the full current schema, start to finish.

Regenerate them after adding a migration — don't hand-edit them, they'll just be overwritten:

```bash
cd backend
dotnet ef migrations script <previous-migration-or-0> <new-migration-name> --idempotent -o db/migrations/000N_description.sql
dotnet ef migrations script --idempotent -o db/schema.sql
```

So far this covers only `Organizations` and `Users` (SRS §4.2, §4.7, §8.2) — the tenant mirror and local identity mirror created by the setup flow.
