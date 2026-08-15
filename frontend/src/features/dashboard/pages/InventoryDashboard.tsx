import { useThunderID } from "@thunderid/react";
import { Tag, InlineNotification } from "@carbon/react";
import { Asset, ToolBox, ArrowsHorizontal, Bot, Report } from "@carbon/icons-react";
import { getRoleLabel } from "@/features/auth/lib/roles";
import FeatureCard from "../components/FeatureCard";
import { useDashboardSummary } from "../hooks/useDashboardSummary";
import { getErrorMessage } from "@/shared/lib/errorMessage";

const FEATURE_CARDS = [
  { to: "/inventory/assets", icon: Asset, title: "Asset Registry", description: "Register, search and track assets by QR code." },
  { to: "/inventory/maintenance", icon: ToolBox, title: "Maintenance", description: "Faults, repairs and preventive schedules." },
  { to: "/inventory/transfers", icon: ArrowsHorizontal, title: "Transfers & Disposals", description: "Department transfers and end-of-life disposal." },
  { to: "/inventory/workflows", icon: Bot, title: "Agentic Workflows", description: "Track agent-recommended actions in progress." },
  { to: "/inventory/reports", icon: Report, title: "Reports", description: "Inventory, maintenance and disposal reports." },
];

// FR-081: role-appropriate indicators for InventoryOfficer — the operational
// subset (excludes open discrepancies and workflow approvals, which belong
// to Auditor/Administrator per the SRS permission matrix).
export default function InventoryDashboard() {
  const { user } = useThunderID();
  const summary = useDashboardSummary();

  const statTiles = summary.data
    ? [
        { label: "Total assets", value: summary.data.total_assets },
        { label: "Active assets", value: summary.data.active_assets },
        { label: "Under maintenance", value: summary.data.assets_under_maintenance },
        { label: "Pending transfers", value: summary.data.pending_transfers },
        { label: "Pending disposals", value: summary.data.pending_disposals },
      ]
    : [];

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <div className="cg-page__title-row">
            <h1 className="cg-page__title">Dashboard</h1>
            <Tag type="blue">{getRoleLabel("InventoryOfficer")}</Tag>
          </div>
          <p className="cg-page__subtitle">{user?.given_name ? `Welcome, ${user.given_name}.` : "Welcome."}</p>
        </div>
      </div>

      {summary.isError && (
        <InlineNotification
          kind="error"
          title="Could not load dashboard indicators"
          subtitle={getErrorMessage(summary.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div className="cg-stat-grid">
        {summary.isLoading
          ? Array.from({ length: 5 }).map((_, i) => (
              <div className="cg-stat-card" key={i}>
                <p className="cg-stat-card__label">Loading…</p>
                <p className="cg-stat-card__value">—</p>
              </div>
            ))
          : statTiles.map((stat) => (
              <div className="cg-stat-card" key={stat.label}>
                <p className="cg-stat-card__label">{stat.label}</p>
                <p className="cg-stat-card__value">{stat.value.toLocaleString()}</p>
              </div>
            ))}
      </div>

      <div className="cg-quick-grid" style={{ marginTop: "1.5rem" }}>
        {FEATURE_CARDS.map((card) => (
          <FeatureCard key={card.to} {...card} />
        ))}
      </div>
    </div>
  );
}
