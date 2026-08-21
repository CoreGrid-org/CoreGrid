import { Asset, ToolBox } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";

// Staff has no real Component D destination at all per the SRS permission
// matrix (no audit/config/user/report access) — both items here are
// Components A/B, mocked for now, same reasoning as InventoryLayout.
const NAV_ITEMS = [
  { to: "/staff/assets", label: "My Assets", icon: Asset },
  { to: "/staff/maintenance", label: "Maintenance", icon: ToolBox },
];

export default function StaffLayout() {
  return <RoleLayout ariaLabel="Staff navigation" homeTo="/staff" navItems={NAV_ITEMS} />;
}
