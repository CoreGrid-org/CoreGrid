START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE TABLE "AuditLogEntries" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "ActorUserId" uuid,
        "EntityType" character varying(60) NOT NULL,
        "EntityId" uuid,
        "Operation" character varying(10) NOT NULL,
        "Changes" jsonb,
        "CorrelationId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLogEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AuditLogEntries_Operation" CHECK ("Operation" IN ('Create','Update','Delete')),
        CONSTRAINT "FK_AuditLogEntries_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AuditLogEntries_Users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_ActorUserId" ON "AuditLogEntries" ("ActorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_CorrelationId" ON "AuditLogEntries" ("CorrelationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_CreatedAt" ON "AuditLogEntries" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_EntityType" ON "AuditLogEntries" ("EntityType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_OrganizationId" ON "AuditLogEntries" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    DO $$ BEGIN IF EXISTS (SELECT FROM pg_roles WHERE rolname='coregrid_app') THEN EXECUTE 'REVOKE UPDATE, DELETE ON "AuditLogEntries" FROM coregrid_app'; END IF; END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815031732_AddAuditLog', '10.0.10');
    END IF;
END $EF$;
COMMIT;

