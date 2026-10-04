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

