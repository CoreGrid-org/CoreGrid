import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import {
  Header,
  HeaderName,
  HeaderGlobalBar,
  HeaderGlobalAction,
  SideNav,
  SideNavItems,
  SideNavLink,
  SideNavMenu,
  SideNavMenuItem,
} from "@carbon/react";
import {
  Notification,
  Settings as SettingsIcon,
  Logout,
  UserAvatar,
  Dashboard as DashboardIcon,
  Asset,
  ToolBox,
  ArrowsHorizontal,
  Search,
  Bot,
  UserMultiple,
  Report,
} from "@carbon/icons-react";
import { SignOutButton } from "@thunderid/react";

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
  { to: "/admin/assets", label: "Register" },
  { to: "/admin/assets/scan", label: "Scan QR" },
  { to: "/admin/assets/config", label: "Asset Config" },
];

// Shared chrome for every /admin/* route: a minimal top header (logo, then
// only global actions) plus a persistent side nav for everything else,
// wrapping whichever page is active (real or mock) via <Outlet>.
export default function AdminLayout() {
  const { pathname } = useLocation();
  const navigate = useNavigate();

  return (
    <>
      <Header aria-label="CoreGrid">
        <HeaderName as={Link} to="/admin">
          CoreGrid
        </HeaderName>
        <HeaderGlobalBar>
          <HeaderGlobalAction aria-label="Notifications">
            <Notification size={20} />
          </HeaderGlobalAction>
          <HeaderGlobalAction aria-label="Settings" onClick={() => navigate("/admin/settings")}>
            <SettingsIcon size={20} />
          </HeaderGlobalAction>
          <SignOutButton>
            {({ signOut }) => (
              <HeaderGlobalAction aria-label="Sign out" onClick={() => signOut()}>
                <Logout size={20} />
              </HeaderGlobalAction>
            )}
          </SignOutButton>
          <HeaderGlobalAction aria-label="User profile">
            <UserAvatar size={20} />
          </HeaderGlobalAction>
        </HeaderGlobalBar>
      </Header>

      <SideNav aria-label="Admin navigation" expanded isFixedNav className="cg-side-nav">
        <SideNavItems>
          <SideNavLink as={Link} to="/admin" renderIcon={DashboardIcon} isActive={pathname === "/admin"}>
            Dashboard
          </SideNavLink>

          <SideNavMenu title="Assets" renderIcon={Asset} defaultExpanded={pathname.startsWith("/admin/assets")}>
            {ASSETS_SUB_ITEMS.map((item) => (
              <SideNavMenuItem key={item.to} as={Link} to={item.to} isActive={pathname === item.to}>
                {item.label}
              </SideNavMenuItem>
            ))}
          </SideNavMenu>

          {NAV_ITEMS.map((item) => {
            const isActive = pathname.startsWith(item.to);
            return (
              <SideNavLink key={item.to} as={Link} to={item.to} renderIcon={item.icon} isActive={isActive}>
                {item.label}
              </SideNavLink>
            );
          })}
        </SideNavItems>
      </SideNav>

      <div className="cg-topnav-content cg-topnav-content--with-sidenav">
        <Outlet />
      </div>
    </>
  );
}
