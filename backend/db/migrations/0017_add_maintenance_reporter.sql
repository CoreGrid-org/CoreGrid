START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925090000_AddMaintenanceReporter') THEN
    ALTER TABLE "MaintenanceRecords" ADD "ReportedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925090000_AddMaintenanceReporter') THEN
    CREATE INDEX "IX_MaintenanceRecords_ReportedByUserId" ON "MaintenanceRecords" ("ReportedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925090000_AddMaintenanceReporter') THEN
    ALTER TABLE "MaintenanceRecords" ADD CONSTRAINT "FK_MaintenanceRecords_Users_ReportedByUserId" FOREIGN KEY ("ReportedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925090000_AddMaintenanceReporter') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260925090000_AddMaintenanceReporter', '10.0.10');
    END IF;
END $EF$;
COMMIT;

