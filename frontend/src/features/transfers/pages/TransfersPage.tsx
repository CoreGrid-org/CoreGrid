import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button } from "@carbon/react";
import { Add, CheckmarkFilled, CloseFilled } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { MOCK_TRANSFERS, MOCK_DISPOSALS } from "../data/mockTransfers";

export default function TransfersPage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Transfers & Disposals</h1>
          <p className="cg-page__subtitle">Department transfers and end-of-life disposal (FR-043 to FR-055).</p>
        </div>
        <Button renderIcon={Add}>New request</Button>
      </div>

      <Tabs>
        <TabList aria-label="Transfer and disposal sections">
          <Tab>Transfers</Tab>
          <Tab>Disposals</Tab>
        </TabList>
        <TabPanels>
          {/* ── Transfers ───────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-043", "FR-045", "FR-046", "FR-047"]}>
              An Inventory Officer raises a transfer with a destination department and reason; an
              Administrator approves it (asset → IN_TRANSIT); the receiving officer scans the asset to
              confirm receipt, which moves ownership and returns the asset to ACTIVE.
            </MockNotice>

            <div className="cg-section">
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Asset</th>
                    <th>From</th>
                    <th>To</th>
                    <th>Status</th>
                    <th>Requested by</th>
                    <th>Requested</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_TRANSFERS.map((t, i) => (
                    <tr key={i}>
                      <td>
                        <span className="cg-table__mono">{t.assetCode}</span>
                        <br />
                        <span className="cg-table__muted">{t.assetName}</span>
                      </td>
                      <td className="cg-table__muted">{t.fromDepartment}</td>
                      <td className="cg-table__muted">{t.toDepartment}</td>
                      <td>
                        <Tag type={statusTagColor(t.status)}>{formatStatusLabel(t.status)}</Tag>
                      </td>
                      <td className="cg-table__muted">{t.requestedBy}</td>
                      <td className="cg-table__muted">{t.requestedAt}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Disposals ───────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-050", "FR-051", "FR-052", "FR-053"]}>
              Disposal is the system's only irreversible action. Approval is permitted only when every
              precondition (P1–P6) is satisfied and the approver is not the requester (separation of duties);
              the checklist below is what the real approval screen renders live before enabling Approve.
            </MockNotice>

            <div className="cg-section">
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Asset</th>
                    <th>Proposed method</th>
                    <th>Status</th>
                    <th>Valuation</th>
                    <th>Requested by</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_DISPOSALS.map((d, i) => (
                    <tr key={i}>
                      <td>
                        <span className="cg-table__mono">{d.assetCode}</span>
                        <br />
                        <span className="cg-table__muted">{d.assetName}</span>
                      </td>
                      <td className="cg-table__muted">{formatStatusLabel(d.proposedMethod)}</td>
                      <td>
                        <Tag type={statusTagColor(d.status)}>{formatStatusLabel(d.status)}</Tag>
                      </td>
                      <td className="cg-table__muted">{d.valuation ? `$${d.valuation.toLocaleString()}` : "—"}</td>
                      <td className="cg-table__muted">{d.requestedBy}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {MOCK_DISPOSALS.filter((d) => d.preconditions.length > 0).map((d, i) => (
              <div className="cg-section" key={i}>
                <div className="cg-section__header">
                  <p className="cg-section__title">
                    Precondition checklist — {d.assetCode} ({formatStatusLabel(d.status)})
                  </p>
                  <Button kind="danger" size="sm" disabled={d.preconditions.some((p) => !p.satisfied)}>
                    Approve disposal
                  </Button>
                </div>
                <div className="cg-section__body" style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
                  {d.preconditions.map((p) => (
                    <div key={p.id} style={{ display: "flex", alignItems: "center", gap: "0.625rem" }}>
                      {p.satisfied ? (
                        <CheckmarkFilled size={18} style={{ fill: "#24a148", flexShrink: 0 }} />
                      ) : (
                        <CloseFilled size={18} style={{ fill: "#da1e28", flexShrink: 0 }} />
                      )}
                      <span style={{ fontSize: "0.8125rem", color: "#525252", fontWeight: 600 }}>{p.id}</span>
                      <span style={{ fontSize: "0.875rem" }}>{p.label}</span>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  );
}
