import { Asset, QrCode, ToolBox, ArrowsHorizontal, Bot, Report } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

// Every destination below is wired to its real page (App.tsx). Officer
// reads organisation-wide (SRS §4.6); what they can act on is decided by
// features/auth/lib/permissions.ts. Grouped to match AdminLayout's sections.
const ASSETS_SUB_ITEMS = [
  { to: "/inventory/assets", label: "Asset Registry", icon: Asset },
  { to: "/inventory/assets/scan", label: "Scan QR", icon: QrCode },
];

const OPERATIONS_SUB_ITEMS = [
  { to: "/inventory/maintenance", label: "Maintenance", icon: ToolBox },
  { to: "/inventory/transfers", label: "Transfers & Disposals", icon: ArrowsHorizontal },
];

const COMPLIANCE_SUB_ITEMS = [
  { to: "/inventory/workflows", label: "Workflows", icon: Bot },
  { to: "/inventory/reports", label: "Reports", icon: Report },
];

export default function InventoryLayout() {
  return (
    <RoleLayout
      ariaLabel="Inventory Officer navigation"
      homeTo="/inventory"
      navGroups={[
        { label: "Assets", items: ASSETS_SUB_ITEMS },
        { label: "Operations", items: OPERATIONS_SUB_ITEMS },
        { label: "Compliance", items: COMPLIANCE_SUB_ITEMS },
      ]}
    />
  );
}
