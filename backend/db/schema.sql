CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Organizations" (
    "Id" uuid NOT NULL,
    "ExternalOrgId" text NOT NULL,
    "Name" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id")
);

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

CREATE UNIQUE INDEX "IX_Organizations_ExternalOrgId" ON "Organizations" ("ExternalOrgId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE UNIQUE INDEX "IX_Users_ExternalSubjectId" ON "Users" ("ExternalSubjectId");

CREATE INDEX "IX_Users_OrganizationId" ON "Users" ("OrganizationId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260809062238_InitialCreate', '10.0.10');

COMMIT;

START TRANSACTION;
DROP INDEX "IX_Organizations_ExternalOrgId";

ALTER TABLE "Organizations" DROP COLUMN "ExternalOrgId";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260809130913_RemoveOrganizationExternalId', '10.0.10');

COMMIT;

START TRANSACTION;
ALTER TABLE "Users" ADD "DepartmentId" uuid;

ALTER TABLE "Organizations" ADD "CreatedBy" uuid;

ALTER TABLE "Organizations" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';

ALTER TABLE "Organizations" ADD "UpdatedBy" uuid;

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

CREATE INDEX "IX_Users_DepartmentId" ON "Users" ("DepartmentId");

CREATE INDEX "IX_AssetAttributeDefinitions_AssetTypeId" ON "AssetAttributeDefinitions" ("AssetTypeId");

CREATE UNIQUE INDEX "IX_AssetAttributeDefinitions_AssetTypeId_Name" ON "AssetAttributeDefinitions" ("AssetTypeId", "Name");

CREATE INDEX "IX_AssetAttributeValues_AssetAttributeDefinitionId_ValueNumber" ON "AssetAttributeValues" ("AssetAttributeDefinitionId", "ValueNumber");

CREATE INDEX "IX_AssetAttributeValues_AssetAttributeDefinitionId_ValueText" ON "AssetAttributeValues" ("AssetAttributeDefinitionId", "ValueText");

CREATE UNIQUE INDEX "IX_AssetAttributeValues_AssetId_AssetAttributeDefinitionId" ON "AssetAttributeValues" ("AssetId", "AssetAttributeDefinitionId");

CREATE INDEX "IX_AssetCategories_OrganizationId" ON "AssetCategories" ("OrganizationId");

CREATE UNIQUE INDEX "IX_AssetCategories_OrganizationId_Code" ON "AssetCategories" ("OrganizationId", "Code");

CREATE INDEX "IX_AssetHistory_ActorUserId" ON "AssetHistory" ("ActorUserId");

CREATE INDEX "IX_AssetHistory_AssetId" ON "AssetHistory" ("AssetId");

CREATE INDEX "IX_AssetHistory_CreatedAt" ON "AssetHistory" ("CreatedAt");

CREATE INDEX "IX_AssetHistory_OrganizationId" ON "AssetHistory" ("OrganizationId");

CREATE INDEX "IX_Assets_AssetTypeId" ON "Assets" ("AssetTypeId");

CREATE INDEX "IX_Assets_Condition" ON "Assets" ("Condition");

CREATE INDEX "IX_Assets_DepartmentId" ON "Assets" ("DepartmentId");

CREATE INDEX "IX_Assets_LocationId" ON "Assets" ("LocationId");

CREATE INDEX "IX_Assets_Name" ON "Assets" ("Name");

CREATE INDEX "IX_Assets_OrganizationId" ON "Assets" ("OrganizationId");

CREATE UNIQUE INDEX "IX_Assets_OrganizationId_AssetCode" ON "Assets" ("OrganizationId", "AssetCode");

CREATE INDEX "IX_Assets_Status" ON "Assets" ("Status");

CREATE INDEX "IX_AssetTypes_AssetCategoryId" ON "AssetTypes" ("AssetCategoryId");

CREATE INDEX "IX_AssetTypes_OrganizationId" ON "AssetTypes" ("OrganizationId");

CREATE UNIQUE INDEX "IX_AssetTypes_OrganizationId_Code" ON "AssetTypes" ("OrganizationId", "Code");

CREATE INDEX "IX_Departments_OrganizationId" ON "Departments" ("OrganizationId");

CREATE UNIQUE INDEX "IX_Departments_OrganizationId_Code" ON "Departments" ("OrganizationId", "Code");

CREATE INDEX "IX_Locations_DepartmentId" ON "Locations" ("DepartmentId");

CREATE INDEX "IX_Locations_OrganizationId" ON "Locations" ("OrganizationId");

CREATE INDEX "IX_OrganizationPolicies_AssetTypeId" ON "OrganizationPolicies" ("AssetTypeId");

CREATE INDEX "IX_OrganizationPolicies_OrganizationId" ON "OrganizationPolicies" ("OrganizationId");

CREATE UNIQUE INDEX "IX_OrganizationPolicies_OrganizationId_AssetTypeId" ON "OrganizationPolicies" ("OrganizationId", "AssetTypeId");

ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT;

DO $$ BEGIN IF EXISTS (SELECT FROM pg_roles WHERE rolname='coregrid_app') THEN EXECUTE 'REVOKE UPDATE, DELETE ON "AssetHistory" FROM coregrid_app'; END IF; END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260814023105_AddAssetSchema', '10.0.10');

COMMIT;

START TRANSACTION;
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

CREATE INDEX "IX_AssetTransfers_ApprovedByUserId" ON "AssetTransfers" ("ApprovedByUserId");

CREATE INDEX "IX_AssetTransfers_ConfirmedByUserId" ON "AssetTransfers" ("ConfirmedByUserId");

CREATE INDEX "IX_AssetTransfers_InitiatedByUserId" ON "AssetTransfers" ("InitiatedByUserId");

CREATE INDEX "IX_AssetTransfers_OrganizationId" ON "AssetTransfers" ("OrganizationId");

CREATE INDEX "IX_DisposalRequests_ApprovedByUserId" ON "DisposalRequests" ("ApprovedByUserId");

CREATE INDEX "IX_DisposalRequests_InitiatedByUserId" ON "DisposalRequests" ("InitiatedByUserId");

CREATE INDEX "IX_DisposalRequests_OrganizationId" ON "DisposalRequests" ("OrganizationId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260814175120_AddTransferAndDisposalEntities', '10.0.10');

COMMIT;

START TRANSACTION;
CREATE INDEX "IX_DisposalRequests_AssetId" ON "DisposalRequests" ("AssetId");

CREATE INDEX "IX_AssetTransfers_AssetId" ON "AssetTransfers" ("AssetId");

CREATE INDEX "IX_AssetTransfers_FromDepartmentId" ON "AssetTransfers" ("FromDepartmentId");

CREATE INDEX "IX_AssetTransfers_FromLocationId" ON "AssetTransfers" ("FromLocationId");

CREATE INDEX "IX_AssetTransfers_ToDepartmentId" ON "AssetTransfers" ("ToDepartmentId");

CREATE INDEX "IX_AssetTransfers_ToLocationId" ON "AssetTransfers" ("ToLocationId");

ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT;

ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Departments_FromDepartmentId" FOREIGN KEY ("FromDepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT;

ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Departments_ToDepartmentId" FOREIGN KEY ("ToDepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT;

ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Locations_FromLocationId" FOREIGN KEY ("FromLocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT;

ALTER TABLE "AssetTransfers" ADD CONSTRAINT "FK_AssetTransfers_Locations_ToLocationId" FOREIGN KEY ("ToLocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT;

ALTER TABLE "DisposalRequests" ADD CONSTRAINT "FK_DisposalRequests_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260815024645_AddTransferDisposalForeignKeys', '10.0.10');

COMMIT;

START TRANSACTION;
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

CREATE INDEX "IX_AuditLogEntries_ActorUserId" ON "AuditLogEntries" ("ActorUserId");

CREATE INDEX "IX_AuditLogEntries_CorrelationId" ON "AuditLogEntries" ("CorrelationId");

CREATE INDEX "IX_AuditLogEntries_CreatedAt" ON "AuditLogEntries" ("CreatedAt");

CREATE INDEX "IX_AuditLogEntries_EntityType" ON "AuditLogEntries" ("EntityType");

CREATE INDEX "IX_AuditLogEntries_OrganizationId" ON "AuditLogEntries" ("OrganizationId");

DO $$ BEGIN IF EXISTS (SELECT FROM pg_roles WHERE rolname='coregrid_app') THEN EXECUTE 'REVOKE UPDATE, DELETE ON "AuditLogEntries" FROM coregrid_app'; END IF; END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260815031732_AddAuditLog', '10.0.10');

COMMIT;

START TRANSACTION;
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

CREATE INDEX "IX_Discrepancies_AssetId" ON "Discrepancies" ("AssetId");

CREATE INDEX "IX_Discrepancies_CampaignId" ON "Discrepancies" ("CampaignId");

CREATE INDEX "IX_Discrepancies_OrganizationId" ON "Discrepancies" ("OrganizationId");

CREATE INDEX "IX_Discrepancies_RaisedByUserId" ON "Discrepancies" ("RaisedByUserId");

CREATE INDEX "IX_Discrepancies_ResolvedByUserId" ON "Discrepancies" ("ResolvedByUserId");

CREATE INDEX "IX_Discrepancies_Status" ON "Discrepancies" ("Status");

CREATE INDEX "IX_Discrepancies_Type" ON "Discrepancies" ("Type");

CREATE INDEX "IX_Discrepancies_VerificationTaskId" ON "Discrepancies" ("VerificationTaskId");

CREATE INDEX "IX_VerificationCampaigns_CreatedByUserId" ON "VerificationCampaigns" ("CreatedByUserId");

CREATE INDEX "IX_VerificationCampaigns_OrganizationId" ON "VerificationCampaigns" ("OrganizationId");

CREATE INDEX "IX_VerificationCampaigns_ScopeAssetCategoryId" ON "VerificationCampaigns" ("ScopeAssetCategoryId");

CREATE INDEX "IX_VerificationCampaigns_ScopeAssetTypeId" ON "VerificationCampaigns" ("ScopeAssetTypeId");

CREATE INDEX "IX_VerificationCampaigns_ScopeDepartmentId" ON "VerificationCampaigns" ("ScopeDepartmentId");

CREATE INDEX "IX_VerificationCampaigns_ScopeLocationId" ON "VerificationCampaigns" ("ScopeLocationId");

CREATE INDEX "IX_VerificationCampaigns_Status" ON "VerificationCampaigns" ("Status");

CREATE INDEX "IX_VerificationTasks_AssertedLocationId" ON "VerificationTasks" ("AssertedLocationId");

CREATE INDEX "IX_VerificationTasks_AssetId" ON "VerificationTasks" ("AssetId");

CREATE INDEX "IX_VerificationTasks_AssignedToUserId" ON "VerificationTasks" ("AssignedToUserId");

CREATE INDEX "IX_VerificationTasks_CampaignId" ON "VerificationTasks" ("CampaignId");

CREATE INDEX "IX_VerificationTasks_CompletedByUserId" ON "VerificationTasks" ("CompletedByUserId");

CREATE INDEX "IX_VerificationTasks_OrganizationId" ON "VerificationTasks" ("OrganizationId");

CREATE INDEX "IX_VerificationTasks_Status" ON "VerificationTasks" ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260815032307_AddVerificationAndDiscrepancies', '10.0.10');

COMMIT;

