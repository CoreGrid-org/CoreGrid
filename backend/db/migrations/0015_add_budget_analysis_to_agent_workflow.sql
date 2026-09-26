START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260924053404_AddBudgetAnalysisToAgentWorkflow') THEN
    ALTER TABLE "AgentWorkflows" ADD "BudgetAnalysis" jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260924053404_AddBudgetAnalysisToAgentWorkflow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260924053404_AddBudgetAnalysisToAgentWorkflow', '10.0.10');
    END IF;
END $EF$;
COMMIT;

