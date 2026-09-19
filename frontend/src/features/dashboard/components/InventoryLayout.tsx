import { Asset, ToolBox, ArrowsHorizontal, Bot, Report } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

// Every destination below is wired to its real page (App.tsx) — Assets,
// Maintenance and Transfers & Disposals are Staff-department-scoped
// server-side (B14/DepartmentScope) the same way Officer's own list
// queries already are.
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
