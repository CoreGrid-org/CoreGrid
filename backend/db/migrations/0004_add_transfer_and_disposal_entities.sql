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

