import { Link, Outlet, useLocation } from "react-router-dom";
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
  Catalog,
  QrCode,
  SettingsAdjust,
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
  { to: "/admin/assets", label: "Register", icon: Catalog },
  { to: "/admin/assets/scan", label: "Scan QR", icon: QrCode },
  { to: "/admin/assets/config", label: "Asset Config", icon: SettingsAdjust },
];

// Shared chrome for every /admin/* route: a minimal top header (logo, then
// only global actions) plus a persistent side nav for everything else,
// wrapping whichever page is active (real or mock) via <Outlet>.
export default function AdminLayout() {
  const { pathname } = useLocation();

  return (
    <>
      <Header aria-label="CoreGrid">
        <HeaderName as={Link} to="/admin" prefix="" className="cg-header-brand">
          <span className="cg-header-brand__inner">
            <span className="cg-header-brand__logo">
              <img src="/assets/w-coregrid.webp" alt="" width={28} height={28} />
            </span>
            CoreGrid
          </span>
        </HeaderName>
        <HeaderGlobalBar>
          <HeaderGlobalAction aria-label="Notifications">
            <Notification size={20} className="cg-header-icon" />
          </HeaderGlobalAction>
          <SignOutButton>
            {({ signOut }) => (
              <HeaderGlobalAction aria-label="Sign out" onClick={() => signOut()}>
                <Logout size={20} className="cg-header-icon" />
              </HeaderGlobalAction>
            )}
          </SignOutButton>
          <HeaderGlobalAction aria-label="User profile">
            <UserAvatar size={20} className="cg-header-icon" />
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
                <span className="cg-side-nav__submenu-item">
                  <item.icon size={16} />
                  {item.label}
                </span>
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
