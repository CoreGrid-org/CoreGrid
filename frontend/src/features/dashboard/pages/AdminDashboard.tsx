import { useThunderID } from "@thunderid/react";
import { Tag } from "@carbon/react";
import { Asset, ToolBox, ArrowsHorizontal, Search, Bot, UserMultiple, Report, Settings } from "@carbon/icons-react";
import { getRoleLabel } from "@/features/auth/lib/roles";
import BarChart from "../components/BarChart";
import LineChart from "../components/LineChart";
import FeatureCard from "../components/FeatureCard";
import {
  MOCK_STAT_TILES,
  MOCK_ASSETS_BY_DEPARTMENT,
  MOCK_ASSETS_BY_CONDITION,
  MOCK_MAINTENANCE_COST_BY_MONTH,
} from "../data/mockOverview";

const FEATURE_CARDS = [
  { to: "/admin/assets", icon: Asset, title: "Asset Registry", description: "Register, search and track assets by QR code." },
  { to: "/admin/maintenance", icon: ToolBox, title: "Maintenance", description: "Faults, repairs and preventive schedules." },
  { to: "/admin/transfers", icon: ArrowsHorizontal, title: "Transfers & Disposals", description: "Department transfers and end-of-life disposal." },
  { to: "/admin/audit", icon: Search, title: "Audit & Compliance", description: "Verification campaigns, discrepancies and the audit log." },
  { to: "/admin/workflows", icon: Bot, title: "Agentic Workflows", description: "Review and approve agent-recommended actions." },
  { to: "/admin/users", icon: UserMultiple, title: "Users & Roles", description: "Invite users and manage their roles." },
  { to: "/admin/reports", icon: Report, title: "Reports", description: "Inventory, maintenance, disposal and audit reports." },
  { to: "/admin/settings", icon: Settings, title: "Organisation Settings", description: "Departments, locations, asset types and policy." },
];

const formatCurrency = (value: number) => `$${value.toLocaleString()}`;

export default function AdminDashboard() {
  const { user } = useThunderID();

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <div className="cg-page__title-row">
            <h1 className="cg-page__title">Dashboard</h1>
            <Tag type="blue">{getRoleLabel("Administrator")}</Tag>
          </div>
          <p className="cg-page__subtitle">{user?.given_name ? `Welcome, ${user.given_name}.` : "Welcome."}</p>
        </div>
      </div>

      <div className="cg-stat-grid">
        {MOCK_STAT_TILES.map((stat) => (
          <div className="cg-stat-card" key={stat.label}>
            <p className="cg-stat-card__label">{stat.label}</p>
            <p className="cg-stat-card__value">{stat.value}</p>
          </div>
        ))}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem", marginBottom: "1rem" }}>
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
