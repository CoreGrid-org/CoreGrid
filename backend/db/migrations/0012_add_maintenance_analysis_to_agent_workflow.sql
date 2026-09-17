START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917083415_AddMaintenanceAnalysisToAgentWorkflow') THEN
    ALTER TABLE "AgentWorkflows" ADD "MaintenanceAnalysis" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917083415_AddMaintenanceAnalysisToAgentWorkflow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917083415_AddMaintenanceAnalysisToAgentWorkflow', '10.0.10');
    END IF;
END $EF$;
COMMIT;

