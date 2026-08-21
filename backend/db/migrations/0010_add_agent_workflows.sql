START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE TABLE "AgentWorkflows" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "Objective" text NOT NULL,
        "Status" integer NOT NULL,
        "Plan" jsonb,
        "AgentOutputs" jsonb,
        "ToolCalls" jsonb,
        "ValidationResult" jsonb,
        "Recommendation" text,
        "IsHighImpact" boolean NOT NULL,
        "ApprovalStatus" integer NOT NULL,
        "RevisionCount" integer NOT NULL,
        "FailureReason" text,
        "CorrelationId" text NOT NULL,
        "InitiatedByUserId" uuid NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AgentWorkflows" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AgentWorkflows_Recommendation" CHECK ("Recommendation" IS NULL OR "Recommendation" IN ('REPAIR','REPLACE','TRANSFER','DISPOSE','RETAIN')),
        CONSTRAINT "FK_AgentWorkflows_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AgentWorkflows_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AgentWorkflows_Users_InitiatedByUserId" FOREIGN KEY ("InitiatedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE TABLE "AgentApprovals" (
        "Id" uuid NOT NULL,
        "WorkflowId" uuid NOT NULL,
        "Decision" text NOT NULL,
        "DecidedByUserId" uuid NOT NULL,
        "Reason" text NOT NULL,
        "WorkflowSnapshot" jsonb,
        "DecidedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AgentApprovals" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AgentApprovals_Decision" CHECK ("Decision" IN ('APPROVE','REJECT','REVISE')),
        CONSTRAINT "FK_AgentApprovals_AgentWorkflows_WorkflowId" FOREIGN KEY ("WorkflowId") REFERENCES "AgentWorkflows" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AgentApprovals_Users_DecidedByUserId" FOREIGN KEY ("DecidedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE TABLE "AgentExecutionSteps" (
        "Id" uuid NOT NULL,
        "WorkflowId" uuid NOT NULL,
        "Agent" text NOT NULL,
        "Sequence" integer NOT NULL,
        "InputHash" text,
        "OutputSummary" text,
        "DurationMs" integer,
        "Status" text NOT NULL,
        "Error" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AgentExecutionSteps" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AgentExecutionSteps_AgentWorkflows_WorkflowId" FOREIGN KEY ("WorkflowId") REFERENCES "AgentWorkflows" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE INDEX "IX_AgentApprovals_DecidedByUserId" ON "AgentApprovals" ("DecidedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE INDEX "IX_AgentApprovals_WorkflowId" ON "AgentApprovals" ("WorkflowId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE INDEX "IX_AgentExecutionSteps_WorkflowId" ON "AgentExecutionSteps" ("WorkflowId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE INDEX "IX_AgentWorkflows_AssetId" ON "AgentWorkflows" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE INDEX "IX_AgentWorkflows_CorrelationId" ON "AgentWorkflows" ("CorrelationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE INDEX "IX_AgentWorkflows_InitiatedByUserId" ON "AgentWorkflows" ("InitiatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE INDEX "IX_AgentWorkflows_OrganizationId" ON "AgentWorkflows" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    CREATE INDEX "IX_AgentWorkflows_Status" ON "AgentWorkflows" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821105606_AddAgentWorkflows') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260821105606_AddAgentWorkflows', '10.0.10');
    END IF;
END $EF$;
COMMIT;

