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

