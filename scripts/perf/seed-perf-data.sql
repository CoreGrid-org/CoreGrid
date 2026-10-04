-- CoreGrid performance dataset (SRS §13.5: >= 500 assets, >= 1,500 maintenance records).
--
-- Seeds one department with two locations, one asset category with three asset
-- types, an organisation-wide default policy (only if none exists), N assets and
-- M maintenance records with realistic completed-history aggregates, all inside
-- the deployment's existing organisation (run first-run Setup first).
--
-- Run against a disposable, staging or local database — never production:
--   psql "$PG_URL" -f scripts/perf/seed-perf-data.sql
--   psql "$PG_URL" -v assets=1000 -v records=3000 -f scripts/perf/seed-perf-data.sql
--   psql "$PG_URL" -v org_id=<uuid> -f scripts/perf/seed-perf-data.sql   (only if >1 organisation)
--
-- Idempotent: if the PERF category already exists for the organisation it does nothing.
-- Every seeded row is recognisable by code/name prefix "PERF".

\set ON_ERROR_STOP on

\if :{?org_id}
SELECT set_config('perf.org_id', :'org_id', false) \gset
\else
SELECT set_config('perf.org_id', '', false) \gset
\endif
\if :{?assets}
\else
\set assets 600
\endif
\if :{?records}
\else
\set records 1800
\endif
SELECT set_config('perf.assets', :'assets', false), set_config('perf.records', :'records', false) \gset

DO $seed$
DECLARE
    v_org        uuid := nullif(current_setting('perf.org_id'), '')::uuid;
    v_assets     int  := current_setting('perf.assets')::int;
    v_records    int  := current_setting('perf.records')::int;
    v_admin      uuid;
    v_dept       uuid := gen_random_uuid();
    v_loc1       uuid := gen_random_uuid();
    v_loc2       uuid := gen_random_uuid();
    v_cat        uuid := gen_random_uuid();
    v_now        timestamptz := now();
    v_org_count  int;
BEGIN
    IF v_org IS NULL THEN
        SELECT count(*) INTO v_org_count FROM "Organizations";
        IF v_org_count = 0 THEN
            RAISE EXCEPTION 'No organisation found. Complete first-run Setup in the web app, then re-run.';
        ELSIF v_org_count > 1 THEN
            RAISE EXCEPTION 'More than one organisation found; pass -v org_id=<uuid>.';
        END IF;
        SELECT "Id" INTO v_org FROM "Organizations";
    END IF;

    IF EXISTS (SELECT 1 FROM "AssetCategories" WHERE "OrganizationId" = v_org AND "Code" = 'PERF') THEN
        RAISE NOTICE 'Performance dataset already present for organisation % — nothing to do.', v_org;
        RETURN;
    END IF;

    -- Role 3 = Administrator (CoreGridRole enum order: Staff, InventoryOfficer, Auditor, Administrator).
    SELECT "Id" INTO v_admin FROM "Users"
     WHERE "OrganizationId" = v_org AND "Role" = 3 AND "IsActive"
     ORDER BY "CreatedAt" LIMIT 1;

    PERFORM setseed(0.42);

    INSERT INTO "Departments" ("Id","OrganizationId","Code","Name","IsActive","CreatedAt","UpdatedAt","CreatedBy","UpdatedBy")
    VALUES (v_dept, v_org, 'PERF', 'PERF Load-Test Department', true, v_now, v_now, v_admin, v_admin);

    INSERT INTO "Locations" ("Id","OrganizationId","DepartmentId","Name","Type","IsActive","CreatedAt","UpdatedAt","CreatedBy","UpdatedBy")
    VALUES (v_loc1, v_org, v_dept, 'PERF Main Store', 'store',    true, v_now, v_now, v_admin, v_admin),
           (v_loc2, v_org, v_dept, 'PERF Workshop',   'workshop', true, v_now, v_now, v_admin, v_admin);

    INSERT INTO "AssetCategories" ("Id","OrganizationId","Code","Name","IsActive","CreatedAt","UpdatedAt","CreatedBy","UpdatedBy")
    VALUES (v_cat, v_org, 'PERF', 'PERF Load-Test Assets', true, v_now, v_now, v_admin, v_admin);

    CREATE TEMP TABLE perf_types (n int, id uuid, code text, name text, life int, interval_days int, base_cost numeric) ON COMMIT DROP;
    INSERT INTO perf_types VALUES
        (0, gen_random_uuid(), 'PERF-VEH', 'PERF Vehicle',        10, 180, 4500000),
        (1, gen_random_uuid(), 'PERF-IT',  'PERF Laptop',          5, 365,  350000),
        (2, gen_random_uuid(), 'PERF-MED', 'PERF Medical Device',  8,  90, 1800000);

    INSERT INTO "AssetTypes" ("Id","OrganizationId","AssetCategoryId","Code","Name","UsefulLifeYears","DefaultMaintenanceIntervalDays","IsActive","CreatedAt","UpdatedAt","CreatedBy","UpdatedBy")
    SELECT id, v_org, v_cat, code, name, life, interval_days, true, v_now, v_now, v_admin, v_admin FROM perf_types;

    -- The Policy Compliance Agent needs a policy; add an org-wide default only if none exists.
    IF NOT EXISTS (SELECT 1 FROM "OrganizationPolicies" WHERE "OrganizationId" = v_org AND "AssetTypeId" IS NULL) THEN
        INSERT INTO "OrganizationPolicies" ("Id","OrganizationId","AssetTypeId","RepairToReplaceCostThreshold","MinimumServiceLifeYears",
            "MaxAcceptableFailureFrequency","ValuationValidityWindowDays","ConfidenceFloor","CostVarianceTolerancePercent",
            "OutstandingTransferDays","ApprovalOverduePeriodHours","CreatedAt","UpdatedAt","CreatedBy","UpdatedBy")
        VALUES (gen_random_uuid(), v_org, NULL, 0.65, 5, 4, 90, 0.70, 15, 14, 72, v_now, v_now, v_admin, v_admin);
    END IF;

    CREATE TEMP TABLE perf_assets ON COMMIT DROP AS
    SELECT g AS rn,
           gen_random_uuid() AS id,
           t.id AS type_id, t.code AS type_code, t.name AS type_name, t.life, t.base_cost,
           (current_date - ((1 + floor(random() * 9))::int * 365 + floor(random() * 300)::int))::date AS acquired,
           (ARRAY['NEW','GOOD','GOOD','FAIR','FAIR','POOR'])[1 + floor(random() * 6)::int] AS cond,
           CASE WHEN g % 2 = 0 THEN v_loc1 ELSE v_loc2 END AS loc
      FROM generate_series(1, v_assets) g
      JOIN perf_types t ON t.n = g % 3;

    INSERT INTO "Assets" ("Id","OrganizationId","AssetTypeId","DepartmentId","LocationId","AssetCode","Name","Status","Condition",
        "AcquisitionDate","AcquisitionCost","ResidualValue","CumulativeMaintenanceCost","RepairCount","LastRepairDate","QrPayload",
        "CreatedAt","UpdatedAt","CreatedBy","UpdatedBy")
    SELECT a.id, v_org, a.type_id, v_dept, a.loc,
           a.type_code || '-' || lpad(a.rn::text, 5, '0'),
           a.type_name || ' #' || a.rn,
           'ACTIVE', a.cond, a.acquired,
           round(a.base_cost * (0.8 + random() * 0.4)::numeric, 2),
           0, 0, 0, NULL,
           a.type_code || '-' || lpad(a.rn::text, 5, '0'),
           v_now, v_now, v_admin, v_admin
      FROM perf_assets a;

    -- Straight-line residual value, same formula as the API (FR-030).
    UPDATE "Assets" x
       SET "ResidualValue" = round(x."AcquisitionCost" * greatest(0, 1 - ((current_date - x."AcquisitionDate") / 365.0) / a.life), 2)
      FROM perf_assets a WHERE x."Id" = a.id;

    INSERT INTO "MaintenanceRecords" ("Id","OrganizationId","AssetId","Description","ObservedCondition","PhotoObjectKey","Type","Priority",
        "Status","EstimatedCost","ActualCost","WorkPerformed","CompletionDate","ResultingCondition","AssigneeId","CancellationReason",
        "CreatedAt","UpdatedAt","CreatedBy","UpdatedBy","ReportedByUserId")
    SELECT gen_random_uuid(), v_org, a.id,
           'PERF seeded maintenance #' || g,
           (ARRAY['GOOD','FAIR','POOR'])[1 + floor(random() * 3)::int],
           NULL,
           CASE WHEN g % 3 = 0 THEN 'PREVENTIVE' ELSE 'CORRECTIVE' END,
           (ARRAY['LOW','MEDIUM','HIGH','CRITICAL'])[1 + floor(random() * 4)::int],
           CASE WHEN g % 10 = 0 THEN 'CANCELLED' ELSE 'COMPLETED' END,
           est, CASE WHEN g % 10 = 0 THEN NULL ELSE round(est * (0.85 + random() * 0.3)::numeric, 2) END,
           CASE WHEN g % 10 = 0 THEN NULL ELSE 'PERF seeded work' END,
           CASE WHEN g % 10 = 0 THEN NULL ELSE done END,
           CASE WHEN g % 10 = 0 THEN NULL ELSE 'GOOD' END,
           v_admin,
           CASE WHEN g % 10 = 0 THEN 'PERF seeded cancellation' END,
           done::timestamptz, done::timestamptz, v_admin, v_admin, v_admin
      FROM (SELECT g,
                   1 + (g % v_assets) AS rn,
                   round((5000 + random() * 95000)::numeric, 2) AS est,
                   (current_date - floor(random() * 1095)::int) AS done
              FROM generate_series(1, v_records) g) m
      JOIN perf_assets a ON a.rn = m.rn;

    -- Cumulative cost / repair count / last repair date (FR-040), from completed records.
    UPDATE "Assets" x
       SET "CumulativeMaintenanceCost" = s.total, "RepairCount" = s.cnt, "LastRepairDate" = s.last_done
      FROM (SELECT "AssetId", sum("ActualCost") total, count(*) cnt, max("CompletionDate") last_done
              FROM "MaintenanceRecords"
             WHERE "OrganizationId" = v_org AND "Status" = 'COMPLETED' AND "Description" LIKE 'PERF seeded%'
             GROUP BY "AssetId") s
     WHERE x."Id" = s."AssetId";

    RAISE NOTICE 'Seeded % assets and % maintenance records into organisation %.', v_assets, v_records, v_org;
END
$seed$;

SELECT (SELECT count(*) FROM "Assets") AS assets_total,
       (SELECT count(*) FROM "MaintenanceRecords") AS maintenance_records_total;
