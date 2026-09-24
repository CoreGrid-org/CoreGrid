START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260919141251_AllowAdHocDiscrepancies') THEN
    ALTER TABLE "Discrepancies" ALTER COLUMN "VerificationTaskId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260919141251_AllowAdHocDiscrepancies') THEN
    ALTER TABLE "Discrepancies" ALTER COLUMN "CampaignId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260919141251_AllowAdHocDiscrepancies') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260919141251_AllowAdHocDiscrepancies', '10.0.10');
    END IF;
END $EF$;
COMMIT;

