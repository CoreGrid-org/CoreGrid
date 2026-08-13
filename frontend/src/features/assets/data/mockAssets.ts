// All mock — Component A (Asset Registry & QR Identification) has no backend
// yet (PROGRESS.md). Shapes follow doc/SRS/08-data-requirements.md §8.2 and
// the physical design in doc/SRS/system.md (Appendix F).

export interface MockAsset {
  code: string;
  name: string;
  type: string;
  category: string;
  department: string;
  location: string;
  status: string;
  condition: string;
  acquisitionCost: number;
}

// FR-021 to FR-032 — the asset register.
export const MOCK_ASSETS: MockAsset[] = [
  { code: "ORG-LAP-0142", name: "Dell Latitude 5440", type: "Laptop", category: "IT & Equipment", department: "IT & Equipment", location: "HQ — IT Store", status: "ACTIVE", condition: "GOOD", acquisitionCost: 1450 },
  { code: "ORG-LAP-0143", name: "Dell Latitude 5440", type: "Laptop", category: "IT & Equipment", department: "Finance", location: "HQ — 3rd Floor", status: "UNDER_MAINTENANCE", condition: "FAIR", acquisitionCost: 1450 },
  { code: "ORG-NSW-0021", name: "Cisco Catalyst 9200", type: "Network Switch", category: "IT & Equipment", department: "IT & Equipment", location: "HQ — Server Room", status: "ACTIVE", condition: "NEW", acquisitionCost: 3200 },
  { code: "ORG-VAN-0008", name: "Toyota HiAce", type: "Delivery Van", category: "Vehicles & Fleet", department: "Fleet Operations", location: "Depot A — Bay 3", status: "ACTIVE", condition: "GOOD", acquisitionCost: 42000 },
  { code: "ORG-VAN-0011", name: "Toyota HiAce", type: "Delivery Van", category: "Vehicles & Fleet", department: "Fleet Operations", location: "Depot A — Bay 1", status: "TRANSFER_REQUESTED", condition: "FAIR", acquisitionCost: 42000 },
  { code: "ORG-VAN-0014", name: "Isuzu NPR", type: "Delivery Van", category: "Vehicles & Fleet", department: "Fleet Operations", location: "Depot B", status: "CONDEMNED", condition: "POOR", acquisitionCost: 38500 },
  { code: "ORG-PMN-0033", name: "Philips IntelliVue MX450", type: "Patient Monitor", category: "Medical Equipment", department: "Ward Services", location: "Ward 4B", status: "ACTIVE", condition: "GOOD", acquisitionCost: 8900 },
  { code: "ORG-PMN-0034", name: "Philips IntelliVue MX450", type: "Patient Monitor", category: "Medical Equipment", department: "Ward Services", location: "Ward 2A", status: "DISPOSAL_REQUESTED", condition: "UNSERVICEABLE", acquisitionCost: 8900 },
  { code: "ORG-HBD-0061", name: "Hill-Rom Advanta 2", type: "Hospital Bed", category: "Medical Equipment", department: "Ward Services", location: "Ward 4B", status: "ACTIVE", condition: "GOOD", acquisitionCost: 5200 },
  { code: "ORG-CHR-0210", name: "Herman Miller Aeron", type: "Office Chair", category: "Facilities & Furniture", department: "Finance", location: "HQ — 3rd Floor", status: "ACTIVE", condition: "GOOD", acquisitionCost: 620 },
  { code: "ORG-CHR-0211", name: "Herman Miller Aeron", type: "Office Chair", category: "Facilities & Furniture", department: "Human Resources", location: "HQ — 2nd Floor", status: "ACTIVE", condition: "FAIR", acquisitionCost: 620 },
  { code: "ORG-VAN-0019", name: "Toyota HiAce", type: "Delivery Van", category: "Vehicles & Fleet", department: "Fleet Operations", location: "Depot A — Bay 2", status: "DISPOSED", condition: "UNSERVICEABLE", acquisitionCost: 41000 },
];

export interface MockAssetCategory {
  code: string;
  name: string;
  typeCount: number;
  assetCount: number;
}

// FR-016 — top-level grouping for reporting.
export const MOCK_ASSET_CATEGORIES: MockAssetCategory[] = [
  { code: "IT", name: "IT & Equipment", typeCount: 2, assetCount: 3 },
  { code: "VEH", name: "Vehicles & Fleet", typeCount: 1, assetCount: 4 },
  { code: "MED", name: "Medical Equipment", typeCount: 2, assetCount: 3 },
  { code: "FAC", name: "Facilities & Furniture", typeCount: 1, assetCount: 2 },
];

export interface MockAttributeDefinition {
  name: string;
  dataType: "TEXT" | "NUMBER" | "DATE" | "BOOLEAN" | "SELECT";
  required: boolean;
  notes?: string;
}

export interface MockAssetType {
  code: string;
  name: string;
  category: string;
  usefulLifeYears: number;
  maintenanceIntervalDays: number | null;
  attributes: MockAttributeDefinition[];
}

// FR-017, FR-018 — this is the platform's central claim: the same schema
// (AssetTypes + AssetAttributeDefinitions, §8.2) serves completely different
// organisations. A transport company, a hospital and a facilities team each
// configure their own asset types here with no code change (FR-020).
export const MOCK_ASSET_TYPES: MockAssetType[] = [
  {
    code: "LAP",
    name: "Laptop",
    category: "IT & Equipment",
    usefulLifeYears: 4,
    maintenanceIntervalDays: 180,
    attributes: [
      { name: "Serial Number", dataType: "TEXT", required: true },
      { name: "Manufacturer", dataType: "TEXT", required: true },
      { name: "CPU", dataType: "TEXT", required: false },
      { name: "RAM (GB)", dataType: "NUMBER", required: false },
      { name: "Warranty Expiry", dataType: "DATE", required: true },
      { name: "Operating System", dataType: "SELECT", required: true, notes: "Windows · macOS · Linux" },
    ],
  },
  {
    code: "NSW",
    name: "Network Switch",
    category: "IT & Equipment",
    usefulLifeYears: 6,
    maintenanceIntervalDays: null,
    attributes: [
      { name: "Serial Number", dataType: "TEXT", required: true },
      { name: "Port Count", dataType: "NUMBER", required: true },
      { name: "Firmware Version", dataType: "TEXT", required: false },
      { name: "Rack Location", dataType: "TEXT", required: false },
    ],
  },
  {
    code: "VAN",
    name: "Delivery Van",
    category: "Vehicles & Fleet",
    usefulLifeYears: 8,
    maintenanceIntervalDays: 90,
    attributes: [
      { name: "Registration Number", dataType: "TEXT", required: true },
      { name: "Engine Capacity (cc)", dataType: "NUMBER", required: false },
      { name: "Fuel Type", dataType: "SELECT", required: true, notes: "Petrol · Diesel · Electric · Hybrid" },
      { name: "Odometer Reading (km)", dataType: "NUMBER", required: false },
      { name: "Insurance Expiry", dataType: "DATE", required: true },
    ],
  },
  {
    code: "PMN",
    name: "Patient Monitor",
    category: "Medical Equipment",
    usefulLifeYears: 7,
    maintenanceIntervalDays: 90,
    attributes: [
      { name: "Serial Number", dataType: "TEXT", required: true },
      { name: "Model Number", dataType: "TEXT", required: true },
      { name: "Calibration Due Date", dataType: "DATE", required: true },
      { name: "Manufacturer", dataType: "TEXT", required: false },
    ],
  },
  {
    code: "HBD",
    name: "Hospital Bed",
    category: "Medical Equipment",
    usefulLifeYears: 10,
    maintenanceIntervalDays: 180,
    attributes: [
      { name: "Bed Type", dataType: "SELECT", required: true, notes: "Manual · Semi-Electric · Full-Electric" },
      { name: "Max Load Capacity (kg)", dataType: "NUMBER", required: false },
      { name: "Ward Assignment", dataType: "TEXT", required: false },
    ],
  },
  {
    code: "CHR",
    name: "Office Chair",
    category: "Facilities & Furniture",
    usefulLifeYears: 6,
    maintenanceIntervalDays: null,
    attributes: [
      { name: "Material", dataType: "TEXT", required: false },
      { name: "Adjustable Height", dataType: "BOOLEAN", required: false },
      { name: "Purchase Batch", dataType: "TEXT", required: false },
    ],
  },
];
