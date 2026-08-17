START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817022114_AddIsActiveToAssetCategoryTypeAttribute') THEN
    ALTER TABLE "AssetTypes" ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817022114_AddIsActiveToAssetCategoryTypeAttribute') THEN
    ALTER TABLE "AssetCategories" ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817022114_AddIsActiveToAssetCategoryTypeAttribute') THEN
    ALTER TABLE "AssetAttributeDefinitions" ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817022114_AddIsActiveToAssetCategoryTypeAttribute') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260817022114_AddIsActiveToAssetCategoryTypeAttribute', '10.0.10');
    END IF;
END $EF$;
COMMIT;

