-- CoreGrid demo dataset: Ministry of Health - Sri Lanka.
--
-- Fills every table except the agentic-workflow ones (AgentWorkflows,
-- AgentExecutionSteps, AgentApprovals: run those by hand from the web app) for
-- the demo deployment's existing organisation, using ONLY its four demo users:
--
--   admin@coregrid.test    Administrator       approvals, configuration, dashboards
--   officer@coregrid.test  Inventory Officer   maintenance, transfers, verification tasks
--   auditor@coregrid.test  Auditor             campaigns, discrepancies, audit log, reports
--   staff@coregrid.test    Department Staff    fault reports (National Hospital of Sri Lanka)
--
-- Every date is relative to the day the script runs, so the data always looks
-- current. About 18 months of history: preventive servicing, corrective
-- repairs, transfers, disposals, verification campaigns, notifications, asset
-- history and audit-log entries consistent with what the API would write.
--
-- Dry run by default (everything is rolled back). To apply:
--   psql "$PG" -v commit=1 -f scripts/demo/seed-demo-data.sql
--
-- Refuses to run if the four users are missing or the organisation already
-- has departments (i.e. it was seeded before or holds real data).

\set ON_ERROR_STOP on

BEGIN;

-- ---------------------------------------------------------------- helpers --

-- A moment `days` days ago (negative = in the future), 09:30 Sri Lanka time.
CREATE FUNCTION pg_temp.ts(days int, mins int DEFAULT 0) RETURNS timestamptz
LANGUAGE sql STABLE AS
$$ SELECT ((current_date - days)::timestamp + interval '4 hours' + make_interval(mins => mins)) AT TIME ZONE 'UTC' $$;

CREATE FUNCTION pg_temp.dt(days int) RETURNS date
LANGUAGE sql STABLE AS $$ SELECT current_date - days $$;

-- --------------------------------------------------------------- context --

CREATE TEMP TABLE ctx ON COMMIT DROP AS
SELECT (SELECT "OrganizationId" FROM "Users" WHERE lower("Email") = 'admin@coregrid.test')   AS org,
       (SELECT "Id" FROM "Users" WHERE lower("Email") = 'admin@coregrid.test')               AS admin,
       (SELECT "Id" FROM "Users" WHERE lower("Email") = 'officer@coregrid.test')             AS officer,
       (SELECT "Id" FROM "Users" WHERE lower("Email") = 'auditor@coregrid.test')             AS auditor,
       (SELECT "Id" FROM "Users" WHERE lower("Email") = 'staff@coregrid.test')               AS staff;

DO $$
DECLARE c record;
BEGIN
    SELECT * INTO c FROM ctx;
    IF c.admin IS NULL OR c.officer IS NULL OR c.auditor IS NULL OR c.staff IS NULL THEN
        RAISE EXCEPTION 'All four demo users (admin/officer/auditor/staff@coregrid.test) must exist first.';
    END IF;
    IF (SELECT count(DISTINCT "OrganizationId") FROM "Users" WHERE "Id" IN (c.admin, c.officer, c.auditor, c.staff)) <> 1 THEN
        RAISE EXCEPTION 'The four demo users belong to different organisations.';
    END IF;
    IF EXISTS (SELECT 1 FROM "Departments" WHERE "OrganizationId" = c.org) THEN
        RAISE EXCEPTION 'Organisation % already has departments: demo data was seeded before (or real data is present). Nothing done.', c.org;
    END IF;
END $$;

UPDATE "Organizations" o
SET "Name" = 'Ministry of Health - Sri Lanka', "UpdatedAt" = now(), "UpdatedBy" = c.admin
FROM ctx c WHERE o."Id" = c.org;

-- ----------------------------------------------------------- departments --

CREATE TEMP TABLE dept (code text PRIMARY KEY, name text, id uuid DEFAULT gen_random_uuid()) ON COMMIT DROP;
INSERT INTO dept (code, name) VALUES
    ('MOH-HQ', 'Ministry of Health - Head Office'),
    ('NHSL',   'National Hospital of Sri Lanka'),
    ('THK',    'Teaching Hospital, Kandy'),
    ('THG',    'Teaching Hospital, Karapitiya'),
    ('THJ',    'Teaching Hospital, Jaffna'),
    ('THA',    'Teaching Hospital, Anuradhapura');

INSERT INTO "Departments" ("Id", "OrganizationId", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT d.id, c.org, d.code, d.name, true, pg_temp.ts(600), pg_temp.ts(600), c.admin, c.admin FROM dept d, ctx c;

-- Staff only see their own department (SRS §4.6).
UPDATE "Users" u SET "DepartmentId" = (SELECT id FROM dept WHERE code = 'NHSL') FROM ctx c WHERE u."Id" = c.staff;

-- ------------------------------------------------------------- locations --

CREATE TEMP TABLE loc (key text PRIMARY KEY, dept text, name text, type text, active boolean DEFAULT true, id uuid DEFAULT gen_random_uuid()) ON COMMIT DROP;
INSERT INTO loc (key, dept, name, type, active) VALUES
    ('HQ-REC',     'MOH-HQ', 'Head Office Records Room',          'office',   true),
    ('HQ-SRV',     'MOH-HQ', 'Head Office IT Server Room',        'office',   true),
    ('HQ-CMS',     'MOH-HQ', 'Central Medical Supplies Store',    'store',    true),
    ('NHSL-STORE', 'NHSL',   'NHSL Main Store',                   'store',    true),
    ('NHSL-ICU',   'NHSL',   'ICU - Ward 5',                      'ward',     true),
    ('NHSL-ETU',   'NHSL',   'Emergency Treatment Unit',          'ward',     true),
    ('NHSL-RAD',   'NHSL',   'Radiology Unit',                    'clinic',   true),
    ('NHSL-OT',    'NHSL',   'Operating Theatre Complex',         'theatre',  true),
    ('NHSL-LAB',   'NHSL',   'Clinical Laboratory',               'clinic',   true),
    ('NHSL-BME',   'NHSL',   'Biomedical Engineering Workshop',   'workshop', true),
    ('NHSL-PWR',   'NHSL',   'NHSL Power House',                  'workshop', true),
    ('NHSL-GAR',   'NHSL',   'NHSL Ambulance Garage',             'garage',   true),
    ('NHSL-OLD',   'NHSL',   'Old Radiology Annex',               'clinic',   false),
    ('THK-STORE',  'THK',    'Kandy Central Store',               'store',    true),
    ('THK-ICU',    'THK',    'Kandy ICU',                         'ward',     true),
    ('THK-OPD',    'THK',    'Kandy OPD',                         'clinic',   true),
    ('THK-GAR',    'THK',    'Kandy Ambulance Bay',               'garage',   true),
    ('THG-STORE',  'THG',    'Karapitiya Main Store',             'store',    true),
    ('THG-CARD',   'THG',    'Karapitiya Cardiology Unit',        'ward',     true),
    ('THJ-STORE',  'THJ',    'Jaffna Main Store',                 'store',    true),
    ('THJ-PAED',   'THJ',    'Jaffna Paediatric Ward',            'ward',     true),
    ('THA-STORE',  'THA',    'Anuradhapura Main Store',           'store',    true),
    ('THA-ETU',    'THA',    'Anuradhapura Emergency Unit',       'ward',     true),
    ('THA-GAR',    'THA',    'Anuradhapura Ambulance Bay',        'garage',   true);

INSERT INTO "Locations" ("Id", "OrganizationId", "DepartmentId", "Name", "Type", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT l.id, c.org, d.id, l.name, l.type, l.active, pg_temp.ts(598),
       CASE WHEN l.active THEN pg_temp.ts(598) ELSE pg_temp.ts(200) END, c.admin, c.admin
FROM loc l JOIN dept d ON d.code = l.dept CROSS JOIN ctx c;

-- -------------------------------------------------- categories and types --

CREATE TEMP TABLE cat (code text PRIMARY KEY, name text, id uuid DEFAULT gen_random_uuid()) ON COMMIT DROP;
INSERT INTO cat (code, name) VALUES
    ('MED', 'Medical Equipment'), ('IT', 'IT Equipment'), ('FUR', 'Furniture & Fittings'),
    ('VEH', 'Vehicles'), ('FAC', 'Facility Equipment'), ('LAB', 'Laboratory Equipment');

INSERT INTO "AssetCategories" ("Id", "OrganizationId", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT k.id, c.org, k.code, k.name, true, pg_temp.ts(597), pg_temp.ts(597), c.admin, c.admin FROM cat k, ctx c;

-- base = typical preventive-service cost (LKR); service = work recorded for it.
CREATE TEMP TABLE typ (code text PRIMARY KEY, cat text, name text, life int, intv int, active boolean, base numeric, service text,
                       id uuid DEFAULT gen_random_uuid()) ON COMMIT DROP;
INSERT INTO typ (code, cat, name, life, intv, active, base, service) VALUES
    ('VENT',  'MED', 'Ventilator',        10,  90, true,  48000, 'Quarterly service: flow and pressure calibration, filter and O2 cell check, alarm tests passed.'),
    ('MON',   'MED', 'Patient Monitor',    8, 180, true,  16000, 'Six-monthly service: NIBP and SpO2 accuracy verified, battery tested, electrical safety test passed.'),
    ('XRAY',  'MED', 'X-Ray Machine',     12, 180, true, 135000, 'Six-monthly service: tube output and kV accuracy verified, detector calibrated, radiation survey passed.'),
    ('INF',   'MED', 'Infusion Pump',      7, 180, true,   8500, 'Six-monthly service: flow-rate accuracy and occlusion pressure verified, battery replaced where weak.'),
    ('DEFIB', 'MED', 'Defibrillator',      8,  90, true,  14000, 'Quarterly service: energy delivery tested at 50/200/360 J, pads and battery checked.'),
    ('LAP',   'IT',  'Laptop',             4, NULL, true,     0, NULL),
    ('DSK',   'IT',  'Desktop Computer',   5, NULL, true,     0, NULL),
    ('SRV',   'IT',  'Server',             6, 180, true,  32000, 'Six-monthly service: firmware updated, fans and filters cleaned, RAID and PSU health verified.'),
    ('PRN',   'IT',  'Printer',            5, 365, true,   7500, 'Annual service: rollers cleaned, fuser inspected, firmware updated.'),
    ('FAX',   'IT',  'Fax Machine',        6, NULL, false,    0, NULL),
    ('BED',   'FUR', 'Hospital Bed',      10, 365, true,   6000, 'Annual service: actuators, brakes and side rails inspected and lubricated.'),
    ('AMB',   'VEH', 'Ambulance',         10,  90, true,  72000, 'Quarterly service: engine oil and filters changed, brakes and suspension inspected, medical fit-out checked.'),
    ('GEN',   'FAC', 'Standby Generator', 15,  90, true,  58000, 'Quarterly service: load-bank test, oil and fuel filters changed, ATS transfer tested.'),
    ('AC',    'FAC', 'Air Conditioner',   10, 180, true,  11000, 'Six-monthly service: coils cleaned, refrigerant pressure checked, filters replaced.'),
    ('CENT',  'LAB', 'Centrifuge',         8, 180, true,  15000, 'Six-monthly service: speed and temperature verified with tachometer, lid lock tested.');

INSERT INTO "AssetTypes" ("Id", "OrganizationId", "AssetCategoryId", "Code", "Name", "UsefulLifeYears", "DefaultMaintenanceIntervalDays",
                          "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT t.id, c.org, k.id, t.code, t.name, t.life, t.intv, t.active, pg_temp.ts(596),
       CASE WHEN t.active THEN pg_temp.ts(596) ELSE pg_temp.ts(230) END, c.admin, c.admin
FROM typ t JOIN cat k ON k.code = t.cat CROSS JOIN ctx c;

CREATE TEMP TABLE adef (typ text, name text, dtype text, req boolean, opts jsonb, ord int, id uuid DEFAULT gen_random_uuid()) ON COMMIT DROP;
INSERT INTO adef (typ, name, dtype, req, opts, ord) VALUES
    ('VENT', 'Serial number',            'TEXT',    true,  NULL, 1),
    ('VENT', 'Manufacturer',             'SELECT',  true,  '["Dräger", "Hamilton Medical", "Philips", "Mindray"]', 2),
    ('VENT', 'Last calibration',         'DATE',    false, NULL, 3),
    ('VENT', 'Portable',                 'BOOLEAN', false, NULL, 4),
    ('MON',  'Serial number',            'TEXT',    true,  NULL, 1),
    ('MON',  'Manufacturer',             'SELECT',  true,  '["Philips", "Mindray", "GE Healthcare", "Nihon Kohden"]', 2),
    ('MON',  'Screen size (inches)',     'NUMBER',  false, NULL, 3),
    ('XRAY', 'Serial number',            'TEXT',    true,  NULL, 1),
    ('XRAY', 'Max tube current (mA)',    'NUMBER',  false, NULL, 2),
    ('XRAY', 'Radiation licence expiry', 'DATE',    true,  NULL, 3),
    ('LAP',  'Serial number',            'TEXT',    true,  NULL, 1),
    ('LAP',  'RAM (GB)',                 'NUMBER',  true,  NULL, 2),
    ('LAP',  'Operating system',         'SELECT',  false, '["Windows 11", "Ubuntu 24.04", "macOS"]', 3),
    ('LAP',  'Warranty expiry',          'DATE',    false, NULL, 4),
    ('SRV',  'Serial number',            'TEXT',    true,  NULL, 1),
    ('SRV',  'RAM (GB)',                 'NUMBER',  true,  NULL, 2),
    ('SRV',  'Rack mounted',             'BOOLEAN', false, NULL, 3),
    ('AMB',  'Registration number',      'TEXT',    true,  NULL, 1),
    ('AMB',  'Fuel type',                'SELECT',  true,  '["Diesel", "Petrol", "Hybrid"]', 2),
    ('AMB',  'Engine capacity (cc)',     'NUMBER',  false, NULL, 3),
    ('AMB',  'Revenue licence expiry',   'DATE',    false, NULL, 4),
    ('GEN',  'Capacity (kVA)',           'NUMBER',  true,  NULL, 1),
    ('GEN',  'Fuel type',                'SELECT',  false, '["Diesel", "Petrol"]', 2),
    ('INF',  'Serial number',            'TEXT',    true,  NULL, 1),
    ('INF',  'Manufacturer',             'SELECT',  true,  '["B. Braun", "Fresenius Kabi", "Baxter"]', 2),
    ('INF',  'Pump type',                'SELECT',  true,  '["Volumetric", "Syringe"]', 3),
    ('INF',  'Last calibration',         'DATE',    false, NULL, 4),
    ('DEFIB','Serial number',            'TEXT',    true,  NULL, 1),
    ('DEFIB','Manufacturer',             'SELECT',  true,  '["Zoll", "Philips", "Mindray"]', 2),
    ('DEFIB','Mode',                     'SELECT',  true,  '["Manual and AED", "AED only"]', 3),
    ('DEFIB','Battery expiry',           'DATE',    false, NULL, 4),
    ('DSK',  'Serial number',            'TEXT',    true,  NULL, 1),
    ('DSK',  'RAM (GB)',                 'NUMBER',  true,  NULL, 2),
    ('DSK',  'Operating system',         'SELECT',  false, '["Windows 10", "Windows 11", "Ubuntu 24.04"]', 3),
    ('DSK',  'Warranty expiry',          'DATE',    false, NULL, 4),
    ('PRN',  'Serial number',            'TEXT',    true,  NULL, 1),
    ('PRN',  'Print type',               'SELECT',  true,  '["Mono laser", "Colour laser", "Multifunction copier"]', 2),
    ('PRN',  'Network enabled',          'BOOLEAN', false, NULL, 3),
    ('PRN',  'Page count',               'NUMBER',  false, NULL, 4),
    ('FAX',  'Serial number',            'TEXT',    true,  NULL, 1),
    ('FAX',  'Line number',              'TEXT',    false, NULL, 2),
    ('BED',  'Serial number',            'TEXT',    true,  NULL, 1),
    ('BED',  'Bed type',                 'SELECT',  true,  '["ICU electric", "Medical-surgical electric", "Manual ward"]', 2),
    ('BED',  'Safe working load (kg)',   'NUMBER',  false, NULL, 3),
    ('BED',  'Mattress included',        'BOOLEAN', false, NULL, 4),
    ('AC',   'Serial number',            'TEXT',    true,  NULL, 1),
    ('AC',   'Cooling capacity (BTU/h)', 'NUMBER',  true,  NULL, 2),
    ('AC',   'Inverter',                 'BOOLEAN', false, NULL, 3),
    ('AC',   'Refrigerant',              'SELECT',  false, '["R32", "R410A", "R22"]', 4),
    ('CENT', 'Serial number',            'TEXT',    true,  NULL, 1),
    ('CENT', 'Max speed (rpm)',          'NUMBER',  true,  NULL, 2),
    ('CENT', 'Refrigerated',             'BOOLEAN', false, NULL, 3),
    ('CENT', 'Last calibration',         'DATE',    false, NULL, 4);

INSERT INTO "AssetAttributeDefinitions" ("Id", "AssetTypeId", "Name", "DataType", "IsRequired", "ValidationRule", "SelectOptions",
                                         "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT a.id, t.id, a.name, a.dtype, a.req, NULL, a.opts, a.ord, true, pg_temp.ts(595), pg_temp.ts(595), c.admin, c.admin
FROM adef a JOIN typ t ON t.code = a.typ CROSS JOIN ctx c;

-- -------------------------------------------------------------- policies --

INSERT INTO "OrganizationPolicies" ("Id", "OrganizationId", "AssetTypeId", "RepairToReplaceCostThreshold", "MinimumServiceLifeYears",
    "MaxAcceptableFailureFrequency", "ValuationValidityWindowDays", "ConfidenceFloor", "CostVarianceTolerancePercent",
    "OutstandingTransferDays", "ApprovalOverduePeriodHours", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT gen_random_uuid(), c.org, t.id, p.r2r, p.minlife, p.maxfail, p.valwin, p.conf, p.tol, p.xfer, p.overdue,
       pg_temp.ts(594), pg_temp.ts(p.upd), c.admin, c.admin
FROM (VALUES (NULL,   0.65, 5.00, 3.00,  90, 0.70, 15.00, 7, 48, 594),
             ('VENT', 0.50, 7.00, 2.00,  60, 0.80, 10.00, 5, 24, 300),
             ('LAP',  0.40, 3.00, 4.00,  90, 0.70, 20.00, 7, 72, 594),
             ('AMB',  0.60, 8.00, 3.00, 120, 0.75, 15.00, 3, 24, 410))
     AS p(typ, r2r, minlife, maxfail, valwin, conf, tol, xfer, overdue, upd)
LEFT JOIN typ t ON t.code = p.typ CROSS JOIN ctx c;

-- ---------------------------------------------------------------- assets --
-- age = days since acquisition; oos = days since it went out of service
-- (condemned/disposed), so no servicing is recorded after that.

CREATE TEMP TABLE a (n int PRIMARY KEY, typ text, name text, dept text, loc text, cond text, age int, cost numeric,
                     status text, oos int DEFAULT 0, id uuid DEFAULT gen_random_uuid(), code text) ON COMMIT DROP;
INSERT INTO a (n, typ, name, dept, loc, cond, age, cost, status, oos) VALUES
    ( 1, 'VENT',  'Hamilton C6 Ventilator',                    'NHSL',   'NHSL-ICU',   'GOOD',          2400,  7200000, 'ACTIVE', 0),
    ( 2, 'VENT',  'Hamilton C6 Ventilator',                    'NHSL',   'NHSL-ICU',   'FAIR',          3300,  7200000, 'ACTIVE', 0),
    ( 3, 'VENT',  'Mindray SV300 Ventilator',                  'THK',    'THK-ICU',    'GOOD',           900,  5400000, 'ACTIVE', 0),
    ( 4, 'VENT',  'Philips Trilogy Evo Ventilator',            'NHSL',   'NHSL-ETU',   'GOOD',           700,  4800000, 'TRANSFER_REQUESTED', 0),
    ( 5, 'VENT',  'Dräger Evita V500 Ventilator',              'THG',    'THG-CARD',   'GOOD',          1500,  8100000, 'ACTIVE', 0),
    ( 6, 'VENT',  'Dräger Savina 300 Ventilator',              'THJ',    'THJ-PAED',   'POOR',          3900,  4900000, 'UNDER_MAINTENANCE', 0),
    ( 7, 'MON',   'Philips IntelliVue MX450 Patient Monitor',  'NHSL',   'NHSL-ICU',   'NEW',            120,  1250000, 'ACTIVE', 0),
    ( 8, 'MON',   'Mindray BeneView T8 Patient Monitor',       'NHSL',   'NHSL-ICU',   'FAIR',          1100,  1100000, 'UNDER_MAINTENANCE', 0),
    ( 9, 'MON',   'GE Carescape B450 Patient Monitor',         'NHSL',   'NHSL-ETU',   'GOOD',           800,   980000, 'IN_TRANSIT', 0),
    (10, 'MON',   'Nihon Kohden Life Scope G5 Monitor',        'THK',    'THK-ICU',    'GOOD',          1300,  1050000, 'ACTIVE', 0),
    (11, 'MON',   'Philips IntelliVue MP20 Patient Monitor',   'THG',    'THG-CARD',   'UNSERVICEABLE', 4300,   850000, 'DISPOSED', 165),
    (12, 'MON',   'Mindray ePM 12M Patient Monitor',           'THA',    'THA-ETU',    'FAIR',          1800,   920000, 'ACTIVE', 0),
    (13, 'XRAY',  'Shimadzu MobileDaRt Evolution Mobile X-Ray','NHSL',   'NHSL-RAD',   'POOR',          2900, 18500000, 'UNDER_MAINTENANCE', 0),
    (14, 'XRAY',  'Siemens Multix Fusion X-Ray System',        'THK',    'THK-OPD',    'GOOD',          1600, 24000000, 'ACTIVE', 0),
    (15, 'XRAY',  'Philips DigitalDiagnost C90 X-Ray',         'NHSL',   'NHSL-RAD',   'GOOD',           500, 32000000, 'ACTIVE', 0),
    (16, 'INF',   'B. Braun Infusomat Space Infusion Pump',    'NHSL',   'NHSL-ICU',   'GOOD',          1000,   420000, 'ACTIVE', 0),
    (17, 'INF',   'B. Braun Infusomat Space Infusion Pump',    'NHSL',   'NHSL-ICU',   'FAIR',          1000,   420000, 'ACTIVE', 0),
    (18, 'INF',   'Fresenius Agilia Volumetric Pump',          'NHSL',   'NHSL-ETU',   'FAIR',          2200,   380000, 'ACTIVE', 0),
    (19, 'INF',   'Baxter Sigma Spectrum Infusion Pump',       'THJ',    'THJ-PAED',   'GOOD',          1400,   450000, 'ACTIVE', 0),
    (20, 'INF',   'B. Braun Perfusor Space Syringe Pump',      'THG',    'THG-CARD',   'POOR',          2700,   310000, 'ACTIVE', 0),
    (21, 'DEFIB', 'Zoll R Series Defibrillator',               'NHSL',   'NHSL-ETU',   'GOOD',          1500,  1600000, 'ACTIVE', 0),
    (22, 'DEFIB', 'Philips HeartStart XL+ Defibrillator',      'THK',    'THK-ICU',    'GOOD',           900,  1450000, 'ACTIVE', 0),
    (23, 'DEFIB', 'Mindray BeneHeart D3 Defibrillator',        'THA',    'THA-ETU',    'FAIR',          2600,  1200000, 'ACTIVE', 0),
    (24, 'DEFIB', 'Philips HeartStart FRx AED',                'THG',    'THG-STORE',  'UNSERVICEABLE', 3400,   380000, 'CONDEMNED', 75),
    (25, 'LAP',   'Dell Latitude 5440 Laptop',                 'MOH-HQ', 'HQ-REC',     'GOOD',           600,   385000, 'ACTIVE', 0),
    (26, 'LAP',   'Lenovo ThinkPad E14 Laptop',                'NHSL',   'NHSL-STORE', 'GOOD',           900,   340000, 'TRANSFER_REQUESTED', 0),
    (27, 'LAP',   'HP ProBook 450 G8 Laptop',                  'MOH-HQ', 'HQ-REC',     'FAIR',          1700,   310000, 'ACTIVE', 0),
    (28, 'LAP',   'Dell Latitude 3420 Laptop',                 'THK',    'THK-OPD',    'POOR',          1650,   295000, 'CONDEMNED', 25),
    (29, 'LAP',   'Lenovo ThinkPad T14 Laptop',                'NHSL',   'NHSL-RAD',   'GOOD',           300,   420000, 'ACTIVE', 0),
    (30, 'DSK',   'HP ProDesk 400 G7 Desktop',                 'NHSL',   'NHSL-ETU',   'GOOD',          1200,   210000, 'ACTIVE', 0),
    (31, 'DSK',   'Dell OptiPlex 7090 Desktop',                'MOH-HQ', 'HQ-REC',     'GOOD',          1000,   235000, 'ACTIVE', 0),
    (32, 'DSK',   'Dell OptiPlex 3020 Desktop',                'THJ',    'THJ-STORE',  'UNSERVICEABLE', 3650,   165000, 'DISPOSAL_REQUESTED', 40),
    (33, 'SRV',   'HPE ProLiant DL380 Gen10 Server',           'MOH-HQ', 'HQ-SRV',     'FAIR',          2300,  2850000, 'ACTIVE', 0),
    (34, 'SRV',   'Dell PowerEdge R740 Server',                'MOH-HQ', 'HQ-SRV',     'GOOD',          1400,  3100000, 'ACTIVE', 0),
    (35, 'SRV',   'HPE ProLiant ML350 Gen9 Server',            'NHSL',   'NHSL-STORE', 'POOR',          3000,  1900000, 'ACTIVE', 0),
    (36, 'PRN',   'Canon imageRUNNER 2630i Copier',            'MOH-HQ', 'HQ-REC',     'GOOD',          1100,   540000, 'ACTIVE', 0),
    (37, 'PRN',   'HP LaserJet Pro M404dn Printer',            'NHSL',   'NHSL-ETU',   'FAIR',          1900,    95000, 'ACTIVE', 0),
    (38, 'PRN',   'Brother HL-L5100DN Printer',                'THK',    'THK-OPD',    'GOOD',          1000,    88000, 'ACTIVE', 0),
    (39, 'FAX',   'Panasonic KX-FL422 Fax Machine',            'MOH-HQ', 'HQ-REC',     'UNSERVICEABLE', 4000,    45000, 'DISPOSED', 245),
    (40, 'BED',   'Hill-Rom Progressa ICU Bed',                'NHSL',   'NHSL-ICU',   'GOOD',          1300,  2400000, 'ACTIVE', 0),
    (41, 'BED',   'Hill-Rom Progressa ICU Bed',                'NHSL',   'NHSL-ICU',   'FAIR',          1300,  2400000, 'ACTIVE', 0),
    (42, 'BED',   'Stryker S3 MedSurg Bed',                    'THG',    'THG-CARD',   'GOOD',           900,  1650000, 'ACTIVE', 0),
    (43, 'BED',   'Paramount Manual Ward Bed',                 'THJ',    'THJ-PAED',   'FAIR',          2500,   185000, 'IN_TRANSIT', 0),
    (44, 'BED',   'Paramount Manual Ward Bed',                 'THJ',    'THJ-STORE',  'POOR',          3100,   185000, 'CONDEMNED', 110),
    (45, 'AMB',   'Toyota HiAce Ambulance (WP NB-4521)',       'NHSL',   'NHSL-GAR',   'FAIR',          2800, 14500000, 'ACTIVE', 0),
    (46, 'AMB',   'Nissan Civilian Ambulance (CP KD-7788)',    'THK',    'THK-GAR',    'GOOD',          1700, 16200000, 'ACTIVE', 0),
    (47, 'AMB',   'Toyota HiAce Ambulance (NC PH-3310)',       'THA',    'THA-GAR',    'GOOD',          1100, 15800000, 'ACTIVE', 0),
    (48, 'GEN',   'Cummins C275 D5 Standby Generator',         'NHSL',   'NHSL-PWR',   'FAIR',          3500,  9800000, 'ACTIVE', 0),
    (49, 'GEN',   'Perkins 100 kVA Standby Generator',         'THJ',    'THJ-STORE',  'GOOD',          1200,  4200000, 'ACTIVE', 0),
    (50, 'GEN',   'Cummins 250 kVA Standby Generator',         'THK',    'THK-STORE',  'GOOD',          2100,  8700000, 'ACTIVE', 0),
    (51, 'AC',    'Daikin 24000 BTU Split Air Conditioner',    'NHSL',   'NHSL-OT',    'GOOD',          1000,   310000, 'ACTIVE', 0),
    (52, 'AC',    'LG 18000 BTU Inverter Air Conditioner',     'MOH-HQ', 'HQ-SRV',     'FAIR',          1900,   245000, 'ACTIVE', 0),
    (53, 'AC',    'Daikin 24000 BTU Split Air Conditioner',    'THA',    'THA-ETU',    'POOR',          2400,   310000, 'ACTIVE', 0),
    (54, 'CENT',  'Eppendorf 5810R Refrigerated Centrifuge',   'NHSL',   'NHSL-LAB',   'GOOD',          1400,  2100000, 'ACTIVE', 0),
    (55, 'CENT',  'Hettich Rotina 380R Centrifuge',            'THK',    'THK-OPD',    'GOOD',           800,  1850000, 'ACTIVE', 0),
    (56, 'CENT',  'Thermo Scientific Sorvall ST16 Centrifuge', 'THG',    'THG-STORE',  'GOOD',          2100,  1600000, 'ACTIVE', 0),
    (57, 'MON',   'Philips IntelliVue MX550 Patient Monitor',  'NHSL',   'NHSL-OT',    'GOOD',           400,  1450000, 'ACTIVE', 0);

UPDATE a SET code = 'MOHSL-' || t.cat || '-' || t.code || '-' || lpad(r.rn::text, 4, '0')
FROM (SELECT n, row_number() OVER (PARTITION BY typ ORDER BY n) AS rn FROM a) r, typ t
WHERE r.n = a.n AND t.code = a.typ;

INSERT INTO "Assets" ("Id", "OrganizationId", "AssetTypeId", "DepartmentId", "LocationId", "AssetCode", "Name", "Status", "Condition",
    "AcquisitionDate", "AcquisitionCost", "ResidualValue", "CumulativeMaintenanceCost", "RepairCount", "LastRepairDate", "QrPayload",
    "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT a.id, c.org, t.id, d.id, l.id, a.code, a.name, a.status, a.cond,
       pg_temp.dt(a.age), a.cost, greatest(0, round(a.cost - a.cost * (a.age / 365.25) / t.life, 2)), 0, 0, NULL, a.code,
       pg_temp.ts(least(a.age - 2, 560)), pg_temp.ts(least(a.age - 2, 560)), c.officer, c.officer
FROM a JOIN typ t ON t.code = a.typ JOIN dept d ON d.code = a.dept JOIN loc l ON l.key = a.loc CROSS JOIN ctx c;

-- Dynamic attributes for the types that define them.
INSERT INTO "AssetAttributeValues" ("Id", "AssetId", "AssetAttributeDefinitionId", "ValueText", "ValueNumber", "ValueDate", "ValueBoolean",
                                    "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT gen_random_uuid(), a.id, d.id, v.txt, v.num, v.dte, v.bool,
       pg_temp.ts(least(a.age - 2, 560)), pg_temp.ts(least(a.age - 2, 560)), c.officer, c.officer
FROM a JOIN adef d ON d.typ = a.typ CROSS JOIN ctx c
CROSS JOIN LATERAL (
    SELECT
        CASE d.name
            WHEN 'Serial number' THEN 'SN-' || upper(substr(md5(a.code), 1, 10))
            WHEN 'Manufacturer' THEN CASE
                WHEN a.name LIKE 'Hamilton%' THEN 'Hamilton Medical' WHEN a.name LIKE 'Dräger%' THEN 'Dräger'
                WHEN a.name LIKE 'Mindray%' THEN 'Mindray' WHEN a.name LIKE 'GE %' THEN 'GE Healthcare'
                WHEN a.name LIKE 'Nihon%' THEN 'Nihon Kohden' WHEN a.name LIKE 'B. Braun%' THEN 'B. Braun'
                WHEN a.name LIKE 'Fresenius%' THEN 'Fresenius Kabi' WHEN a.name LIKE 'Baxter%' THEN 'Baxter'
                WHEN a.name LIKE 'Zoll%' THEN 'Zoll' ELSE 'Philips' END
            WHEN 'Operating system' THEN CASE WHEN a.name LIKE '%3020%' THEN 'Windows 10' ELSE 'Windows 11' END
            WHEN 'Pump type' THEN CASE WHEN a.name LIKE '%Syringe%' THEN 'Syringe' ELSE 'Volumetric' END
            WHEN 'Mode' THEN CASE WHEN a.name LIKE '%AED' THEN 'AED only' ELSE 'Manual and AED' END
            WHEN 'Print type' THEN CASE WHEN a.name LIKE '%Copier%' THEN 'Multifunction copier' ELSE 'Mono laser' END
            WHEN 'Line number' THEN '+94 11 2669192'
            WHEN 'Bed type' THEN CASE WHEN a.name LIKE '%Progressa%' THEN 'ICU electric'
                                      WHEN a.name LIKE 'Stryker%' THEN 'Medical-surgical electric' ELSE 'Manual ward' END
            WHEN 'Refrigerant' THEN CASE WHEN a.name LIKE 'LG%' THEN 'R410A' WHEN a.age > 2000 THEN 'R22' ELSE 'R32' END
            WHEN 'Registration number' THEN substring(a.name FROM '\((.*)\)')
            WHEN 'Fuel type' THEN 'Diesel'
        END AS txt,
        CASE d.name
            WHEN 'Screen size (inches)' THEN CASE WHEN a.n IN (7, 57) THEN 15 ELSE 12 END
            WHEN 'Max tube current (mA)' THEN CASE a.n WHEN 13 THEN 100 ELSE 630 END
            WHEN 'RAM (GB)' THEN CASE WHEN a.typ = 'SRV' THEN CASE WHEN a.n = 35 THEN 64 ELSE 128 END
                                      WHEN a.typ = 'DSK' THEN CASE WHEN a.name LIKE '%3020%' THEN 4 ELSE 16 END
                                      WHEN a.n IN (25, 29) THEN 16 ELSE 8 END
            WHEN 'Page count' THEN CASE WHEN a.name LIKE '%Copier%' THEN 412000 WHEN a.name LIKE 'HP%' THEN 186500 ELSE 64200 END
            WHEN 'Safe working load (kg)' THEN CASE WHEN a.name LIKE '%Progressa%' THEN 250 WHEN a.name LIKE 'Stryker%' THEN 227 ELSE 180 END
            WHEN 'Cooling capacity (BTU/h)' THEN CASE WHEN a.name LIKE '%18000%' THEN 18000 ELSE 24000 END
            WHEN 'Max speed (rpm)' THEN CASE WHEN a.name LIKE 'Eppendorf%' THEN 11000 WHEN a.name LIKE 'Hettich%' THEN 15000 ELSE 15200 END
            WHEN 'Engine capacity (cc)' THEN CASE WHEN a.name LIKE 'Nissan%' THEN 4164 ELSE 2982 END
            WHEN 'Capacity (kVA)' THEN CASE a.n WHEN 48 THEN 275 WHEN 49 THEN 100 ELSE 250 END
        END AS num,
        CASE d.name
            WHEN 'Last calibration' THEN CASE WHEN a.typ = 'VENT' THEN pg_temp.dt(30 + a.n)
                                              ELSE pg_temp.dt(30 + ('x' || substr(md5(a.code), 1, 4))::bit(16)::int % 60) END
            WHEN 'Battery expiry' THEN CASE WHEN a.status IN ('CONDEMNED', 'DISPOSAL_REQUESTED', 'DISPOSED') THEN pg_temp.dt(200)
                                            ELSE pg_temp.dt(-300 - ('x' || substr(md5(a.code), 1, 4))::bit(16)::int % 200) END
            WHEN 'Radiation licence expiry' THEN pg_temp.dt(-200 - a.n * 3)
            WHEN 'Warranty expiry' THEN pg_temp.dt(a.age - 1095)
            WHEN 'Revenue licence expiry' THEN pg_temp.dt(-120 - a.n)
        END AS dte,
        CASE d.name
            WHEN 'Portable' THEN a.name LIKE '%Trilogy%'
            WHEN 'Rack mounted' THEN a.n <> 35
            WHEN 'Network enabled' THEN true
            WHEN 'Mattress included' THEN a.status <> 'CONDEMNED'
            WHEN 'Inverter' THEN a.name LIKE '%Inverter%'
            WHEN 'Refrigerated' THEN a.name LIKE '%Refrigerated%' OR a.name LIKE '%380R%'
        END AS bool
) v
WHERE num_nonnulls(v.txt, v.num, v.dte, v.bool) = 1;

-- ----------------------------------------------------------- maintenance --
-- One row per record with its lifecycle as day offsets (days ago); NULL = the
-- record never reached that step. reporter: who raised it (staff/admin =
-- fault report, officer = created directly, NULL = preventive scheduler).

CREATE TEMP TABLE m (id uuid DEFAULT gen_random_uuid(), n int, mtype text, prio text, status text, descr text, obs text,
                     est numeric, act numeric, work text, res text, reporter text,
                     req_d int, appr_d int, start_d int, done_d int, cancel_d int, cancel_reason text) ON COMMIT DROP;

-- Preventive servicing over the last ~18 months. Each asset's latest service
-- is inside its interval, so the API's scheduler only picks up the two assets
-- (48, 51) that already have an open scheduled record below.
INSERT INTO m (n, mtype, prio, status, descr, obs, est, act, work, res, reporter, req_d, appr_d, start_d, done_d)
SELECT a.n, 'PREVENTIVE', 'MEDIUM', 'COMPLETED',
       format('Scheduled preventive maintenance (interval: %s days).', t.intv),
       CASE WHEN a.cond = 'UNSERVICEABLE' THEN 'POOR' ELSE a.cond END,
       t.base, round(t.base * (0.85 + ((a.n * 7 + g.j * 11) % 35) / 100.0), -2), t.service,
       CASE WHEN a.cond = 'UNSERVICEABLE' THEN 'POOR' ELSE a.cond END,
       NULL, dd.d + 6, dd.d + 4, dd.d + 2, dd.d
FROM a JOIN typ t ON t.code = a.typ
CROSS JOIN LATERAL (SELECT CASE WHEN a.n IN (48, 51) THEN t.intv + 12 ELSE 5 + (a.n * 13) % greatest(t.intv - 25, 10) END AS off) o
CROSS JOIN LATERAL generate_series(0, 20) AS g(j)
CROSS JOIN LATERAL (SELECT o.off + g.j * t.intv AS d) dd
WHERE t.intv IS NOT NULL AND dd.d < least(a.age - 10, 540) AND dd.d > a.oos;

-- Completed corrective repairs (requested 8 days, approved 6, started 4 before completion).
INSERT INTO m (n, mtype, prio, status, descr, obs, est, act, work, res, reporter, req_d, appr_d, start_d, done_d)
SELECT x.n, 'CORRECTIVE', x.prio, 'COMPLETED', x.descr, x.obs, x.est, x.act, x.work, x.res, x.reporter, x.d + 8, x.d + 6, x.d + 4, x.d
FROM (VALUES
    ( 2, 'HIGH',     'High-pressure alarm during ventilation; exhalation valve suspected.',          'FAIR', 150000, 182000, 'Replaced exhalation valve assembly and recalibrated the flow sensor.',                 'FAIR', 'staff',   400),
    ( 2, 'CRITICAL', 'Ventilator shuts down intermittently during patient use.',                     'POOR', 250000, 268000, 'Replaced main power supply board; 24-hour burn-in test passed.',                      'FAIR', 'staff',   250),
    ( 2, 'HIGH',     'Oxygen sensor reading drifting; O2 alarm keeps triggering.',                   'POOR',  90000, 115000, 'Replaced O2 cell and blower motor bearings.',                                         'FAIR', 'officer', 130),
    ( 2, 'CRITICAL', 'Blower unit noisy and overheating after four hours of use.',                   'POOR', 380000, 412000, 'Replaced turbine blower module; returned to service under observation.',             'FAIR', 'staff',    45),
    (37, 'LOW',      'Paper jams on every second print job.',                                        'FAIR',  12000,  14500, 'Replaced pickup roller and separation pad.',                                          'FAIR', 'staff',   600),
    (37, 'MEDIUM',   'Prints show dark streaks and smudging.',                                       'FAIR',  22000,  26800, 'Replaced fuser unit.',                                                                'FAIR', 'staff',   380),
    (37, 'MEDIUM',   'Printer not detected on the network; error 49 on the display.',                'POOR',  18000,  31000, 'Replaced formatter board and reinstalled firmware.',                                  'FAIR', 'officer', 160),
    (35, 'HIGH',     'RAID controller reporting a degraded array.',                                  'FAIR', 140000, 162000, 'Replaced failed SAS drive and rebuilt the RAID 5 array.',                             'FAIR', 'officer', 500),
    (35, 'CRITICAL', 'Server rebooting unexpectedly under load.',                                    'POOR', 220000, 245000, 'Replaced redundant power supply and system-board capacitor bank.',                   'POOR', 'officer', 210),
    (35, 'HIGH',     'Fan failure warning; CPU temperature above threshold.',                        'POOR',  60000,  78000, 'Replaced two system fans and re-applied thermal paste on both CPUs.',                'POOR', 'officer',  70),
    (33, 'MEDIUM',   'Correctable ECC memory errors logged in iLO.',                                 'FAIR',  85000,  79000, 'Replaced 2 x 32 GB DIMMs.',                                                           'FAIR', 'officer', 320),
    (45, 'HIGH',     'Gearbox slipping when climbing gradients.',                                    'FAIR', 320000, 356000, 'Gearbox overhauled; clutch plate and release bearing replaced.',                     'FAIR', 'staff',   290),
    (45, 'HIGH',     'Rear suspension leaf spring cracked.',                                         'FAIR', 145000, 139000, 'Replaced rear leaf spring set and shock absorbers.',                                  'FAIR', 'admin',    95),
    (16, 'HIGH',     'Door sensor error; pump refuses to start an infusion.',                        'FAIR',  35000,  32500, 'Replaced door latch sensor; flow-rate accuracy test passed.',                         'GOOD', 'staff',   180),
    (30, 'LOW',      'Desktop very slow; disk health warning on start-up.',                          'FAIR',  25000,  23800, 'Replaced HDD with a 512 GB SSD and restored the user profile.',                       'GOOD', 'staff',   220),
    (40, 'MEDIUM',   'Bed height actuator not responding.',                                          'FAIR',  65000,  71000, 'Replaced lift actuator motor.',                                                       'GOOD', 'staff',   140),
    (13, 'HIGH',     'Image artefacts on the detector panel.',                                       'FAIR', 650000, 720000, 'Detector panel recalibrated; replaced the cable harness.',                            'FAIR', 'officer', 330),
    (48, 'CRITICAL', 'Generator failed to auto-start during the mains outage test.',                 'FAIR', 180000, 196000, 'Replaced starter motor and ATS control relay.',                                       'FAIR', 'officer', 260),
    ( 6, 'MEDIUM',   'Touchscreen unresponsive in the lower half.',                                  'FAIR', 120000, 118000, 'Replaced touchscreen digitiser.',                                                     'FAIR', 'officer', 410),
    (12, 'MEDIUM',   'SpO2 module readings erratic.',                                                'FAIR',  45000,  47500, 'Replaced SpO2 module.',                                                               'FAIR', 'officer', 120),
    (53, 'HIGH',     'Not cooling; compressor keeps tripping.',                                      'POOR',  55000,  61000, 'Recharged refrigerant and replaced the run capacitor.',                               'POOR', 'officer',  75)
) AS x(n, prio, descr, obs, est, act, work, res, reporter, d);

-- Open and cancelled work: the live queue each role acts on.
INSERT INTO m (n, mtype, prio, status, descr, obs, est, reporter, req_d, appr_d, start_d, cancel_d, cancel_reason) VALUES
    -- Staff fault reports waiting for approval.
    (17, 'CORRECTIVE', 'MEDIUM',   'REQUESTED',   'Occlusion alarm triggers repeatedly even with a clear, unkinked line.',      'POOR', NULL,   'staff',    2, NULL, NULL, NULL, NULL),
    (41, 'CORRECTIVE', 'MEDIUM',   'REQUESTED',   'Left side rail latch does not lock; the rail drops when pushed.',            'FAIR', NULL,   'staff',    4, NULL, NULL, NULL, NULL),
    -- Officer-raised request waiting for approval.
    (36, 'CORRECTIVE', 'LOW',      'REQUESTED',   'Toner low warning persists after replacing the cartridge.',                  'GOOD', 9000,   'officer',  1, NULL, NULL, NULL, NULL),
    -- Raised by the preventive scheduler (FR-041).
    (48, 'PREVENTIVE', 'MEDIUM',   'REQUESTED',   'Scheduled preventive maintenance (interval: 90 days).',                       'FAIR', NULL,   NULL,       8, NULL, NULL, NULL, NULL),
    (51, 'PREVENTIVE', 'MEDIUM',   'REQUESTED',   'Scheduled preventive maintenance (interval: 180 days).',                      'GOOD', NULL,   NULL,       6, NULL, NULL, NULL, NULL),
    -- Approved, ready for the officer to start.
    (18, 'CORRECTIVE', 'HIGH',     'APPROVED',    'Pump shows an "Air in line" alarm with no air in the line.',                 'FAIR', 28000,  'staff',    9,    3, NULL, NULL, NULL),
    -- In progress, ready for the officer to complete (FR-038).
    ( 8, 'CORRECTIVE', 'HIGH',     'IN_PROGRESS', 'Screen flickers and goes black after about 10 minutes of use.',              'FAIR', 65000,  'staff',   10,    7,    5, NULL, NULL),
    (13, 'CORRECTIVE', 'CRITICAL', 'IN_PROGRESS', 'Detector not acquiring images; generator error E-204 on start-up.',          'POOR', 450000, 'officer', 15,   12,    9, NULL, NULL),
    ( 6, 'CORRECTIVE', 'HIGH',     'IN_PROGRESS', 'Expiratory flow sensor failing the pre-use self-test.',                      'POOR', 95000,  'officer', 12,   10,    8, NULL, NULL),
    -- Cancelled.
    (17, 'CORRECTIVE', 'MEDIUM',   'CANCELLED',   'Infusion pump alarm beeping continuously.',                                  'FAIR', NULL,   'staff',   24, NULL, NULL,   21, 'Fault could not be reproduced; the occlusion was caused by a kinked giving set.'),
    (52, 'CORRECTIVE', 'LOW',      'CANCELLED',   'Air conditioner remote not working.',                                        'FAIR', 4000,   'officer', 48,   46, NULL,   44, 'Remote batteries replaced by the ward; no repair needed.');

INSERT INTO "MaintenanceRecords" ("Id", "OrganizationId", "AssetId", "Description", "ObservedCondition", "PhotoObjectKey", "Type", "Priority",
    "Status", "EstimatedCost", "ActualCost", "WorkPerformed", "CompletionDate", "ResultingCondition", "AssigneeId", "CancellationReason",
    "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "ReportedByUserId")
SELECT m.id, c.org, a.id, m.descr, m.obs, NULL, m.mtype, m.prio, m.status,
       CASE WHEN m.appr_d IS NOT NULL OR m.reporter = 'officer' THEN m.est END,
       m.act, m.work, pg_temp.dt(m.done_d), m.res,
       CASE WHEN m.appr_d IS NOT NULL THEN c.officer END,
       m.cancel_reason,
       pg_temp.ts(m.req_d, 15), pg_temp.ts(coalesce(m.done_d, m.cancel_d, m.start_d, m.appr_d, m.req_d), 45),
       r.uid,
       CASE WHEN m.done_d IS NOT NULL OR m.start_d IS NOT NULL OR m.cancel_d IS NOT NULL THEN c.officer
            WHEN m.appr_d IS NOT NULL THEN c.admin ELSE r.uid END,
       CASE WHEN m.reporter IN ('staff', 'admin') THEN r.uid END
FROM m JOIN a ON a.n = m.n CROSS JOIN ctx c
CROSS JOIN LATERAL (SELECT CASE m.reporter WHEN 'staff' THEN c.staff WHEN 'admin' THEN c.admin WHEN 'officer' THEN c.officer END AS uid) r;

-- Running totals the completion step maintains (FR-040).
UPDATE "Assets" s
SET "CumulativeMaintenanceCost" = x.total, "RepairCount" = x.cnt, "LastRepairDate" = x.last,
    "UpdatedAt" = greatest(s."UpdatedAt", x.last_ts), "UpdatedBy" = (SELECT officer FROM ctx)
FROM (SELECT a.id, sum(m.act) AS total, count(*) AS cnt, pg_temp.dt(min(m.done_d)) AS last, pg_temp.ts(min(m.done_d), 60) AS last_ts
      FROM m JOIN a ON a.n = m.n WHERE m.status = 'COMPLETED' GROUP BY a.id) x
WHERE s."Id" = x.id;

-- ------------------------------------------------------------- transfers --

CREATE TEMP TABLE tr (n int, fd text, fl text, td text, tl text, st int, req int, appr int, conf int, reason text,
                      id uuid DEFAULT gen_random_uuid()) ON COMMIT DROP;
-- st: 0 REQUESTED, 1 APPROVED (asset in transit), 3 COMPLETED, 4 REJECTED
INSERT INTO tr (n, fd, fl, td, tl, st, req, appr, conf, reason) VALUES
    ( 4, 'NHSL', 'NHSL-ETU',   'THA',    'THA-ETU',   0,   1, NULL, NULL, NULL),
    (26, 'NHSL', 'NHSL-STORE', 'MOH-HQ', 'HQ-REC',    0,   3, NULL, NULL, NULL),
    ( 9, 'NHSL', 'NHSL-ETU',   'THK',    'THK-ICU',   1,   5,    2, NULL, NULL),
    (43, 'THJ',  'THJ-PAED',   'THA',    'THA-ETU',   1,  16,   12, NULL, NULL),
    (14, 'NHSL', 'NHSL-RAD',   'THK',    'THK-OPD',   3, 250,  247,  240, NULL),
    (55, 'NHSL', 'NHSL-LAB',   'THK',    'THK-OPD',   3, 160,  158,  150, NULL),
    (25, 'THK',  'THK-OPD',    'MOH-HQ', 'HQ-REC',    3,  95,   94,   90, NULL),
    (49, 'THJ',  'THJ-STORE',  'THA',    'THA-STORE', 4,  60,   58, NULL,
        'This is the only standby generator at Jaffna; moving it would leave the paediatric ward without backup power.');

INSERT INTO "AssetTransfers" ("Id", "OrganizationId", "AssetId", "FromDepartmentId", "ToDepartmentId", "FromLocationId", "ToLocationId",
    "InitiatedByUserId", "ApprovedByUserId", "ConfirmedByUserId", "Status", "RequestedAt", "ApprovedAt", "ConfirmedAt", "RejectionReason")
SELECT tr.id, c.org, a.id, fd.id, td.id, fl.id, tl.id, c.officer,
       CASE WHEN tr.st IN (1, 3) THEN c.admin END, CASE WHEN tr.st = 3 THEN c.officer END,
       tr.st, pg_temp.ts(tr.req, 20), CASE WHEN tr.st IN (1, 3) THEN pg_temp.ts(tr.appr, 90) END,
       CASE WHEN tr.st = 3 THEN pg_temp.ts(tr.conf, 120) END, tr.reason
FROM tr JOIN a ON a.n = tr.n JOIN dept fd ON fd.code = tr.fd JOIN dept td ON td.code = tr.td
JOIN loc fl ON fl.key = tr.fl JOIN loc tl ON tl.key = tr.tl CROSS JOIN ctx c;

-- ------------------------------------------------------------- disposals --

CREATE TEMP TABLE ds (n int, method int, val numeric, st int, req int, dec int, val_d int, reason text, notes text,
                      id uuid DEFAULT gen_random_uuid()) ON COMMIT DROP;
-- method: 0 SCRAP, 1 AUCTION, 2 DONATION, 3 DESTROY; st: 0 PENDING, 1 APPROVED, 2 REJECTED, 3 REVISION_REQUESTED
INSERT INTO ds (n, method, val, st, req, dec, val_d, reason, notes) VALUES
    (32, 0,  8000, 0,   2, NULL,  10, 'Motherboard failed; parts no longer available.',
        'Hard disk to be wiped and physically destroyed before scrapping.'),
    (24, 0,  5000, 3,  20,   15, 130, 'Fails self-test; manufacturer no longer supplies batteries or pads.',
        'Device decommissioned by the biomedical engineering unit.' || E'\n' ||
        '[Revision Requested - ' || to_char(pg_temp.ts(15, 60) AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI:SS') || ' UTC]: ' ||
        'The valuation is older than the 90-day validity window. Please attach a fresh valuation before resubmitting.'),
    (11, 1, 45000, 1, 150,  140, 160, 'Display failed and spare parts are obsolete.',
        'Sold at the quarterly public auction of medical equipment.'),
    (39, 0,     0, 1, 230,  220, 235, 'Obsolete technology; replaced by e-mail and scanning.',
        NULL),
    (44, 2, 12000, 2, 100,   90, 105, 'Frame corroded at the castor mounts.',
        'Proposed donation to a rural divisional hospital.' || E'\n' ||
        '[Rejected - ' || to_char(pg_temp.ts(90, 60) AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI:SS') || ' UTC]: ' ||
        'The frame can be refurbished locally for under LKR 20,000; repair it instead.');

INSERT INTO "DisposalRequests" ("Id", "OrganizationId", "AssetId", "InitiatedByUserId", "ApprovedByUserId", "DisposalMethod",
    "EstimatedResidualValue", "Status", "RequestedAt", "ApprovedAt", "DisposedAt", "Notes", "ValuationDate")
SELECT ds.id, c.org, a.id, c.officer, CASE WHEN ds.st = 1 THEN c.admin END, ds.method, ds.val, ds.st,
       pg_temp.ts(ds.req, 30), CASE WHEN ds.st = 1 THEN pg_temp.ts(ds.dec, 60) END,
       CASE WHEN ds.st = 1 THEN pg_temp.ts(ds.dec, 60) END, ds.notes, pg_temp.dt(ds.val_d)
FROM ds JOIN a ON a.n = ds.n CROSS JOIN ctx c;

-- ---------------------------------------------------------- verification --

CREATE TEMP TABLE camp (key text PRIMARY KEY, name text, ps int, pe int, sdept text, scat text, st int, created int,
                        id uuid DEFAULT gen_random_uuid()) ON COMMIT DROP;
-- st: 0 Active, 1 Completed, 2 Cancelled
INSERT INTO camp (key, name, ps, pe, sdept, scat, st, created) VALUES
    ('Q2MED',  'Q2 2026 NHSL Medical Equipment Verification',        170, 140, 'NHSL', 'MED', 1, 172),
    ('Q4IT',   'Q4 2026 Organisation-wide IT Equipment Audit',         10, -20, NULL,   'IT',  0,  11),
    ('THK26',  'Teaching Hospital Kandy - Annual Verification 2026',    3, -27, 'THK',  NULL,  0,   4),
    ('THJFUR', 'Jaffna Furniture Count - September 2026',              40,  25, 'THJ',  'FUR', 2,  42);

INSERT INTO "VerificationCampaigns" ("Id", "OrganizationId", "Name", "PeriodStart", "PeriodEnd", "ScopeDepartmentId", "ScopeLocationId",
    "ScopeAssetCategoryId", "ScopeAssetTypeId", "Status", "CreatedByUserId", "CreatedAt")
SELECT k.id, c.org, k.name, pg_temp.dt(k.ps), pg_temp.dt(k.pe), d.id, NULL, ct.id, NULL, k.st, c.auditor, pg_temp.ts(k.created, 10)
FROM camp k LEFT JOIN dept d ON d.code = k.sdept LEFT JOIN cat ct ON ct.code = k.scat CROSS JOIN ctx c;

-- Tasks: one per in-scope asset that existed when the campaign started
-- (the cancelled campaign was stopped before any were generated).
CREATE TEMP TABLE vt (id uuid DEFAULT gen_random_uuid(), camp text, n int, done int, present boolean, aloc text, acond text) ON COMMIT DROP;
INSERT INTO vt (camp, n)
SELECT k.key, a.n
FROM camp k JOIN a ON (k.sdept IS NULL OR a.dept = k.sdept) JOIN typ t ON t.code = a.typ
WHERE k.st <> 2 AND (k.scat IS NULL OR t.cat = k.scat) AND a.age > k.created
  AND (a.status <> 'DISPOSED' OR a.oos < k.created)
  AND NOT (k.key = 'THK26' AND a.n IN (9));   -- still in transit, not yet Kandy's

-- Asset 14 only arrived at Kandy after Q2, and 55 left NHSL for Kandy before it.
DELETE FROM vt WHERE camp = 'Q2MED' AND n IN (14, 55);

-- Q2 campaign: every task completed, two mismatches.
UPDATE vt SET done = 165 - (vt.n % 20), present = true, aloc = a.loc, acond = a.cond
FROM a WHERE a.n = vt.n AND vt.camp = 'Q2MED';
UPDATE vt SET aloc = 'NHSL-ICU' WHERE camp = 'Q2MED' AND n = 21;
UPDATE vt SET acond = 'POOR' WHERE camp = 'Q2MED' AND n = 17;

-- Q4 IT audit: about half done, three mismatches found so far.
UPDATE vt SET done = x.d, present = true, aloc = a.loc, acond = a.cond
FROM a, (VALUES (25, 9), (29, 9), (30, 8), (31, 7), (33, 6), (34, 6), (36, 5), (27, 4), (38, 2)) AS x(n, d)
WHERE a.n = vt.n AND x.n = vt.n AND vt.camp = 'Q4IT';
UPDATE vt SET present = false, aloc = NULL, acond = NULL WHERE camp = 'Q4IT' AND n = 27;
UPDATE vt SET aloc = 'THK-STORE' WHERE camp = 'Q4IT' AND n = 38;
UPDATE vt SET acond = 'FAIR' WHERE camp = 'Q4IT' AND n = 34;

INSERT INTO "VerificationTasks" ("Id", "OrganizationId", "CampaignId", "AssetId", "AssignedToUserId", "DueDate", "Status",
    "AssertedPresent", "AssertedLocationId", "AssertedCondition", "CompletedByUserId", "CompletedAt", "CreatedAt")
SELECT vt.id, c.org, k.id, a.id, c.officer, pg_temp.dt(k.pe), CASE WHEN vt.done IS NULL THEN 0 ELSE 1 END,
       vt.present, l.id, vt.acond, CASE WHEN vt.done IS NOT NULL THEN c.officer END,
       pg_temp.ts(vt.done, 60 + vt.n), pg_temp.ts(k.created, 10)
FROM vt JOIN camp k ON k.key = vt.camp JOIN a ON a.n = vt.n LEFT JOIN loc l ON l.key = vt.aloc CROSS JOIN ctx c;

-- Discrepancies. type: 0 Missing, 2 LocationMismatch, 3 ConditionMismatch, 4 DataMismatch, 5 Other; status: 0 Open, 1 Resolved.
CREATE TEMP TABLE dsc (camp text, n int, type int, auto boolean, raised text, descr text, st int, rtype text, rexpl text, ract text,
                       corrected boolean, res_d int, created int) ON COMMIT DROP;
INSERT INTO dsc VALUES
    ('Q2MED', 21, 2, true,  NULL,      'Found in ICU - Ward 5; register says Emergency Treatment Unit.', 1, 'ASSET_RELOCATED',
        'The defibrillator had been borrowed by the ICU during a cardiac arrest and not returned.', 'Returned to the Emergency Treatment Unit the same day.', false, 140, NULL),
    ('Q2MED', 17, 3, true,  NULL,      'Observed condition POOR; register says GOOD.', 1, 'CONDITION_UPDATED',
        'Pump housing cracked and keypad worn; confirmed by the biomedical engineering unit.', 'Register condition updated; pump scheduled for repair.', true, 138, NULL),
    ('Q4IT',  27, 0, true,  NULL,      'Asset not found at Head Office Records Room.', 0, NULL, NULL, NULL, false, NULL, NULL),
    ('Q4IT',  38, 2, true,  NULL,      'Found in Kandy Central Store; register says Kandy OPD.', 0, NULL, NULL, NULL, false, NULL, NULL),
    ('Q4IT',  34, 3, true,  NULL,      'Observed condition FAIR; register says GOOD.', 0, NULL, NULL, NULL, false, NULL, NULL),
    (NULL,    50, 4, false, 'auditor', 'Nameplate reads 275 kVA; the register records 250 kVA.', 0, NULL, NULL, NULL, false, NULL, 6),
    (NULL,    46, 5, false, 'auditor', 'Revenue licence sticker on the vehicle has expired (register shows it valid).', 1, 'REGISTER_CORRECTED',
        'Licence lapsed while the vehicle was off the road for repairs.', 'Licence renewed at the Divisional Secretariat; expiry date updated in the register.', true, 45, 50);

INSERT INTO "Discrepancies" ("Id", "OrganizationId", "CampaignId", "VerificationTaskId", "AssetId", "Type", "IsAutomatic", "RaisedByUserId",
    "Description", "PhotoUrl", "Status", "ResolutionType", "ResolutionExplanation", "CorrectiveAction", "RegisterCorrected",
    "ResolvedByUserId", "ResolvedAt", "CreatedAt")
SELECT gen_random_uuid(), c.org, k.id, vt.id, a.id, x.type, x.auto, CASE WHEN x.raised = 'auditor' THEN c.auditor END,
       x.descr, NULL, x.st, x.rtype, x.rexpl, x.ract, x.corrected,
       CASE WHEN x.st = 1 THEN c.auditor END, pg_temp.ts(x.res_d, 200),
       coalesce(pg_temp.ts(x.created, 30), pg_temp.ts(vt.done, 60 + vt.n))
FROM dsc x JOIN a ON a.n = x.n LEFT JOIN camp k ON k.key = x.camp LEFT JOIN vt ON vt.camp = x.camp AND vt.n = x.n CROSS JOIN ctx c;

-- --------------------------------------------------------- asset history --

INSERT INTO "AssetHistory" ("Id", "OrganizationId", "AssetId", "ActorUserId", "EventType", "Description", "PreviousValue", "NewValue", "CreatedAt")
SELECT gen_random_uuid(), c.org, h.asset, h.actor, h.evt, h.descr, h.prev, h.new, h.at
FROM ctx c CROSS JOIN LATERAL (
    -- Registration.
    SELECT a.id AS asset, c.officer AS actor, 'STATUS_CHANGE' AS evt, 'Asset registered.' AS descr,
           NULL::jsonb AS prev, '{"status": "ACTIVE"}'::jsonb AS new, pg_temp.ts(least(a.age - 2, 560)) AS at
    FROM a
    UNION ALL
    -- Maintenance started.
    SELECT a.id, c.officer, 'MAINTENANCE', format('Maintenance record %s started — asset placed UNDER_MAINTENANCE.', m.id),
           '{"status": "ACTIVE"}', '{"status": "UNDER_MAINTENANCE"}', pg_temp.ts(m.start_d, 30)
    FROM m JOIN a ON a.n = m.n WHERE m.start_d IS NOT NULL
    UNION ALL
    -- Maintenance completed.
    SELECT a.id, c.officer, 'MAINTENANCE',
           format('Maintenance record %s completed. Asset condition updated from %s to %s. Asset status set to ACTIVE.', m.id, m.obs, m.res),
           jsonb_build_object('status', 'UNDER_MAINTENANCE', 'condition', m.obs),
           jsonb_build_object('status', 'ACTIVE', 'condition', m.res, 'lastRepairDate', pg_temp.dt(m.done_d)),
           pg_temp.ts(m.done_d, 60)
    FROM m JOIN a ON a.n = m.n WHERE m.status = 'COMPLETED'
    UNION ALL
    -- Transfer requested.
    SELECT a.id, c.officer, 'TRANSFER', format('Transfer requested to %s / %s.', td.name, tl.name),
           '{"status": "ACTIVE"}', jsonb_build_object('status', 'TRANSFER_REQUESTED', 'toDepartmentId', td.id, 'toLocationId', tl.id),
           pg_temp.ts(tr.req, 20)
    FROM tr JOIN a ON a.n = tr.n JOIN dept td ON td.code = tr.td JOIN loc tl ON tl.key = tr.tl
    UNION ALL
    -- Transfer approved / rejected.
    SELECT a.id, c.admin, 'TRANSFER',
           CASE WHEN tr.st = 4 THEN 'Transfer rejected: ' || tr.reason ELSE 'Transfer approved; asset in transit.' END,
           '{"status": "TRANSFER_REQUESTED"}',
           CASE WHEN tr.st = 4 THEN '{"status": "ACTIVE"}'::jsonb ELSE '{"status": "IN_TRANSIT"}'::jsonb END,
           pg_temp.ts(tr.appr, 90)
    FROM tr JOIN a ON a.n = tr.n WHERE tr.st IN (1, 3, 4)
    UNION ALL
    -- Transfer completed.
    SELECT a.id, c.officer, 'TRANSFER', 'Transfer completed; asset received.',
           jsonb_build_object('status', 'IN_TRANSIT', 'departmentId', fd.id, 'locationId', fl.id),
           jsonb_build_object('status', 'ACTIVE', 'departmentId', td.id, 'locationId', tl.id),
           pg_temp.ts(tr.conf, 120)
    FROM tr JOIN a ON a.n = tr.n JOIN dept fd ON fd.code = tr.fd JOIN loc fl ON fl.key = tr.fl
    JOIN dept td ON td.code = tr.td JOIN loc tl ON tl.key = tr.tl WHERE tr.st = 3
    UNION ALL
    -- Condemned.
    SELECT a.id, c.officer, 'STATUS_CHANGE', 'Asset condemned: ' || coalesce(ds.reason, 'Beyond economic repair.'),
           '{"status": "ACTIVE"}', '{"status": "CONDEMNED", "evidenceUrl": null}', pg_temp.ts(a.oos, 10)
    FROM a LEFT JOIN ds ON ds.n = a.n WHERE a.status IN ('CONDEMNED', 'DISPOSAL_REQUESTED', 'DISPOSED')
    UNION ALL
    -- Disposed / disposal rejected / returned for revision.
    SELECT a.id, c.admin, 'DISPOSAL',
           CASE WHEN ds.st = 1 THEN 'Asset disposed via ' || (ARRAY['SCRAP', 'AUCTION', 'DONATION', 'DESTROY'])[ds.method + 1] || '.'
                WHEN ds.st = 3 THEN 'Disposal request returned for revision: The valuation is older than the 90-day validity window. Please attach a fresh valuation before resubmitting.'
                ELSE 'Disposal request rejected: The frame can be refurbished locally for under LKR 20,000; repair it instead.' END,
           '{"status": "DISPOSAL_REQUESTED"}',
           CASE WHEN ds.st = 1 THEN '{"status": "DISPOSED"}'::jsonb ELSE '{"status": "CONDEMNED"}'::jsonb END,
           pg_temp.ts(ds.dec, 60)
    FROM ds JOIN a ON a.n = ds.n WHERE ds.st IN (1, 2, 3)
    UNION ALL
    -- Verification task completions.
    SELECT a.id, c.officer, 'VERIFICATION',
           CASE WHEN vt.present THEN format('Verified present at completion of task %s.', vt.id)
                ELSE format('Verified NOT present at completion of task %s.', vt.id) END,
           NULL, jsonb_build_object('assertedPresent', vt.present, 'assertedLocationId', l.id, 'assertedCondition', vt.acond),
           pg_temp.ts(vt.done, 60 + vt.n)
    FROM vt JOIN a ON a.n = vt.n LEFT JOIN loc l ON l.key = vt.aloc WHERE vt.done IS NOT NULL
) h;

-- --------------------------------------------------------- notifications --

INSERT INTO "Notifications" ("Id", "OrganizationId", "RecipientUserId", "Type", "Title", "Message", "RelatedEntityType", "RelatedEntityId",
                             "IsRead", "CreatedAt")
SELECT gen_random_uuid(), c.org, x.recipient, x.type, x.title, x.msg, 'MaintenanceRecord', x.rec, x.d > x.read_after, pg_temp.ts(x.d, x.mins)
FROM ctx c CROSS JOIN LATERAL (
    -- Status updates to whoever reported the fault.
    SELECT CASE m.reporter WHEN 'staff' THEN c.staff ELSE c.admin END AS recipient, 'MAINTENANCE_STATUS_CHANGED' AS type,
           'Fault report status updated' AS title, format('Your fault report for %s is now %s.', a.code, s.status) AS msg,
           m.id AS rec, s.d, 14 AS read_after, s.mins
    FROM m JOIN a ON a.n = m.n
    CROSS JOIN LATERAL (VALUES ('APPROVED', m.appr_d, 50), ('IN_PROGRESS', m.start_d, 35), ('COMPLETED', m.done_d, 65), ('CANCELLED', m.cancel_d, 50))
        AS s(status, d, mins)
    WHERE m.reporter IN ('staff', 'admin') AND s.d IS NOT NULL
    UNION ALL
    -- Assignment to the officer (corrective work, plus the last two months of servicing).
    SELECT c.officer, 'MAINTENANCE_ASSIGNED', 'Maintenance assigned to you',
           format('You''ve been assigned maintenance for %s: %s', a.code, m.descr), m.id, m.appr_d, 7, 50
    FROM m JOIN a ON a.n = m.n
    WHERE m.appr_d IS NOT NULL AND (m.mtype = 'CORRECTIVE' OR m.appr_d <= 60)
    UNION ALL
    -- Cancellation after assignment.
    SELECT c.officer, 'MAINTENANCE_CANCELLED', 'Maintenance cancelled',
           format('Maintenance for %s was cancelled: %s', a.code, m.cancel_reason), m.id, m.cancel_d, 7, 50
    FROM m JOIN a ON a.n = m.n WHERE m.cancel_d IS NOT NULL AND m.appr_d IS NOT NULL AND m.reporter <> 'officer'
) x;

-- ------------------------------------------------------------- audit log --
-- What AuditSaveChangesInterceptor would have written: a Create entry per
-- seeded row (every field, before = null) and Update entries for each
-- workflow transition. Enum fields are logged as their integer values.

DO $$
DECLARE
    c record;
    r record;
BEGIN
    SELECT * INTO c FROM ctx;
    FOR r IN SELECT * FROM (VALUES
        ('Departments',               'Department',               't."CreatedBy"',          't."CreatedAt"',   't."OrganizationId" = $1'),
        ('Locations',                 'Location',                 't."CreatedBy"',          't."CreatedAt"',   't."OrganizationId" = $1'),
        ('AssetCategories',           'AssetCategory',            't."CreatedBy"',          't."CreatedAt"',   't."OrganizationId" = $1'),
        ('AssetTypes',                'AssetType',                't."CreatedBy"',          't."CreatedAt"',   't."OrganizationId" = $1'),
        ('AssetAttributeDefinitions', 'AssetAttributeDefinition', 't."CreatedBy"',          't."CreatedAt"',
            't."AssetTypeId" IN (SELECT "Id" FROM "AssetTypes" WHERE "OrganizationId" = $1)'),
        ('OrganizationPolicies',      'OrganizationPolicy',       't."CreatedBy"',          't."CreatedAt"',   't."OrganizationId" = $1'),
        ('Assets',                    'Asset',                    't."CreatedBy"',          't."CreatedAt"',   't."OrganizationId" = $1'),
        ('AssetAttributeValues',      'AssetAttributeValue',      't."CreatedBy"',          't."CreatedAt"',
            't."AssetId" IN (SELECT "Id" FROM "Assets" WHERE "OrganizationId" = $1)'),
        ('MaintenanceRecords',        'MaintenanceRecord',        't."CreatedBy"',          't."CreatedAt"',   't."OrganizationId" = $1'),
        ('AssetTransfers',            'AssetTransfer',            't."InitiatedByUserId"',  't."RequestedAt"', 't."OrganizationId" = $1'),
        ('DisposalRequests',          'DisposalRequest',          't."InitiatedByUserId"',  't."RequestedAt"', 't."OrganizationId" = $1'),
        ('VerificationCampaigns',     'VerificationCampaign',     't."CreatedByUserId"',    't."CreatedAt"',   't."OrganizationId" = $1'),
        ('VerificationTasks',         'VerificationTask',         '$2',                     't."CreatedAt"',   't."OrganizationId" = $1'),
        ('Discrepancies',             'Discrepancy',              't."RaisedByUserId"',     't."CreatedAt"',   't."OrganizationId" = $1'),
        ('AssetHistory',              'AssetHistory',             't."ActorUserId"',        't."CreatedAt"',   't."OrganizationId" = $1'),
        ('Notifications',             'Notification',             'NULL::uuid',             't."CreatedAt"',   't."OrganizationId" = $1')
    ) AS v(tbl, entity, actor, at, filter)
    LOOP
        EXECUTE format(
            'INSERT INTO "AuditLogEntries" ("Id", "OrganizationId", "ActorUserId", "EntityType", "EntityId", "Operation", "Changes", "CorrelationId", "CreatedAt")
             SELECT gen_random_uuid(), $1, %s, %L, t."Id", ''Create'',
                    (SELECT jsonb_agg(jsonb_build_object(''field'', e.key, ''before'', NULL, ''after'', e.value) ORDER BY e.key) FROM jsonb_each(to_jsonb(t)) e),
                    gen_random_uuid(), %s
             FROM %I t WHERE %s',
            r.actor, r.entity, r.at, r.tbl, r.filter)
        USING c.org, c.auditor;
    END LOOP;
END $$;

INSERT INTO "AuditLogEntries" ("Id", "OrganizationId", "ActorUserId", "EntityType", "EntityId", "Operation", "Changes", "CorrelationId", "CreatedAt")
SELECT gen_random_uuid(), c.org, u.actor, u.entity, u.entity_id, 'Update', u.changes, gen_random_uuid(), u.at
FROM ctx c CROSS JOIN LATERAL (
    -- Maintenance lifecycle (Status: 0 REQUESTED, 1 APPROVED, 2 IN_PROGRESS, 3 COMPLETED, 4 CANCELLED).
    SELECT c.admin AS actor, 'MaintenanceRecord' AS entity, m.id AS entity_id,
           jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 0, 'after', 1),
                             jsonb_build_object('field', 'AssigneeId', 'before', NULL, 'after', c.officer),
                             jsonb_build_object('field', 'EstimatedCost', 'before', NULL, 'after', m.est)) AS changes,
           pg_temp.ts(m.appr_d, 45) AS at
    FROM m WHERE m.appr_d IS NOT NULL
    UNION ALL
    SELECT c.officer, 'MaintenanceRecord', m.id, jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 1, 'after', 2)), pg_temp.ts(m.start_d, 30)
    FROM m WHERE m.start_d IS NOT NULL
    UNION ALL
    SELECT c.officer, 'MaintenanceRecord', m.id,
           jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 2, 'after', 3),
                             jsonb_build_object('field', 'ActualCost', 'before', NULL, 'after', m.act),
                             jsonb_build_object('field', 'CompletionDate', 'before', NULL, 'after', pg_temp.dt(m.done_d)),
                             jsonb_build_object('field', 'ResultingCondition', 'before', NULL, 'after', m.res)),
           pg_temp.ts(m.done_d, 60)
    FROM m WHERE m.done_d IS NOT NULL
    UNION ALL
    SELECT c.officer, 'MaintenanceRecord', m.id,
           jsonb_build_array(jsonb_build_object('field', 'Status', 'before', CASE WHEN m.appr_d IS NULL THEN 0 ELSE 1 END, 'after', 4),
                             jsonb_build_object('field', 'CancellationReason', 'before', NULL, 'after', m.cancel_reason)),
           pg_temp.ts(m.cancel_d, 45)
    FROM m WHERE m.cancel_d IS NOT NULL
    UNION ALL
    -- Asset status changes driven by maintenance.
    SELECT c.officer, 'Asset', a.id, jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 'ACTIVE', 'after', 'UNDER_MAINTENANCE')),
           pg_temp.ts(m.start_d, 30)
    FROM m JOIN a ON a.n = m.n WHERE m.start_d IS NOT NULL
    UNION ALL
    SELECT c.officer, 'Asset', a.id,
           jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 'UNDER_MAINTENANCE', 'after', 'ACTIVE'),
                             jsonb_build_object('field', 'Condition', 'before', m.obs, 'after', m.res)),
           pg_temp.ts(m.done_d, 60)
    FROM m JOIN a ON a.n = m.n WHERE m.done_d IS NOT NULL
    UNION ALL
    -- Transfers (Status: 0 REQUESTED, 1 APPROVED, 3 COMPLETED, 4 REJECTED).
    SELECT c.admin, 'AssetTransfer', tr.id,
           jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 0, 'after', CASE WHEN tr.st = 4 THEN 4 ELSE 1 END)),
           pg_temp.ts(tr.appr, 90)
    FROM tr WHERE tr.appr IS NOT NULL
    UNION ALL
    SELECT c.officer, 'AssetTransfer', tr.id, jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 1, 'after', 3)), pg_temp.ts(tr.conf, 120)
    FROM tr WHERE tr.conf IS NOT NULL
    UNION ALL
    -- Disposals (Status: 0 PENDING, 1 APPROVED, 2 REJECTED, 3 REVISION_REQUESTED).
    SELECT c.admin, 'DisposalRequest', ds.id, jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 0, 'after', ds.st)), pg_temp.ts(ds.dec, 60)
    FROM ds WHERE ds.dec IS NOT NULL
    UNION ALL
    -- Verification task completions and discrepancy resolutions.
    SELECT c.officer, 'VerificationTask', vt.id,
           jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 0, 'after', 1),
                             jsonb_build_object('field', 'AssertedPresent', 'before', NULL, 'after', vt.present)),
           pg_temp.ts(vt.done, 60 + vt.n)
    FROM vt WHERE vt.done IS NOT NULL
    UNION ALL
    SELECT c.auditor, 'Discrepancy', d."Id",
           jsonb_build_array(jsonb_build_object('field', 'Status', 'before', 0, 'after', 1),
                             jsonb_build_object('field', 'ResolutionType', 'before', NULL, 'after', d."ResolutionType")),
           d."ResolvedAt"
    FROM "Discrepancies" d WHERE d."OrganizationId" = c.org AND d."Status" = 1
    UNION ALL
    -- The staff user's department assignment and the organisation rename.
    SELECT c.admin, 'User', c.staff,
           jsonb_build_array(jsonb_build_object('field', 'DepartmentId', 'before', NULL, 'after', (SELECT id FROM dept WHERE code = 'NHSL'))),
           pg_temp.ts(598, 30)
    UNION ALL
    SELECT c.admin, 'Organization', c.org,
           jsonb_build_array(jsonb_build_object('field', 'Name', 'before', 'CoreGrid Demo Organisation', 'after', 'Ministry of Health - Sri Lanka')),
           pg_temp.ts(600, 5)
) u;

-- ---------------------------------------------------------------- summary --

SELECT t AS "table", n AS "rows" FROM (
    SELECT 1 o, 'Departments' t, count(*) n FROM "Departments" UNION ALL
    SELECT 2, 'Locations', count(*) FROM "Locations" UNION ALL
    SELECT 3, 'AssetCategories', count(*) FROM "AssetCategories" UNION ALL
    SELECT 4, 'AssetTypes', count(*) FROM "AssetTypes" UNION ALL
    SELECT 5, 'AssetAttributeDefinitions', count(*) FROM "AssetAttributeDefinitions" UNION ALL
    SELECT 6, 'OrganizationPolicies', count(*) FROM "OrganizationPolicies" UNION ALL
    SELECT 7, 'Assets', count(*) FROM "Assets" UNION ALL
    SELECT 8, 'AssetAttributeValues', count(*) FROM "AssetAttributeValues" UNION ALL
    SELECT 9, 'MaintenanceRecords', count(*) FROM "MaintenanceRecords" UNION ALL
    SELECT 10, 'AssetTransfers', count(*) FROM "AssetTransfers" UNION ALL
    SELECT 11, 'DisposalRequests', count(*) FROM "DisposalRequests" UNION ALL
    SELECT 12, 'VerificationCampaigns', count(*) FROM "VerificationCampaigns" UNION ALL
    SELECT 13, 'VerificationTasks', count(*) FROM "VerificationTasks" UNION ALL
    SELECT 14, 'Discrepancies', count(*) FROM "Discrepancies" UNION ALL
    SELECT 15, 'AssetHistory', count(*) FROM "AssetHistory" UNION ALL
    SELECT 16, 'Notifications', count(*) FROM "Notifications" UNION ALL
    SELECT 17, 'AuditLogEntries', count(*) FROM "AuditLogEntries" UNION ALL
    SELECT 18, 'AgentWorkflows (left empty)', count(*) FROM "AgentWorkflows"
) s ORDER BY o;

\if :{?commit}
COMMIT;
\echo 'Demo data committed.'
\else
ROLLBACK;
\echo 'Dry run only: everything was rolled back. Re-run with -v commit=1 to apply.'
\endif
