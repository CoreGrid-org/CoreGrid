import { Asset, ToolBox, ArrowsHorizontal, Bot, Search, Report } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

// Audit & Compliance and Reports are Component D, real. Assets/Maintenance/
// Transfers/Workflows are Components A/B/C — mocked for now, same reasoning
// as InventoryLayout.
const NAV_ITEMS = [
  { to: "/audit/assets", label: "Asset Registry", icon: Asset },
  { to: "/audit/maintenance", label: "Maintenance", icon: ToolBox },
  { to: "/audit/transfers", label: "Transfers & Disposals", icon: ArrowsHorizontal },
  { to: "/audit/workflows", label: "Workflows", icon: Bot },
  { to: "/audit/audit", label: "Audit & Compliance", icon: Search },
  { to: "/audit/reports", label: "Reports", icon: Report },
];

export default function AuditLayout() {
  return <RoleLayout ariaLabel="Auditor navigation" homeTo="/audit" navItems={NAV_ITEMS} />;
}
