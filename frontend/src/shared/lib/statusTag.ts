import type { TagProps } from "@carbon/react";

type TagColor = NonNullable<TagProps<"span">["type"]>;

// One colour mapping shared by every mock admin page so the same status
// string (Appendix A — Status and Enumeration Reference) always reads the
// same way regardless of which entity it appears on.
const STATUS_COLORS: Record<string, TagColor> = {
  // AssetStatus
  ACTIVE: "green",
  UNDER_MAINTENANCE: "purple",
  TRANSFER_REQUESTED: "blue",
  IN_TRANSIT: "cyan",
  CONDEMNED: "red",
  DISPOSAL_REQUESTED: "magenta",
  DISPOSED: "gray",
  // AssetCondition
  NEW: "teal",
  GOOD: "green",
  FAIR: "blue",
  POOR: "magenta",
  UNSERVICEABLE: "red",
  // MaintenanceStatus
  REQUESTED: "blue",
  APPROVED: "green",
  IN_PROGRESS: "purple",
  COMPLETED: "gray",
  CANCELLED: "red",
  // Priority
  LOW: "gray",
  MEDIUM: "blue",
  HIGH: "magenta",
  CRITICAL: "red",
  // TransferStatus additions
  REJECTED: "red",
  // DisposalStatus
  DRAFT: "gray",
  PENDING_APPROVAL: "blue",
  REVISION_REQUESTED: "magenta",
  // DiscrepancyStatus
  OPEN: "magenta",
  UNDER_REVIEW: "blue",
  RESOLVED: "green",
  // DiscrepancyType
  MISSING: "red",
  SURPLUS: "purple",
  LOCATION_MISMATCH: "blue",
  CONDITION_MISMATCH: "magenta",
  DATA_MISMATCH: "teal",
  OTHER: "gray",
  // WorkflowStatus
  PLANNING: "gray",
  ANALYZING: "blue",
  VALIDATING: "blue",
  AWAITING_APPROVAL: "magenta",
  COMPLETED_ADVISORY: "teal",
  FAILED_SAFE: "red",
  // ApprovalStatus
  NOT_REQUIRED: "gray",
  PENDING: "blue",
};

export function statusTagColor(status: string): TagColor {
  const normalized = status
    .replace(/([a-z])([A-Z])/g, "$1_$2")
    .toUpperCase();
  return STATUS_COLORS[normalized] ?? "gray";
}

export function formatStatusLabel(status: string): string {
  const normalized = status
    .replace(/([a-z])([A-Z])/g, "$1_$2")
    .toUpperCase();
  return normalized
    .toLowerCase()
    .split("_")
    .map((word) => word.length > 0 ? word[0].toUpperCase() + word.slice(1) : "")
    .join(" ");
}
