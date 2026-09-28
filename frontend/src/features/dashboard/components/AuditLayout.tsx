import { Asset, QrCode, ToolBox, ArrowsHorizontal, Bot, Search, Report } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

// Every destination below is wired to its real page (App.tsx). Auditor is
// read-only on Assets/Maintenance/Transfers & Disposals (the backend's own
// policies never grant Auditor a write action on any of them). Grouped to
// match AdminLayout's sections.
const ASSETS_SUB_ITEMS = [
  { to: "/audit/assets", label: "Asset Registry", icon: Asset },
  { to: "/audit/assets/scan", label: "Scan QR", icon: QrCode },
];

const OPERATIONS_SUB_ITEMS = [
  { to: "/audit/maintenance", label: "Maintenance", icon: ToolBox },
  { to: "/audit/transfers", label: "Transfers & Disposals", icon: ArrowsHorizontal },
];

const COMPLIANCE_SUB_ITEMS = [
  { to: "/audit/audit", label: "Audit & Compliance", icon: Search },
  { to: "/audit/workflows", label: "Workflows", icon: Bot },
  { to: "/audit/reports", label: "Reports", icon: Report },
];

export default function AuditLayout() {
  return (
    <RoleLayout
      ariaLabel="Auditor navigation"
      homeTo="/audit"
      navGroups={[
        { label: "Assets", items: ASSETS_SUB_ITEMS },
        { label: "Operations", items: OPERATIONS_SUB_ITEMS },
        { label: "Compliance", items: COMPLIANCE_SUB_ITEMS },
      ]}
    />
  );
}
