START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE TABLE "VerificationCampaigns" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "Name" text NOT NULL,
        "PeriodStart" date NOT NULL,
        "PeriodEnd" date NOT NULL,
        "ScopeDepartmentId" uuid,
        "ScopeLocationId" uuid,
        "ScopeAssetCategoryId" uuid,
        "ScopeAssetTypeId" uuid,
        "Status" integer NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VerificationCampaigns" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VerificationCampaigns_AssetCategories_ScopeAssetCategoryId" FOREIGN KEY ("ScopeAssetCategoryId") REFERENCES "AssetCategories" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationCampaigns_AssetTypes_ScopeAssetTypeId" FOREIGN KEY ("ScopeAssetTypeId") REFERENCES "AssetTypes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationCampaigns_Departments_ScopeDepartmentId" FOREIGN KEY ("ScopeDepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationCampaigns_Locations_ScopeLocationId" FOREIGN KEY ("ScopeLocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationCampaigns_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationCampaigns_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE TABLE "VerificationTasks" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "CampaignId" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "AssignedToUserId" uuid,
        "DueDate" date NOT NULL,
        "Status" integer NOT NULL,
        "AssertedPresent" boolean,
        "AssertedLocationId" uuid,
        "AssertedCondition" text,
        "CompletedByUserId" uuid,
        "CompletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VerificationTasks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VerificationTasks_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationTasks_Locations_AssertedLocationId" FOREIGN KEY ("AssertedLocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationTasks_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationTasks_Users_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationTasks_Users_CompletedByUserId" FOREIGN KEY ("CompletedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VerificationTasks_VerificationCampaigns_CampaignId" FOREIGN KEY ("CampaignId") REFERENCES "VerificationCampaigns" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE TABLE "Discrepancies" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "CampaignId" uuid NOT NULL,
        "VerificationTaskId" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "IsAutomatic" boolean NOT NULL,
        "RaisedByUserId" uuid,
        "Description" text NOT NULL,
        "PhotoUrl" text,
        "Status" integer NOT NULL,
        "ResolutionType" text,
        "ResolutionExplanation" text,
        "CorrectiveAction" text,
        "RegisterCorrected" boolean NOT NULL,
        "ResolvedByUserId" uuid,
        "ResolvedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Discrepancies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Discrepancies_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Discrepancies_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Discrepancies_Users_RaisedByUserId" FOREIGN KEY ("RaisedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Discrepancies_Users_ResolvedByUserId" FOREIGN KEY ("ResolvedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Discrepancies_VerificationCampaigns_CampaignId" FOREIGN KEY ("CampaignId") REFERENCES "VerificationCampaigns" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Discrepancies_VerificationTasks_VerificationTaskId" FOREIGN KEY ("VerificationTaskId") REFERENCES "VerificationTasks" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_Discrepancies_AssetId" ON "Discrepancies" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_Discrepancies_CampaignId" ON "Discrepancies" ("CampaignId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_Discrepancies_OrganizationId" ON "Discrepancies" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_Discrepancies_RaisedByUserId" ON "Discrepancies" ("RaisedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_Discrepancies_ResolvedByUserId" ON "Discrepancies" ("ResolvedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_Discrepancies_Status" ON "Discrepancies" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_Discrepancies_Type" ON "Discrepancies" ("Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_Discrepancies_VerificationTaskId" ON "Discrepancies" ("VerificationTaskId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationCampaigns_CreatedByUserId" ON "VerificationCampaigns" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationCampaigns_OrganizationId" ON "VerificationCampaigns" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationCampaigns_ScopeAssetCategoryId" ON "VerificationCampaigns" ("ScopeAssetCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationCampaigns_ScopeAssetTypeId" ON "VerificationCampaigns" ("ScopeAssetTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationCampaigns_ScopeDepartmentId" ON "VerificationCampaigns" ("ScopeDepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationCampaigns_ScopeLocationId" ON "VerificationCampaigns" ("ScopeLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationCampaigns_Status" ON "VerificationCampaigns" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationTasks_AssertedLocationId" ON "VerificationTasks" ("AssertedLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationTasks_AssetId" ON "VerificationTasks" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationTasks_AssignedToUserId" ON "VerificationTasks" ("AssignedToUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationTasks_CampaignId" ON "VerificationTasks" ("CampaignId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationTasks_CompletedByUserId" ON "VerificationTasks" ("CompletedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationTasks_OrganizationId" ON "VerificationTasks" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    CREATE INDEX "IX_VerificationTasks_Status" ON "VerificationTasks" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815032307_AddVerificationAndDiscrepancies') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815032307_AddVerificationAndDiscrepancies', '10.0.10');
    END IF;
END $EF$;
COMMIT;

