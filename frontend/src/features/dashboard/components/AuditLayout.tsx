import { Asset, ToolBox, ArrowsHorizontal, Bot, Search, Report } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

// Every destination below is wired to its real page (App.tsx) — Auditor is
// read-only on Assets/Maintenance/Transfers & Disposals (the backend's own
// policies never grant Auditor a write action on any of them).
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
