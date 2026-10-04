START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004084249_AgentWorkflowAssetTypeScope') THEN
    ALTER TABLE "AgentWorkflows" ALTER COLUMN "AssetId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004084249_AgentWorkflowAssetTypeScope') THEN
    ALTER TABLE "AgentWorkflows" ADD "AssetTypeId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004084249_AgentWorkflowAssetTypeScope') THEN
    UPDATE "AgentWorkflows" w SET "AssetTypeId" = a."AssetTypeId" FROM "Assets" a WHERE a."Id" = w."AssetId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004084249_AgentWorkflowAssetTypeScope') THEN
    CREATE INDEX "IX_AgentWorkflows_AssetTypeId" ON "AgentWorkflows" ("AssetTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004084249_AgentWorkflowAssetTypeScope') THEN
    ALTER TABLE "AgentWorkflows" ADD CONSTRAINT "FK_AgentWorkflows_AssetTypes_AssetTypeId" FOREIGN KEY ("AssetTypeId") REFERENCES "AssetTypes" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004084249_AgentWorkflowAssetTypeScope') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261004084249_AgentWorkflowAssetTypeScope', '10.0.10');
    END IF;
END $EF$;
COMMIT;

