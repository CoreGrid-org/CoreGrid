import {
  ToolBox,
  ArrowsHorizontal,
  Search,
  Bot,
  UserMultiple,
  Report,
  Settings as SettingsIcon,
  Asset,
  Catalog,
  QrCode,
  SettingsAdjust,
} from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

const NAV_ITEMS = [
  { to: "/admin/maintenance", label: "Maintenance", icon: ToolBox },
  { to: "/admin/transfers", label: "Transfers & Disposals", icon: ArrowsHorizontal },
  { to: "/admin/audit", label: "Audit & Compliance", icon: Search },
  { to: "/admin/workflows", label: "Workflows", icon: Bot },
  { to: "/admin/users", label: "Users & Roles", icon: UserMultiple },
  { to: "/admin/reports", label: "Reports", icon: Report },
  { to: "/admin/settings", label: "Settings", icon: SettingsIcon },
];

const ASSETS_SUB_ITEMS = [
  { to: "/admin/assets", label: "Register", icon: Catalog },
  { to: "/admin/assets/scan", label: "Scan QR", icon: QrCode },
  { to: "/admin/assets/config", label: "Asset Config", icon: SettingsAdjust },
];

export default function AdminLayout() {
  return (
    <RoleLayout
      ariaLabel="Admin navigation"
      homeTo="/admin"
      navGroups={[{ label: "Assets", icon: Asset, items: ASSETS_SUB_ITEMS }]}
      navItems={NAV_ITEMS}
    />
  );
}
