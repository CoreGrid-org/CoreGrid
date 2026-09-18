import { Fragment } from "react";
import { Link, Outlet, useLocation } from "react-router-dom";
import type { ComponentType } from "react";
import {
  Header,
  HeaderName,
  HeaderGlobalBar,
  HeaderGlobalAction,
  SideNav,
  SideNavItems,
  SideNavLink,
} from "@carbon/react";
import { Notification, Logout, UserAvatar, Dashboard as DashboardIcon } from "@carbon/icons-react";
import { SignOutButton } from "@thunderid/react";
import { useNotificationCenter } from "@/features/notifications/hooks/useNotifications";
import NotificationPanel from "@/features/notifications/components/NotificationPanel";

export interface RoleNavItem {
  to: string;
  label: string;
  icon: ComponentType<{ size?: number; className?: string }>;
}

// A labelled section of the nav — always fully visible, no expand/collapse.
// Just a small caption above its items, not an interactive accordion.
export interface RoleNavGroup {
  label: string;
  items: RoleNavItem[];
}

interface RoleLayoutProps {
  ariaLabel: string;
  homeTo: string;
  navItems?: RoleNavItem[];
  navGroups?: RoleNavGroup[];
}

// Shared chrome for every per-role dashboard route: a minimal top header
// (logo, then only global actions) plus a persistent side nav for
// everything else, wrapping whichever page is active via <Outlet>. Extracted
// from what was originally AdminLayout-only markup so InventoryLayout and
// AuditLayout render identical chrome with role-specific nav content
// instead of duplicating this file. Staff has no web portal at all (see
// auth/lib/roles.ts), so there's no StaffLayout consumer.
export default function RoleLayout({ ariaLabel, homeTo, navItems = [], navGroups = [] }: RoleLayoutProps) {
  const { pathname } = useLocation();
  const notificationCenter = useNotificationCenter();

  // "Register" (/admin/assets) is itself a prefix of "Scan QR"
  // (/admin/assets/scan) and "Asset Config" (/admin/assets/config), so a
  // plain pathname.startsWith(item.to) check would light up all three at
  // once on those pages. Only the single most specific (longest) matching
  // item should be active.
  const allItems = [...navGroups.flatMap((g) => g.items), ...navItems];
  const isItemActive = (to: string) =>
    pathname.startsWith(to) && !allItems.some((other) => other.to !== to && other.to.startsWith(to) && pathname.startsWith(other.to));

  return (
    <>
      <Header aria-label="CoreGrid">
        <HeaderName as={Link} to={homeTo} prefix="" className="cg-header-brand">
          <span className="cg-header-brand__inner">
            <span className="cg-header-brand__logo">
              <img src="/assets/w-coregrid.webp" alt="" width={28} height={28} />
            </span>
            CoreGrid
          </span>
        </HeaderName>
        <HeaderGlobalBar>
          <div style={{ position: "relative" }}>
            <HeaderGlobalAction
              aria-label={`Notifications${notificationCenter.unreadCount > 0 ? ` (${notificationCenter.unreadCount} unread)` : ""}`}
              onClick={notificationCenter.toggle}
              isActive={notificationCenter.isOpen}
            >
              <Notification size={20} className="cg-header-icon" />
              {notificationCenter.unreadCount > 0 && (
                <span
                  aria-hidden="true"
                  style={{
                    position: "absolute",
                    top: "0.5rem",
                    right: "0.5rem",
                    minWidth: "0.9rem",
                    height: "0.9rem",
                    padding: "0 0.2rem",
                    borderRadius: "0.5rem",
                    background: "#da1e28",
                    color: "#fff",
                    fontSize: "0.625rem",
                    lineHeight: "0.9rem",
                    textAlign: "center",
                  }}
                >
                  {notificationCenter.unreadCount > 9 ? "9+" : notificationCenter.unreadCount}
                </span>
              )}
            </HeaderGlobalAction>
            {notificationCenter.isOpen && (
              <NotificationPanel
                notifications={notificationCenter.notifications}
                isLoading={notificationCenter.isLoading}
                isError={notificationCenter.isError}
                onClose={notificationCenter.close}
                onMarkAsRead={notificationCenter.markAsRead}
                onMarkAllAsRead={notificationCenter.markAllAsRead}
              />
            )}
          </div>
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

      <SideNav aria-label={ariaLabel} expanded isFixedNav className="cg-side-nav">
        <SideNavItems>
          <SideNavLink as={Link} to={homeTo} renderIcon={DashboardIcon} isActive={pathname === homeTo}>
            Dashboard
          </SideNavLink>

          {navGroups.map((group) => (
            <Fragment key={group.label}>
              <li className="cg-side-nav__section-label">{group.label}</li>
              {group.items.map((item) => (
                <SideNavLink key={item.to} as={Link} to={item.to} renderIcon={item.icon} isActive={isItemActive(item.to)}>
                  {item.label}
                </SideNavLink>
              ))}
            </Fragment>
          ))}

          {navItems.map((item) => (
            <SideNavLink key={item.to} as={Link} to={item.to} renderIcon={item.icon} isActive={isItemActive(item.to)}>
              {item.label}
            </SideNavLink>
          ))}
        </SideNavItems>
      </SideNav>

      <div className="cg-topnav-content cg-topnav-content--with-sidenav">
        <Outlet />
      </div>
    </>
  );
}
