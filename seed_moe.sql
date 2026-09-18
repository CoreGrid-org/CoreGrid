DO $$
DECLARE
  org_id uuid := '9d7ca11b-3578-48e4-9d18-c4530d3819d7';
  user_id uuid := '3a6f9a03-b44e-4c5a-8514-68867eabbd6b';
  
  -- Departments
  dept_it uuid := gen_random_uuid();
  dept_sci uuid := gen_random_uuid();
  dept_admin uuid := gen_random_uuid();
  dept_sports uuid := gen_random_uuid();
  
  -- Locations
  loc_hq uuid := gen_random_uuid();
  loc_kandy uuid := gen_random_uuid();
  loc_royal uuid := gen_random_uuid();
  loc_ananda uuid := gen_random_uuid();

  -- Categories
  cat_it uuid := gen_random_uuid();
  cat_furn uuid := gen_random_uuid();
  cat_lab uuid := gen_random_uuid();
  cat_sports uuid := gen_random_uuid();

  -- Asset Types
  typ_laptop uuid := gen_random_uuid();
  typ_proj uuid := gen_random_uuid();
  typ_desk uuid := gen_random_uuid();
  typ_micro uuid := gen_random_uuid();
  typ_bat uuid := gen_random_uuid();

BEGIN
  -- Insert Departments
  INSERT INTO "Departments" ("Id", "OrganizationId", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") VALUES
  (dept_it, org_id, 'D-IT', 'Information Technology', true, NOW(), NOW(), user_id, user_id),
  (dept_sci, org_id, 'D-SCI', 'Science & Research', true, NOW(), NOW(), user_id, user_id),
  (dept_admin, org_id, 'D-ADM', 'Administration', true, NOW(), NOW(), user_id, user_id),
  (dept_sports, org_id, 'D-SPT', 'Sports & Physical Education', true, NOW(), NOW(), user_id, user_id);

  -- Insert Locations
  INSERT INTO "Locations" ("Id", "OrganizationId", "DepartmentId", "Name", "Type", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") VALUES
  (loc_hq, org_id, dept_admin, 'Ministry Headquarters', 'Headquarters', true, NOW(), NOW(), user_id, user_id),
  (loc_kandy, org_id, dept_admin, 'Kandy Regional Office', 'RegionalOffice', true, NOW(), NOW(), user_id, user_id),
  (loc_royal, org_id, dept_sci, 'Royal College', 'School', true, NOW(), NOW(), user_id, user_id),
  (loc_ananda, org_id, dept_sports, 'Ananda College', 'School', true, NOW(), NOW(), user_id, user_id);

  -- Insert Asset Categories
  INSERT INTO "AssetCategories" ("Id", "OrganizationId", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") VALUES
  (cat_it, org_id, 'C-IT', 'IT Equipment', true, NOW(), NOW(), user_id, user_id),
  (cat_furn, org_id, 'C-FURN', 'Furniture', true, NOW(), NOW(), user_id, user_id),
  (cat_lab, org_id, 'C-LAB', 'Laboratory Equipment', true, NOW(), NOW(), user_id, user_id),
  (cat_sports, org_id, 'C-SPT', 'Sports Gear', true, NOW(), NOW(), user_id, user_id);

  -- Insert Asset Types
  INSERT INTO "AssetTypes" ("Id", "OrganizationId", "AssetCategoryId", "Code", "Name", "UsefulLifeYears", "DefaultMaintenanceIntervalDays", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") VALUES
  (typ_laptop, org_id, cat_it, 'T-LAPTOP', 'Laptop Computer', 5, 365, true, NOW(), NOW(), user_id, user_id),
  (typ_proj, org_id, cat_it, 'T-PROJ', 'Digital Projector', 4, 180, true, NOW(), NOW(), user_id, user_id),
  (typ_desk, org_id, cat_furn, 'T-DESK', 'Student Desk', 10, NULL, true, NOW(), NOW(), user_id, user_id),
  (typ_micro, org_id, cat_lab, 'T-MICRO', 'Binocular Microscope', 8, 365, true, NOW(), NOW(), user_id, user_id),
  (typ_bat, org_id, cat_sports, 'T-BAT', 'Cricket Bat', 3, NULL, true, NOW(), NOW(), user_id, user_id);

  -- Insert Assets
  INSERT INTO "Assets" ("Id", "OrganizationId", "AssetTypeId", "DepartmentId", "LocationId", "AssetCode", "Name", "Status", "Condition", "AcquisitionDate", "AcquisitionCost", "ResidualValue", "CumulativeMaintenanceCost", "RepairCount", "QrPayload", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") VALUES
  -- IT Assets at HQ
  (gen_random_uuid(), org_id, typ_laptop, dept_it, loc_hq, 'IT-LPT-001', 'Admin Laptop 1', 'ACTIVE', 'NEW', '2023-01-15', 150000.00, 10000.00, 0, 0, 'qr-payload-it-lpt-001', NOW(), NOW(), user_id, user_id),
  (gen_random_uuid(), org_id, typ_laptop, dept_it, loc_hq, 'IT-LPT-002', 'Admin Laptop 2', 'ACTIVE', 'GOOD', '2023-01-15', 150000.00, 10000.00, 5000, 1, 'qr-payload-it-lpt-002', NOW(), NOW(), user_id, user_id),
  
  -- Projectors at Royal College
  (gen_random_uuid(), org_id, typ_proj, dept_sci, loc_royal, 'IT-PRJ-001', 'Smart Classroom Projector A', 'ACTIVE', 'GOOD', '2022-05-10', 85000.00, 5000.00, 12000, 2, 'qr-payload-it-prj-001', NOW(), NOW(), user_id, user_id),
  (gen_random_uuid(), org_id, typ_proj, dept_sci, loc_royal, 'IT-PRJ-002', 'Smart Classroom Projector B', 'UNDER_MAINTENANCE', 'FAIR', '2021-11-20', 80000.00, 5000.00, 15000, 3, 'qr-payload-it-prj-002', NOW(), NOW(), user_id, user_id),

  -- Desks at Ananda College
  (gen_random_uuid(), org_id, typ_desk, dept_admin, loc_ananda, 'FRN-DSK-001', 'Classroom Desk - Grade 10', 'ACTIVE', 'GOOD', '2020-02-05', 12000.00, 500.00, 0, 0, 'qr-payload-frn-dsk-001', NOW(), NOW(), user_id, user_id),
  (gen_random_uuid(), org_id, typ_desk, dept_admin, loc_ananda, 'FRN-DSK-002', 'Classroom Desk - Grade 10', 'ACTIVE', 'FAIR', '2020-02-05', 12000.00, 500.00, 0, 0, 'qr-payload-frn-dsk-002', NOW(), NOW(), user_id, user_id),
  
  -- Microscopes at Royal College
  (gen_random_uuid(), org_id, typ_micro, dept_sci, loc_royal, 'LAB-MIC-001', 'Biology Lab Microscope 1', 'ACTIVE', 'GOOD', '2019-08-15', 45000.00, 2000.00, 3000, 1, 'qr-payload-lab-mic-001', NOW(), NOW(), user_id, user_id),
  (gen_random_uuid(), org_id, typ_micro, dept_sci, loc_royal, 'LAB-MIC-002', 'Biology Lab Microscope 2', 'ACTIVE', 'NEW', '2024-01-10', 55000.00, 5000.00, 0, 0, 'qr-payload-lab-mic-002', NOW(), NOW(), user_id, user_id),

  -- Cricket Bats at Kandy Regional Office (for distribution)
  (gen_random_uuid(), org_id, typ_bat, dept_sports, loc_kandy, 'SPT-BAT-001', 'Tournament Cricket Bat', 'IN_TRANSIT', 'NEW', '2024-03-01', 25000.00, 0.00, 0, 0, 'qr-payload-spt-bat-001', NOW(), NOW(), user_id, user_id);
END $$;
