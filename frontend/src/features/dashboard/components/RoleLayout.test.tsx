import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ToolBox, Catalog, QrCode, SettingsAdjust } from "@carbon/icons-react";
import RoleLayout from "./RoleLayout";
import type { RoleNavGroup } from "./RoleLayout";

vi.mock("@thunderid/react", () => ({
  useThunderID: () => ({ getAccessToken: async () => "token", isSignedIn: true }),
  SignOutButton: ({ children }: { children: (args: { signOut: () => void }) => React.ReactNode }) => children({ signOut: () => {} }),
}));
vi.mock("@/features/notifications/hooks/useNotifications", () => ({
  useNotificationCenter: () => ({
    unreadCount: 0,
    isOpen: false,
    toggle: () => {},
    close: () => {},
    notifications: [],
    isLoading: false,
    isError: false,
    markAsRead: () => {},
    markAllAsRead: () => {},
  }),
}));

const ASSETS_GROUP: RoleNavGroup = {
  label: "Assets",
  items: [
    { to: "/admin/assets", label: "Register", icon: Catalog },
    { to: "/admin/assets/scan", label: "Scan QR", icon: QrCode },
    { to: "/admin/assets/config", label: "Asset Config", icon: SettingsAdjust },
  ],
};

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route
          path="/admin/*"
          element={<RoleLayout ariaLabel="Admin navigation" homeTo="/admin" navGroups={[ASSETS_GROUP]} navItems={[{ to: "/admin/maintenance", label: "Maintenance", icon: ToolBox }]} />}
        />
      </Routes>
    </MemoryRouter>,
  );
}

// Carbon's SideNavLink never sets aria-current — it only toggles this class
// when `isActive` is true (see @carbon/react's SideNavLink.js).
function isHighlighted(label: string) {
  return screen.getByRole("link", { name: new RegExp(label) }).classList.contains("cds--side-nav__link--current");
}

describe("RoleLayout — active nav item highlighting", () => {
  it("highlights only Register on the Register page, not its sibling routes", () => {
    renderAt("/admin/assets");
    expect(isHighlighted("Register")).toBe(true);
    expect(isHighlighted("Scan QR")).toBe(false);
    expect(isHighlighted("Asset Config")).toBe(false);
  });

  it("highlights only Scan QR on the Scan QR page — not Register, even though /admin/assets is a prefix of /admin/assets/scan", () => {
    renderAt("/admin/assets/scan");
    expect(isHighlighted("Scan QR")).toBe(true);
    expect(isHighlighted("Register")).toBe(false);
    expect(isHighlighted("Asset Config")).toBe(false);
  });

  it("highlights only Asset Config on the Asset Config page", () => {
    renderAt("/admin/assets/config");
    expect(isHighlighted("Asset Config")).toBe(true);
    expect(isHighlighted("Register")).toBe(false);
    expect(isHighlighted("Scan QR")).toBe(false);
  });

  it("highlights an unrelated flat nav item normally", () => {
    renderAt("/admin/maintenance");
    expect(isHighlighted("Maintenance")).toBe(true);
    expect(isHighlighted("Register")).toBe(false);
  });
});
