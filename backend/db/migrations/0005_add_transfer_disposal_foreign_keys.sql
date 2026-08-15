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

