START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809130913_RemoveOrganizationExternalId') THEN
    DROP INDEX "IX_Organizations_ExternalOrgId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809130913_RemoveOrganizationExternalId') THEN
    ALTER TABLE "Organizations" DROP COLUMN "ExternalOrgId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809130913_RemoveOrganizationExternalId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260809130913_RemoveOrganizationExternalId', '10.0.10');
    END IF;
END $EF$;
COMMIT;

