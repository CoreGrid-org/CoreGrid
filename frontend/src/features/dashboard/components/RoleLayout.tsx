import { Fragment, useRef, useState } from "react";
import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import type { ComponentType } from "react";
import {
  Header,
  HeaderName,
  HeaderGlobalBar,
  HeaderGlobalAction,
  SideNav,
  SideNavItems,
  SideNavLink,
  Modal,
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

// Defines a labelled navigation section.
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
// Provides shared layout and navigation for role-based routes.
export default function RoleLayout({ ariaLabel, homeTo, navItems = [], navGroups = [] }: RoleLayoutProps) {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const notificationCenter = useNotificationCenter();
  const notificationAreaRef = useRef<HTMLDivElement>(null);
  const [isSignOutOpen, setIsSignOutOpen] = useState(false);
  const [isSigningOut, setIsSigningOut] = useState(false);
// Activates only the most specific matching navigation item.
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
          <div ref={notificationAreaRef} className="cg-header-notifications">
            <HeaderGlobalAction
              aria-label={`Notifications${notificationCenter.unreadCount > 0 ? ` (${notificationCenter.unreadCount} unread)` : ""}`}
              onClick={notificationCenter.toggle}
              isActive={notificationCenter.isOpen}
            >
              <Notification size={20} className="cg-header-icon" />
              {notificationCenter.unreadCount > 0 && (
                <span aria-hidden="true" className="cg-header-badge">
                  {notificationCenter.unreadCount > 9 ? "9+" : notificationCenter.unreadCount}
                </span>
              )}
            </HeaderGlobalAction>
            {notificationCenter.isOpen && (
              <NotificationPanel
                notifications={notificationCenter.notifications}
                isLoading={notificationCenter.isLoading}
                isError={notificationCenter.isError}
                containerRef={notificationAreaRef}
                homeTo={homeTo}
                onClose={notificationCenter.close}
                onRetry={notificationCenter.retry}
                onMarkAllAsRead={notificationCenter.markAllAsRead}
                onOpenNotification={(n, link) => {
                  void notificationCenter.markAsRead(n.id);
                  if (link) {
                    notificationCenter.close();
                    navigate(link);
                  }
                }}
              />
            )}
          </div>
          <HeaderGlobalAction aria-label="Sign out" onClick={() => setIsSignOutOpen(true)}>
            <Logout size={20} className="cg-header-icon" />
          </HeaderGlobalAction>
          <HeaderGlobalAction onClick={() => navigate(`${homeTo}/profile`)} aria-label="Open user profile">
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

      {/* Mounted only while open: a closed Carbon Modal still renders its buttons. */}
      {isSignOutOpen && (
        <SignOutButton>
          {({ signOut }) => (
            <Modal
              open
              size="xs"
              modalHeading="Sign out of CoreGrid?"
              primaryButtonText={isSigningOut ? "Signing out…" : "Sign out"}
              secondaryButtonText="Cancel"
              primaryButtonDisabled={isSigningOut}
              onRequestClose={() => !isSigningOut && setIsSignOutOpen(false)}
              onRequestSubmit={() => {
                setIsSigningOut(true);
                Promise.resolve(signOut()).catch(() => setIsSigningOut(false));
              }}
            >
              <p className="cg-modal-form__intro">
                You'll need to sign in again to continue. Anything you haven't saved on this page will be lost.
              </p>
            </Modal>
          )}
        </SignOutButton>
      )}
    </>
  );
}
