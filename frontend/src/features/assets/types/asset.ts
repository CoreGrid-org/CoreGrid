// Wire types for backend/Features/Assets/DTOs — JSON is snake_case
// (Program.cs sets JsonNamingPolicy.SnakeCaseLower) and enums are strings.

export type AssetStatus =
  | "ACTIVE"
  | "UNDER_MAINTENANCE"
  | "TRANSFER_REQUESTED"
  | "IN_TRANSIT"
  | "CONDEMNED"
  | "DISPOSAL_REQUESTED"
  | "DISPOSED";

export type AssetCondition = "NEW" | "GOOD" | "FAIR" | "POOR" | "UNSERVICEABLE";

export type AssetAttributeDataType = "TEXT" | "NUMBER" | "DATE" | "BOOLEAN" | "SELECT";

// GET /api/assets — list item, no residual_value/attributes (only on AssetDetail).
export interface Asset {
  id: string;
  asset_code: string;
  name: string;
  asset_type_id: string;
  asset_type_name: string;
  department_id: string;
  department_name: string;
  location_id: string;
  location_name: string;
  status: AssetStatus;
  condition: AssetCondition;
  acquisition_date: string;
  acquisition_cost: number;
  qr_payload: string;
}

export interface AssetAttributeValue {
  attribute_definition_id: string;
  name: string;
  data_type: AssetAttributeDataType;
  is_required: boolean;
  value_text: string | null;
  value_number: number | null;
  value_date: string | null;
  value_boolean: boolean | null;
}

// GET /api/assets/{id}, GET /api/assets/qr/{code}
export interface AssetDetail extends Asset {
  residual_value: number;
  attributes: AssetAttributeValue[];
}

export interface AssetCategory {
  id: string;
  code: string;
  name: string;
  type_count: number;
  asset_count: number;
  is_active: boolean;
}

export interface AssetType {
  id: string;
  code: string;
  name: string;
  asset_category_id: string;
  category_name: string;
  category_code: string;
  useful_life_years: number;
  default_maintenance_interval_days: number | null;
  attribute_count: number;
  is_active: boolean;
}

export interface AssetAttributeDefinition {
  id: string;
  asset_type_id: string;
  name: string;
  data_type: AssetAttributeDataType;
  is_required: boolean;
  validation_rule: string | null;
  select_options: string[] | null;
  display_order: number;
  is_active: boolean;
}

export interface Department {
  id: string;
  code: string;
  name: string;
  is_active: boolean;
}

export interface Location {
  id: string;
  name: string;
  type: string;
  department_id: string;
  department_name: string;
  is_active: boolean;
}

// GET /api/assets/{id}/history — one row per FR-027 lifecycle event
// (registration, field amendment, condition change, verification correction, ...).
export interface AssetHistoryEntry {
  id: string;
  asset_id: string;
  event_type: string;
  description: string;
  previous_value: string | null;
  new_value: string | null;
  actor_user_id: string | null;
  actor_email: string | null;
  created_at: string;
}

export interface AssetHistoryQueryParameters {
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  total_count: number;
  page: number;
  page_size: number;
  total_pages: number;
}

export interface AssetQueryParameters {
  search?: string;
  categoryId?: string;
  assetTypeId?: string;
  departmentId?: string;
  locationId?: string;
  status?: string;
  condition?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
  page?: number;
  pageSize?: number;
}

export interface UpdateAssetConditionRequest {
  condition: AssetCondition;
}

export interface AssetAttributeValueRequest {
  asset_attribute_definition_id: string;
  value_text: string | null;
  value_number: number | null;
  value_date: string | null;
  value_boolean: boolean | null;
}

export interface CreateAssetRequest {
  asset_type_id: string;
  department_id: string;
  location_id: string;
  name: string;
  acquisition_date: string;
  acquisition_cost: number;
  residual_value: number;
  condition: AssetCondition;
  attributes: AssetAttributeValueRequest[];
}

export interface UpdateAssetRequest {
  asset_type_id: string;
  department_id: string;
  location_id: string;
  name: string;
  acquisition_date: string;
  acquisition_cost: number;
  residual_value: number;
  attributes: AssetAttributeValueRequest[];
}

export interface OrganizationCode {
  code: string;
}

export interface CreateAssetCategoryRequest {
  code: string;
  name: string;
}

export interface UpdateAssetCategoryRequest {
  code: string;
  name: string;
}

export interface CreateAssetTypeRequest {
  code: string;
  name: string;
  asset_category_id: string;
  useful_life_years: number;
  default_maintenance_interval_days: number | null;
}

export interface UpdateAssetTypeRequest {
  code: string;
  name: string;
  asset_category_id: string;
  useful_life_years: number;
  default_maintenance_interval_days: number | null;
}

export interface CreateAssetAttributeDefinitionRequest {
  name: string;
  data_type: AssetAttributeDataType;
  is_required: boolean;
  validation_rule: string | null;
  select_options: string[] | null;
  display_order: number | null;
}

export interface UpdateAssetAttributeDefinitionRequest {
  name: string;
  data_type: AssetAttributeDataType;
  is_required: boolean;
  validation_rule: string | null;
  select_options: string[] | null;
  display_order: number;
}

export const ASSET_STATUSES: AssetStatus[] = [
  "ACTIVE",
  "UNDER_MAINTENANCE",
  "TRANSFER_REQUESTED",
  "IN_TRANSIT",
  "CONDEMNED",
  "DISPOSAL_REQUESTED",
  "DISPOSED",
];

export const ASSET_CONDITIONS: AssetCondition[] = ["NEW", "GOOD", "FAIR", "POOR", "UNSERVICEABLE"];

export const ASSET_ATTRIBUTE_DATA_TYPES: AssetAttributeDataType[] = ["TEXT", "NUMBER", "DATE", "BOOLEAN", "SELECT"];
