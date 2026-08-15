import { Asset, ToolBox, ArrowsHorizontal, Bot, Report } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

// Component D (Reports, FR-084/085) is the only nav destination Officer has
// wired to a real page today — Assets/Maintenance/Transfers/Workflows are
// Components A/B/C, which this pass deliberately leaves mocked (see
// App.tsx) rather than exposing untested cross-component pages to a role
// that hasn't been through department-scoping/permission review on them yet.
const NAV_ITEMS = [
  { to: "/inventory/assets", label: "Asset Registry", icon: Asset },
  { to: "/inventory/maintenance", label: "Maintenance", icon: ToolBox },
  { to: "/inventory/transfers", label: "Transfers & Disposals", icon: ArrowsHorizontal },
  { to: "/inventory/workflows", label: "Workflows", icon: Bot },
  { to: "/inventory/reports", label: "Reports", icon: Report },
];

export default function InventoryLayout() {
  return <RoleLayout ariaLabel="Inventory Officer navigation" homeTo="/inventory" navItems={NAV_ITEMS} />;
}
