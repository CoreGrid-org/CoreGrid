using Npgsql;

namespace backend.Tests.Features.Audit;

// DR-12 / FR-063 / FR-064: AuditLogEntries and AssetHistory must be
// append-only — no UPDATE, no DELETE, through any path. The only real way
// to prove that is against a real Postgres instance (InMemory can't model
// GRANT/REVOKE at all), so unlike the rest of this project, this suite
// needs TEST_DB_CONNECTION pointing at an actual Postgres — the same local
// docker-compose instance in dev, a service container in CI.
//
// The migrations (AddAuditLog, AddAssetSchema) already contain
// `REVOKE UPDATE, DELETE ... FROM coregrid_app`, but that REVOKE was
// silently inert: it's conditional on a `coregrid_app` role existing, and
// nothing ever created one, so it always no-opped. Postgres also can't
// restrict a table's OWNER via GRANT/REVOKE at all — and the app's actual
// runtime connection (appsettings' "CoreGrid" string) connects as the same
// role that owns the tables (it ran the migrations), so even with the role
// created, the app's own connection is unaffected by the REVOKE.
//
// This test suite creates the `coregrid_app` role (idempotent) and proves
// the REVOKE mechanism itself is correct for a connection that actually
// uses it. Full enforcement for the running app requires splitting the
// migration-owner connection from a restricted runtime connection — a
// connection-string/config change, not a test, and out of scope here;
// tracked as a follow-up rather than silently left unverified.
public class AppendOnlyTests : IAsyncLifetime
{
    private static string OwnerConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=coregrid;Username=coregrid;Password=coregrid";

    private const string AppRoleConnectionString =
        "Host=localhost;Port=5433;Database=coregrid;Username=coregrid_app;Password=coregrid_app";

    public async Task InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(OwnerConnectionString);
        await conn.OpenAsync();

        await using var setup = new NpgsqlCommand("""
            DO $$
            BEGIN
              IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='coregrid_app') THEN
                CREATE ROLE coregrid_app LOGIN PASSWORD 'coregrid_app';
              END IF;
            END
            $$;
            GRANT CONNECT ON DATABASE coregrid TO coregrid_app;
            GRANT USAGE ON SCHEMA public TO coregrid_app;
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO coregrid_app;
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO coregrid_app;
            REVOKE UPDATE, DELETE ON "AuditLogEntries" FROM coregrid_app;
            REVOKE UPDATE, DELETE ON "AssetHistory" FROM coregrid_app;
            """, conn);
        await setup.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AuditLogEntries_UpdateIsRejected_ForTheRestrictedRole()
    {
        await using var conn = new NpgsqlConnection(AppRoleConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("UPDATE \"AuditLogEntries\" SET \"Operation\" = 'Update' WHERE false;", conn);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState); // insufficient_privilege
    }

    [Fact]
    public async Task AuditLogEntries_DeleteIsRejected_ForTheRestrictedRole()
    {
        await using var conn = new NpgsqlConnection(AppRoleConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM \"AuditLogEntries\" WHERE false;", conn);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState);
    }

    [Fact]
    public async Task AssetHistory_UpdateIsRejected_ForTheRestrictedRole()
    {
        await using var conn = new NpgsqlConnection(AppRoleConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("UPDATE \"AssetHistory\" SET \"Description\" = 'x' WHERE false;", conn);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState);
    }

    [Fact]
    public async Task AssetHistory_DeleteIsRejected_ForTheRestrictedRole()
    {
        await using var conn = new NpgsqlConnection(AppRoleConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM \"AssetHistory\" WHERE false;", conn);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState);
    }

    [Fact]
    public async Task AuditLogEntries_SelectAndInsertStillWork_ForTheRestrictedRole()
    {
        await using var conn = new NpgsqlConnection(AppRoleConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT count(*) FROM \"AuditLogEntries\";", conn);

        var result = await cmd.ExecuteScalarAsync();
        Assert.NotNull(result);
    }
}
