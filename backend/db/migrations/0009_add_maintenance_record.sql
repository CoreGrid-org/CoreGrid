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

