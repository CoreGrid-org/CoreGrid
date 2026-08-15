START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    ALTER TABLE "Users" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    ALTER TABLE "Organizations" ADD "CreatedBy" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    ALTER TABLE "Organizations" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    ALTER TABLE "Organizations" ADD "UpdatedBy" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "AssetCategories" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "Code" character varying(20) NOT NULL,
        "Name" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_AssetCategories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssetCategories_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "Departments" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "Code" character varying(20) NOT NULL,
        "Name" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_Departments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Departments_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "AssetTypes" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "AssetCategoryId" uuid NOT NULL,
        "Code" character varying(20) NOT NULL,
        "Name" text NOT NULL,
        "UsefulLifeYears" integer NOT NULL,
        "DefaultMaintenanceIntervalDays" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_AssetTypes" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AssetTypes_UsefulLifeYears" CHECK ("UsefulLifeYears" > 0),
        CONSTRAINT "FK_AssetTypes_AssetCategories_AssetCategoryId" FOREIGN KEY ("AssetCategoryId") REFERENCES "AssetCategories" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AssetTypes_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "Locations" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Type" character varying(30) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_Locations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Locations_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Locations_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "AssetAttributeDefinitions" (
        "Id" uuid NOT NULL,
        "AssetTypeId" uuid NOT NULL,
        "Name" text NOT NULL,
        "DataType" character varying(10) NOT NULL,
        "IsRequired" boolean NOT NULL,
        "ValidationRule" text,
        "SelectOptions" jsonb,
        "DisplayOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_AssetAttributeDefinitions" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AssetAttributeDefinitions_DataType" CHECK ("DataType" IN ('TEXT','NUMBER','DATE','BOOLEAN','SELECT')),
        CONSTRAINT "CK_AssetAttributeDefinitions_SelectOptions" CHECK ("DataType" <> 'SELECT' OR "SelectOptions" IS NOT NULL),
        CONSTRAINT "FK_AssetAttributeDefinitions_AssetTypes_AssetTypeId" FOREIGN KEY ("AssetTypeId") REFERENCES "AssetTypes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "OrganizationPolicies" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "AssetTypeId" uuid,
        "RepairToReplaceCostThreshold" numeric(5,2) NOT NULL,
        "MinimumServiceLifeYears" numeric(5,2) NOT NULL,
        "MaxAcceptableFailureFrequency" numeric(5,2) NOT NULL,
        "ValuationValidityWindowDays" integer NOT NULL,
        "ConfidenceFloor" numeric(5,2) NOT NULL,
        "CostVarianceTolerancePercent" numeric(5,2) NOT NULL,
        "OutstandingTransferDays" integer NOT NULL,
        "ApprovalOverduePeriodHours" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_OrganizationPolicies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrganizationPolicies_AssetTypes_AssetTypeId" FOREIGN KEY ("AssetTypeId") REFERENCES "AssetTypes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_OrganizationPolicies_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "Assets" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "AssetTypeId" uuid NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "LocationId" uuid NOT NULL,
        "AssetCode" character varying(40) NOT NULL,
        "Name" text NOT NULL,
        "Status" character varying(20) NOT NULL,
        "Condition" character varying(15) NOT NULL,
        "AcquisitionDate" date NOT NULL,
        "AcquisitionCost" numeric(18,2) NOT NULL,
        "ResidualValue" numeric(18,2) NOT NULL,
        "CumulativeMaintenanceCost" numeric(18,2) NOT NULL,
        "RepairCount" integer NOT NULL,
        "LastRepairDate" date,
        "QrPayload" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_Assets" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Assets_AcquisitionCost" CHECK ("AcquisitionCost" >= 0),
        CONSTRAINT "CK_Assets_Condition" CHECK ("Condition" IN ('NEW','GOOD','FAIR','POOR','UNSERVICEABLE')),
        CONSTRAINT "CK_Assets_Status" CHECK ("Status" IN ('ACTIVE','UNDER_MAINTENANCE','TRANSFER_REQUESTED','IN_TRANSIT','CONDEMNED','DISPOSAL_REQUESTED','DISPOSED')),
        CONSTRAINT "FK_Assets_AssetTypes_AssetTypeId" FOREIGN KEY ("AssetTypeId") REFERENCES "AssetTypes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Assets_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Assets_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Assets_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "AssetAttributeValues" (
        "Id" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "AssetAttributeDefinitionId" uuid NOT NULL,
        "ValueText" text,
        "ValueNumber" numeric(18,4),
        "ValueDate" date,
        "ValueBoolean" boolean,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_AssetAttributeValues" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AssetAttributeValues_ExactlyOneValue" CHECK (num_nonnulls("ValueText", "ValueNumber", "ValueDate", "ValueBoolean") = 1),
        CONSTRAINT "FK_AssetAttributeValues_AssetAttributeDefinitions_AssetAttribu~" FOREIGN KEY ("AssetAttributeDefinitionId") REFERENCES "AssetAttributeDefinitions" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AssetAttributeValues_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE TABLE "AssetHistory" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "ActorUserId" uuid,
        "EventType" character varying(30) NOT NULL,
        "Description" text NOT NULL,
        "PreviousValue" jsonb,
        "NewValue" jsonb,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AssetHistory" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AssetHistory_EventType" CHECK ("EventType" IN ('STATUS_CHANGE','FIELD_AMENDMENT','VERIFICATION','MAINTENANCE','TRANSFER','DISPOSAL','AGENT_RECOMMENDATION')),
        CONSTRAINT "FK_AssetHistory_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AssetHistory_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AssetHistory_Users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Users_DepartmentId" ON "Users" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetAttributeDefinitions_AssetTypeId" ON "AssetAttributeDefinitions" ("AssetTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE UNIQUE INDEX "IX_AssetAttributeDefinitions_AssetTypeId_Name" ON "AssetAttributeDefinitions" ("AssetTypeId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetAttributeValues_AssetAttributeDefinitionId_ValueNumber" ON "AssetAttributeValues" ("AssetAttributeDefinitionId", "ValueNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetAttributeValues_AssetAttributeDefinitionId_ValueText" ON "AssetAttributeValues" ("AssetAttributeDefinitionId", "ValueText");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE UNIQUE INDEX "IX_AssetAttributeValues_AssetId_AssetAttributeDefinitionId" ON "AssetAttributeValues" ("AssetId", "AssetAttributeDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetCategories_OrganizationId" ON "AssetCategories" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE UNIQUE INDEX "IX_AssetCategories_OrganizationId_Code" ON "AssetCategories" ("OrganizationId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetHistory_ActorUserId" ON "AssetHistory" ("ActorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetHistory_AssetId" ON "AssetHistory" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetHistory_CreatedAt" ON "AssetHistory" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetHistory_OrganizationId" ON "AssetHistory" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Assets_AssetTypeId" ON "Assets" ("AssetTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Assets_Condition" ON "Assets" ("Condition");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Assets_DepartmentId" ON "Assets" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Assets_LocationId" ON "Assets" ("LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Assets_Name" ON "Assets" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Assets_OrganizationId" ON "Assets" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE UNIQUE INDEX "IX_Assets_OrganizationId_AssetCode" ON "Assets" ("OrganizationId", "AssetCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Assets_Status" ON "Assets" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetTypes_AssetCategoryId" ON "AssetTypes" ("AssetCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_AssetTypes_OrganizationId" ON "AssetTypes" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE UNIQUE INDEX "IX_AssetTypes_OrganizationId_Code" ON "AssetTypes" ("OrganizationId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Departments_OrganizationId" ON "Departments" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE UNIQUE INDEX "IX_Departments_OrganizationId_Code" ON "Departments" ("OrganizationId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Locations_DepartmentId" ON "Locations" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_Locations_OrganizationId" ON "Locations" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_OrganizationPolicies_AssetTypeId" ON "OrganizationPolicies" ("AssetTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE INDEX "IX_OrganizationPolicies_OrganizationId" ON "OrganizationPolicies" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    CREATE UNIQUE INDEX "IX_OrganizationPolicies_OrganizationId_AssetTypeId" ON "OrganizationPolicies" ("OrganizationId", "AssetTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    DO $$ BEGIN IF EXISTS (SELECT FROM pg_roles WHERE rolname='coregrid_app') THEN EXECUTE 'REVOKE UPDATE, DELETE ON "AssetHistory" FROM coregrid_app'; END IF; END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814023105_AddAssetSchema') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260814023105_AddAssetSchema', '10.0.10');
    END IF;
END $EF$;
COMMIT;

