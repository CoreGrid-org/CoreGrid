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

