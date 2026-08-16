import { useThunderID } from "@thunderid/react";
import { Tag, InlineNotification } from "@carbon/react";
import { Asset, ToolBox, ArrowsHorizontal, Search, Bot, Report } from "@carbon/icons-react";
import { getRoleLabel } from "@/features/auth/lib/roles";
import BarChart from "../components/BarChart";
import LineChart from "../components/LineChart";
import FeatureCard from "../components/FeatureCard";
import { useDashboardSummary } from "../hooks/useDashboardSummary";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import {
  MOCK_ASSETS_BY_DEPARTMENT,
  MOCK_ASSETS_BY_CONDITION,
  MOCK_MAINTENANCE_COST_BY_MONTH,
} from "../data/mockOverview";

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

      <div className="cg-chart-grid">
        <div className="cg-section">
          <div className="cg-section__header">
            <p className="cg-section__title">Assets by department</p>
          </div>
          <div className="cg-section__body">
            <BarChart data={MOCK_ASSETS_BY_DEPARTMENT} />
          </div>
        </div>

        <div className="cg-section">
          <div className="cg-section__header">
            <p className="cg-section__title">Assets by condition</p>
          </div>
          <div className="cg-section__body">
            <BarChart data={MOCK_ASSETS_BY_CONDITION} />
          </div>
        </div>
      </div>

      <div className="cg-section">
        <div className="cg-section__header">
          <p className="cg-section__title">Maintenance cost by month</p>
        </div>
        <div className="cg-section__body">
          <LineChart data={MOCK_MAINTENANCE_COST_BY_MONTH} valueFormatter={formatCurrency} />
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
