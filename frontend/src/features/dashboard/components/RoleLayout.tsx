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
  SideNavMenu,
  SideNavMenuItem,
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

export interface RoleNavGroup {
  label: string;
  icon: ComponentType<{ size?: number; className?: string }>;
  items: RoleNavItem[];
}

interface RoleLayoutProps {
  ariaLabel: string;
  homeTo: string;
  navItems: RoleNavItem[];
  navGroups?: RoleNavGroup[];
}

// Shared chrome for every per-role dashboard route: a minimal top header
// (logo, then only global actions) plus a persistent side nav for
// everything else, wrapping whichever page is active via <Outlet>. Extracted
// from what was originally AdminLayout-only markup so InventoryLayout,
// AuditLayout and StaffLayout render identical chrome with role-specific
// nav content instead of duplicating this file three times.
export default function RoleLayout({ ariaLabel, homeTo, navItems, navGroups = [] }: RoleLayoutProps) {
  const { pathname } = useLocation();
  const notificationCenter = useNotificationCenter();

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
            <SideNavMenu
              key={group.label}
              title={group.label}
              renderIcon={group.icon}
              defaultExpanded={group.items.some((item) => pathname.startsWith(item.to))}
            >
              {group.items.map((item) => (
                <SideNavMenuItem key={item.to} as={Link} to={item.to} isActive={pathname === item.to}>
                  <span className="cg-side-nav__submenu-item">
                    <item.icon size={16} />
                    {item.label}
                  </span>
                </SideNavMenuItem>
              ))}
            </SideNavMenu>
          ))}

          {navItems.map((item) => {
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
