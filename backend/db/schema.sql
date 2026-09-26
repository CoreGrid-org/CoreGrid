CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809062238_InitialCreate') THEN
    CREATE TABLE "Organizations" (
        "Id" uuid NOT NULL,
        "ExternalOrgId" text NOT NULL,
        "Name" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809062238_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "ExternalSubjectId" text NOT NULL,
        "Email" text NOT NULL,
        "GivenName" text NOT NULL,
        "FamilyName" text NOT NULL,
        "Role" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Users_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809062238_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Organizations_ExternalOrgId" ON "Organizations" ("ExternalOrgId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809062238_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809062238_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_ExternalSubjectId" ON "Users" ("ExternalSubjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809062238_InitialCreate') THEN
    CREATE INDEX "IX_Users_OrganizationId" ON "Users" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809062238_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260809062238_InitialCreate', '10.0.10');
    END IF;
END $EF$;
COMMIT;

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

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE TABLE "AssetTransfers" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "FromDepartmentId" uuid NOT NULL,
        "ToDepartmentId" uuid NOT NULL,
        "FromLocationId" uuid NOT NULL,
        "ToLocationId" uuid NOT NULL,
        "InitiatedByUserId" uuid NOT NULL,
        "ApprovedByUserId" uuid,
        "ConfirmedByUserId" uuid,
        "Status" integer NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "ApprovedAt" timestamp with time zone,
        "ConfirmedAt" timestamp with time zone,
        "RejectionReason" text,
        CONSTRAINT "PK_AssetTransfers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssetTransfers_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AssetTransfers_Users_ApprovedByUserId" FOREIGN KEY ("ApprovedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AssetTransfers_Users_ConfirmedByUserId" FOREIGN KEY ("ConfirmedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AssetTransfers_Users_InitiatedByUserId" FOREIGN KEY ("InitiatedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE TABLE "DisposalRequests" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "InitiatedByUserId" uuid NOT NULL,
        "ApprovedByUserId" uuid,
        "DisposalMethod" integer NOT NULL,
        "EstimatedResidualValue" numeric(18,2) NOT NULL,
        "Status" integer NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "ApprovedAt" timestamp with time zone,
        "DisposedAt" timestamp with time zone,
        "Notes" text,
        CONSTRAINT "PK_DisposalRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DisposalRequests_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DisposalRequests_Users_ApprovedByUserId" FOREIGN KEY ("ApprovedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DisposalRequests_Users_InitiatedByUserId" FOREIGN KEY ("InitiatedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE INDEX "IX_AssetTransfers_ApprovedByUserId" ON "AssetTransfers" ("ApprovedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE INDEX "IX_AssetTransfers_ConfirmedByUserId" ON "AssetTransfers" ("ConfirmedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE INDEX "IX_AssetTransfers_InitiatedByUserId" ON "AssetTransfers" ("InitiatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE INDEX "IX_AssetTransfers_OrganizationId" ON "AssetTransfers" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE INDEX "IX_DisposalRequests_ApprovedByUserId" ON "DisposalRequests" ("ApprovedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE INDEX "IX_DisposalRequests_InitiatedByUserId" ON "DisposalRequests" ("InitiatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    CREATE INDEX "IX_DisposalRequests_OrganizationId" ON "DisposalRequests" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814175120_AddTransferAndDisposalEntities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260814175120_AddTransferAndDisposalEntities', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    CREATE INDEX "IX_DisposalRequests_AssetId" ON "DisposalRequests" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    CREATE INDEX "IX_AssetTransfers_AssetId" ON "AssetTransfers" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    CREATE INDEX "IX_AssetTransfers_FromDepartmentId" ON "AssetTransfers" ("FromDepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    CREATE INDEX "IX_AssetTransfers_FromLocationId" ON "AssetTransfers" ("FromLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    CREATE INDEX "IX_AssetTransfers_ToDepartmentId" ON "AssetTransfers" ("ToDepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    CREATE INDEX "IX_AssetTransfers_ToLocationId" ON "AssetTransfers" ("ToLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Departments_FromDepartmentId" FOREIGN KEY ("FromDepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Departments_ToDepartmentId" FOREIGN KEY ("ToDepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Locations_FromLocationId" FOREIGN KEY ("FromLocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Locations_ToLocationId" FOREIGN KEY ("ToLocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    ALTER TABLE "DisposalRequests" ADD CONSTRAINT "FK_DisposalRequests_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815024645_AddTransferDisposalForeignKeys') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815024645_AddTransferDisposalForeignKeys', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE TABLE "AuditLogEntries" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "ActorUserId" uuid,
        "EntityType" character varying(60) NOT NULL,
        "EntityId" uuid,
        "Operation" character varying(10) NOT NULL,
        "Changes" jsonb,
        "CorrelationId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLogEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AuditLogEntries_Operation" CHECK ("Operation" IN ('Create','Update','Delete')),
        CONSTRAINT "FK_AuditLogEntries_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AuditLogEntries_Users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_ActorUserId" ON "AuditLogEntries" ("ActorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_CorrelationId" ON "AuditLogEntries" ("CorrelationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_CreatedAt" ON "AuditLogEntries" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_EntityType" ON "AuditLogEntries" ("EntityType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLogEntries_OrganizationId" ON "AuditLogEntries" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    DO $$ BEGIN IF EXISTS (SELECT FROM pg_roles WHERE rolname='coregrid_app') THEN EXECUTE 'REVOKE UPDATE, DELETE ON "AuditLogEntries" FROM coregrid_app'; END IF; END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815031732_AddAuditLog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815031732_AddAuditLog', '10.0.10');
    END IF;
END $EF$;
COMMIT;

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

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817151743_AddMaintenanceRecord') THEN
    CREATE TABLE "MaintenanceRecords" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "Description" text NOT NULL,
        "ObservedCondition" character varying(15) NOT NULL,
        "PhotoUrl" character varying(500),
        "Type" character varying(20) NOT NULL,
        "Priority" character varying(20) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "EstimatedCost" numeric(18,2),
        "ActualCost" numeric(18,2),
        "WorkPerformed" text,
        "CompletionDate" date,
        "ResultingCondition" character varying(15),
        "AssigneeId" uuid,
        "CancellationReason" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedBy" uuid,
        CONSTRAINT "PK_MaintenanceRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_MaintenanceRecords_ObservedCondition" CHECK ("ObservedCondition" IN ('NEW','GOOD','FAIR','POOR','UNSERVICEABLE')),
        CONSTRAINT "CK_MaintenanceRecords_Priority" CHECK ("Priority" IN ('LOW','MEDIUM','HIGH','CRITICAL')),
        CONSTRAINT "CK_MaintenanceRecords_ResultingCondition" CHECK ("ResultingCondition" IS NULL OR "ResultingCondition" IN ('NEW','GOOD','FAIR','POOR','UNSERVICEABLE')),
        CONSTRAINT "CK_MaintenanceRecords_Status" CHECK ("Status" IN ('REQUESTED','APPROVED','IN_PROGRESS','COMPLETED','CANCELLED')),
        CONSTRAINT "CK_MaintenanceRecords_Type" CHECK ("Type" IN ('CORRECTIVE','PREVENTIVE')),
        CONSTRAINT "FK_MaintenanceRecords_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_MaintenanceRecords_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_MaintenanceRecords_Users_AssigneeId" FOREIGN KEY ("AssigneeId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817151743_AddMaintenanceRecord') THEN
    CREATE INDEX "IX_MaintenanceRecords_AssetId" ON "MaintenanceRecords" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817151743_AddMaintenanceRecord') THEN
    CREATE INDEX "IX_MaintenanceRecords_AssigneeId" ON "MaintenanceRecords" ("AssigneeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817151743_AddMaintenanceRecord') THEN
    CREATE INDEX "IX_MaintenanceRecords_OrganizationId" ON "MaintenanceRecords" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817151743_AddMaintenanceRecord') THEN
    CREATE INDEX "IX_MaintenanceRecords_Priority" ON "MaintenanceRecords" ("Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817151743_AddMaintenanceRecord') THEN
    CREATE INDEX "IX_MaintenanceRecords_Status" ON "MaintenanceRecords" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817151743_AddMaintenanceRecord') THEN
    CREATE INDEX "IX_MaintenanceRecords_Type" ON "MaintenanceRecords" ("Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817151743_AddMaintenanceRecord') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260817151743_AddMaintenanceRecord', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817202030_AddValuationDateToDisposalRequest') THEN
    ALTER TABLE "DisposalRequests" ADD "ValuationDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260817202030_AddValuationDateToDisposalRequest') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260817202030_AddValuationDateToDisposalRequest', '10.0.10');
    END IF;
END $EF$;
COMMIT;

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

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912055250_AddNotifications') THEN
    CREATE TABLE "Notifications" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "RecipientUserId" uuid NOT NULL,
        "Type" character varying(40) NOT NULL,
        "Title" text NOT NULL,
        "Message" text NOT NULL,
        "RelatedEntityType" character varying(60),
        "RelatedEntityId" uuid,
        "IsRead" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Notifications_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Notifications_Users_RecipientUserId" FOREIGN KEY ("RecipientUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912055250_AddNotifications') THEN
    CREATE INDEX "IX_Notifications_CreatedAt" ON "Notifications" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912055250_AddNotifications') THEN
    CREATE INDEX "IX_Notifications_OrganizationId" ON "Notifications" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912055250_AddNotifications') THEN
    CREATE INDEX "IX_Notifications_RecipientUserId_IsRead" ON "Notifications" ("RecipientUserId", "IsRead");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912055250_AddNotifications') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260912055250_AddNotifications', '10.0.10');
    END IF;
END $EF$;
COMMIT;

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

START TRANSACTION;

ALTER TABLE "MaintenanceRecords" ADD "ReportedByUserId" uuid;

CREATE INDEX "IX_MaintenanceRecords_ReportedByUserId" ON "MaintenanceRecords" ("ReportedByUserId");

ALTER TABLE "MaintenanceRecords" ADD CONSTRAINT "FK_MaintenanceRecords_Users_ReportedByUserId"
    FOREIGN KEY ("ReportedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260925090000_AddMaintenanceReporter', '10.0.10');

COMMIT;
