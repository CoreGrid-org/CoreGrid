import { Asset, QrCode, ToolBox, ArrowsHorizontal, Bot, Report } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

// Every destination below is wired to its real page (App.tsx). Officer
// reads organisation-wide (SRS §4.6); what they can act on is decided by
// features/auth/lib/permissions.ts.
const NAV_ITEMS = [
  { to: "/inventory/assets", label: "Asset Registry", icon: Asset },
  { to: "/inventory/assets/scan", label: "Scan QR", icon: QrCode },
  { to: "/inventory/maintenance", label: "Maintenance", icon: ToolBox },
  { to: "/inventory/transfers", label: "Transfers & Disposals", icon: ArrowsHorizontal },
  { to: "/inventory/workflows", label: "Workflows", icon: Bot },
  { to: "/inventory/reports", label: "Reports", icon: Report },
];

export default function InventoryLayout() {
  return <RoleLayout ariaLabel="Inventory Officer navigation" homeTo="/inventory" navItems={NAV_ITEMS} />;
}
