import { useThunderID } from "@thunderid/react";
import { Tag, InlineNotification } from "@carbon/react";
import { Asset, ToolBox } from "@carbon/icons-react";
import { getRoleLabel } from "@/features/auth/lib/roles";
import FeatureCard from "../components/FeatureCard";
import { useDashboardSummary } from "../hooks/useDashboardSummary";
import { getErrorMessage } from "@/shared/lib/errorMessage";

const FEATURE_CARDS = [
  { to: "/staff/assets", icon: Asset, title: "My Assets", description: "Assets registered to your department." },
  { to: "/staff/maintenance", icon: ToolBox, title: "Maintenance", description: "Report a fault and track its status." },
];

// FR-081: role-appropriate indicators for Staff — the minimal read-only
// subset (Staff has no create/approve permissions anywhere in the SRS
// matrix). Note: these counts come from GET /api/dashboard/summary, which
// is org-wide today, not yet scoped to the caller's own department
// (FR-086 — tracked in doc/PROGRESS.md).
export default function StaffDashboard() {
  const { user } = useThunderID();
  const summary = useDashboardSummary();

  const statTiles = summary.data
    ? [
        { label: "Total assets", value: summary.data.total_assets },
        { label: "Active assets", value: summary.data.active_assets },
        { label: "Under maintenance", value: summary.data.assets_under_maintenance },
      ]
    : [];

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <div className="cg-page__title-row">
            <h1 className="cg-page__title">Dashboard</h1>
            <Tag type="blue">{getRoleLabel("Staff")}</Tag>
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
          ? Array.from({ length: 3 }).map((_, i) => (
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
