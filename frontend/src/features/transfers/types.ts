// Wire types for backend/Features/Transfers and backend/Features/Disposals
// JSON serialization uses JsonNamingPolicy.SnakeCaseLower (snake_case)
// and JsonStringEnumConverter (string enums).

export type TransferStatus =
  | "REQUESTED"
  | "APPROVED"
  | "IN_TRANSIT"
  | "COMPLETED"
  | "REJECTED"
  | "CANCELLED";

export type DisposalStatus =
  | "PENDING"
  | "APPROVED"
  | "REJECTED"
  | "REVISION_REQUESTED"
  | "DISPOSED";

export type DisposalMethod = "SCRAP" | "AUCTION" | "DONATION" | "DESTROY";

// DTOs matching backend/Features/Transfers/DTOs/TransferDtos.cs
export interface InitiateTransferRequest {
  asset_id: string;
  to_department_id: string;
  to_location_id: string;
}

export interface TransferResponse {
  id: string;
  organization_id: string;
  asset_id: string;
  asset_code: string;
  asset_name: string;
  from_department_id: string;
  from_department_name: string | null;
  to_department_id: string;
  to_department_name: string | null;
  from_location_id: string;
  from_location_name: string | null;
  to_location_id: string;
  to_location_name: string | null;
  initiated_by_user_id: string;
  initiated_by_user_email: string | null;
  approved_by_user_id: string | null;
  approved_by_user_email: string | null;
  confirmed_by_user_id: string | null;
  confirmed_by_user_email: string | null;
  status: TransferStatus;
  requested_at: string;
  approved_at: string | null;
  confirmed_at: string | null;
  rejection_reason: string | null;
}

export interface PagedResult<T> {
  items: T[];
  total_count: number;
  page: number;
  page_size: number;
  total_pages: number;
}

export interface TransferQueryParameters {
  status?: TransferStatus;
  departmentId?: string;
  page?: number;
  pageSize?: number;
}

// Precondition evaluation matching CoreGrid.Api.Domain.DisposalPreconditionResult
export interface PreconditionCheck {
  code: string; // "P1".."P6"
  description: string;
  passed: boolean;
  failure_reason: string | null;
}

export interface DisposalPreconditionResult {
  separation_of_duties_passed: boolean;
  separation_of_duties_failure_reason: string | null;
  all_passed: boolean;
  checks: PreconditionCheck[];
}

// DTOs matching backend/Features/Disposals/DTOs/DisposalDtos.cs
export interface CondemnAssetRequest {
  reason?: string;
  evidence_url?: string;
}

export interface CondemnAssetResponse {
  asset_id: string;
  asset_code: string;
  name: string;
  status: string;
  condition: string;
  reason: string | null;
  condemned_at: string;
}

export interface SubmitDisposalRequest {
  asset_id: string;
  disposal_method: DisposalMethod;
  estimated_residual_value: number;
  valuation_date?: string | null;
  notes?: string | null;
}

export interface RequestDisposalRevisionRequest {
  comments: string;
}

export interface DisposalResponse {
  id: string;
  organization_id: string;
  asset_id: string;
  asset_code: string;
  asset_name: string;
  asset_condition: string;
  asset_status: string;
  initiated_by_user_id: string;
  initiated_by_user_email: string | null;
  approved_by_user_id: string | null;
  approved_by_user_email: string | null;
  disposal_method: DisposalMethod;
  estimated_residual_value: number;
  valuation_date: string | null;
  status: DisposalStatus;
  requested_at: string;
  approved_at: string | null;
  disposed_at: string | null;
  notes: string | null;
  precondition_evaluation?: DisposalPreconditionResult | null;
}

export interface DisposalQueryParameters {
  status?: DisposalStatus;
  method?: DisposalMethod;
  page?: number;
  pageSize?: number;
}
