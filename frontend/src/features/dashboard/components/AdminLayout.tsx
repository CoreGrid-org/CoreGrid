import {
  ToolBox,
  ArrowsHorizontal,
  Search,
  Bot,
  UserMultiple,
  Report,
  Settings as SettingsIcon,
  Catalog,
  QrCode,
  SettingsAdjust,
} from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

const ASSETS_SUB_ITEMS = [
  { to: "/admin/assets", label: "Register", icon: Catalog },
  { to: "/admin/assets/scan", label: "Scan QR", icon: QrCode },
  { to: "/admin/assets/config", label: "Asset Config", icon: SettingsAdjust },
];

const OPERATIONS_SUB_ITEMS = [
  { to: "/admin/maintenance", label: "Maintenance", icon: ToolBox },
  { to: "/admin/transfers", label: "Transfers & Disposals", icon: ArrowsHorizontal },
];

const COMPLIANCE_SUB_ITEMS = [
  { to: "/admin/audit", label: "Audit & Compliance", icon: Search },
  { to: "/admin/workflows", label: "Workflows", icon: Bot },
  { to: "/admin/reports", label: "Reports", icon: Report },
];

const ADMINISTRATION_SUB_ITEMS = [
  { to: "/admin/users", label: "Users & Roles", icon: UserMultiple },
  { to: "/admin/settings", label: "Settings", icon: SettingsIcon },
];

export default function AdminLayout() {
  return (
    <RoleLayout
      ariaLabel="Admin navigation"
      homeTo="/admin"
      navGroups={[
        { label: "Assets", items: ASSETS_SUB_ITEMS },
        { label: "Operations", items: OPERATIONS_SUB_ITEMS },
        { label: "Compliance", items: COMPLIANCE_SUB_ITEMS },
        { label: "Administration", items: ADMINISTRATION_SUB_ITEMS },
      ]}
    />
  );
}
