export type MaintenanceType = "CORRECTIVE" | "PREVENTIVE";

export type MaintenancePriority = "LOW" | "MEDIUM" | "HIGH" | "CRITICAL";

export type MaintenanceStatus =
  | "REQUESTED"
  | "APPROVED"
  | "IN_PROGRESS"
  | "COMPLETED"
  | "CANCELLED";

export interface MaintenanceRecord {
  id: string;
  asset_id: string;
  asset_code: string;
  asset_name: string;
  asset_type_name: string;
  description: string;
  observed_condition: string;
  photo_url?: string;
  type: MaintenanceType;
  priority: MaintenancePriority;
  status: MaintenanceStatus;
  estimated_cost?: number;
  actual_cost?: number;
  work_performed?: string;
  completion_date?: string;
  resulting_condition?: string;
  assignee_id?: string;
  assignee_email?: string;
  cancellation_reason?: string;
  created_at: string;
}

export interface ReportFaultRequest {
  asset_id: string;
  description: string;
  observed_condition: string;
  photo_url?: string;
}

export interface CreateMaintenanceRequest {
  asset_id: string;
  description: string;
  observed_condition: string;
  photo_url?: string;
  type: MaintenanceType;
  priority: MaintenancePriority;
  estimated_cost?: number;
  assignee_id?: string;
}

export interface ApproveMaintenanceRequest {
  estimated_cost: number;
  assignee_id: string;
}

export interface CompleteMaintenanceRequest {
  actual_cost: number;
  work_performed: string;
  completion_date: string;
  resulting_condition: string;
  overspend_justification?: string;
}

export interface CancelMaintenanceRequest {
  reason?: string;
}

export interface MaintenanceQueryParameters {
  assetId?: string;
  departmentId?: string;
  assigneeId?: string;
  status?: MaintenanceStatus;
  type?: MaintenanceType;
  priority?: MaintenancePriority;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
  page?: number;
  pageSize?: number;
}

export interface PagedMaintenanceRecords {
  items: MaintenanceRecord[];
  total_count: number;
  page: number;
  page_size: number;
  total_pages: number;
}
