START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917090120_RenameMaintenancePhotoUrlToPhotoObjectKey') THEN
    ALTER TABLE "MaintenanceRecords" RENAME COLUMN "PhotoUrl" TO "PhotoObjectKey";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917090120_RenameMaintenancePhotoUrlToPhotoObjectKey') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917090120_RenameMaintenancePhotoUrlToPhotoObjectKey', '10.0.10');
    END IF;
END $EF$;
COMMIT;

