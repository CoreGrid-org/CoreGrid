import { useThunderID } from "@thunderid/react";
import { Tag, InlineNotification } from "@carbon/react";
import { Asset, ToolBox, ArrowsHorizontal, Search, Bot, Report } from "@carbon/icons-react";
import { getRoleLabel } from "@/features/auth/lib/roles";
import { formatStatusLabel } from "@/shared/lib/statusTag";
import BarChart from "../components/BarChart";
import LineChart from "../components/LineChart";
import FeatureCard from "../components/FeatureCard";
import { useDashboardSummary } from "../hooks/useDashboardSummary";
import { useDashboardCharts } from "../hooks/useDashboardCharts";
import { getErrorMessage } from "@/shared/lib/errorMessage";

// FR-082 — "assets by condition" is always New→Unserviceable, positionally
// zero-filled by the backend, so this ramp lines up with the response order.
const CONDITION_COLORS = ["#86b6ef", "#5598e7", "#2a78d6", "#1c5cab", "#104281"];

const FEATURE_CARDS = [
  { to: "/audit/audit", icon: Search, title: "Audit & Compliance", description: "Verification campaigns, discrepancies and the audit log." },
  { to: "/audit/reports", icon: Report, title: "Reports", description: "Audit, inventory and disposal reports." },
  { to: "/audit/assets", icon: Asset, title: "Asset Registry", description: "Register, search and track assets by QR code." },
  { to: "/audit/maintenance", icon: ToolBox, title: "Maintenance", description: "Faults, repairs and preventive schedules." },
  { to: "/audit/transfers", icon: ArrowsHorizontal, title: "Transfers & Disposals", description: "Department transfers and end-of-life disposal." },
  { to: "/audit/workflows", icon: Bot, title: "Agentic Workflows", description: "Track agent-recommended actions in progress." },
];

const formatCurrency = (value: number) => `LKR ${value.toLocaleString()}`;

// FR-081/FR-082: Auditor gets the same three visualisations as Administrator
// (only role besides Admin the SRS grants them to), plus indicators weighted
// toward org-wide read and compliance (open discrepancies) rather than
// operational counts like pending transfers/disposals.
export default function AuditDashboard() {
  const { user } = useThunderID();
  const summary = useDashboardSummary();
  const charts = useDashboardCharts();

  const assetsByDepartment = charts.data?.assets_by_department.map((d) => ({ label: d.label, value: d.value })) ?? [];
  const assetsByCondition = charts.data?.assets_by_condition.map((d, i) => ({
    label: formatStatusLabel(d.label),
    value: d.value,
    color: CONDITION_COLORS[i],
  })) ?? [];
  const maintenanceCostByMonth = charts.data?.maintenance_cost_by_month.map((d) => ({ label: d.label, value: d.value })) ?? [];

  const statTiles = summary.data
    ? [
        { label: "Total assets", value: summary.data.total_assets },
        { label: "Active assets", value: summary.data.active_assets },
        { label: "Under maintenance", value: summary.data.assets_under_maintenance },
        { label: "Open discrepancies", value: summary.data.open_discrepancies },
      ]
    : [];

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <div className="cg-page__title-row">
            <h1 className="cg-page__title">Dashboard</h1>
            <Tag type="blue">{getRoleLabel("Auditor")}</Tag>
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
          ? Array.from({ length: 4 }).map((_, i) => (
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

      {charts.isError && (
        <InlineNotification
          kind="error"
          title="Could not load dashboard charts"
          subtitle={getErrorMessage(charts.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div className="cg-chart-grid">
        <div className="cg-section">
          <div className="cg-section__header">
            <p className="cg-section__title">Assets by department</p>
          </div>
          <div className="cg-section__body">
            {charts.isLoading ? <p className="cg-table__muted">Loading…</p> : <BarChart data={assetsByDepartment} />}
          </div>
        </div>

        <div className="cg-section">
          <div className="cg-section__header">
            <p className="cg-section__title">Assets by condition</p>
          </div>
          <div className="cg-section__body">
            {charts.isLoading ? <p className="cg-table__muted">Loading…</p> : <BarChart data={assetsByCondition} />}
          </div>
        </div>
      </div>

      <div className="cg-section">
        <div className="cg-section__header">
          <p className="cg-section__title">Maintenance cost by month</p>
        </div>
        <div className="cg-section__body">
          {charts.isLoading ? (
            <p className="cg-table__muted">Loading…</p>
          ) : (
            <LineChart data={maintenanceCostByMonth} valueFormatter={formatCurrency} />
          )}
        </div>
      </div>

      <div className="cg-quick-grid" style={{ marginTop: "1.5rem" }}>
        {FEATURE_CARDS.map((card) => (
          <FeatureCard key={card.to} {...card} />
        ))}
      </div>
    </div>
  );
}
