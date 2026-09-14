import { useState } from "react";
import { Tag, Button, Dropdown, InlineNotification } from "@carbon/react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { useCampaignsList } from "../hooks/useCampaigns";
import { useDiscrepanciesList } from "../hooks/useDiscrepancies";
import ResolveDiscrepancyModal from "./ResolveDiscrepancyModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Discrepancy } from "../api/discrepancies";

const DISCREPANCY_STATUS_FILTERS = ["Open only", "All"];

export default function DiscrepanciesPanel() {
  const campaigns = useCampaignsList();
  const [campaignId, setCampaignId] = useState<string | undefined>();
  const [statusFilter, setStatusFilter] = useState(DISCREPANCY_STATUS_FILTERS[0]);
  const discrepancies = useDiscrepanciesList({ campaignId, onlyOpen: statusFilter === "Open only" });
  const [resolvingDiscrepancy, setResolvingDiscrepancy] = useState<Discrepancy | null>(null);

  return (
    <>
      <div className="cg-section">
        <div className="cg-toolbar">
          <Dropdown
            id="discrepancy-campaign"
            titleText="Campaign"
            label="All campaigns"
            items={["", ...(campaigns.data?.map((c) => c.id) ?? [])]}
            itemToString={(id) => (id ? campaigns.data?.find((c) => c.id === id)?.name ?? id : "All campaigns")}
            selectedItem={campaignId ?? ""}
            onChange={({ selectedItem }) => setCampaignId(selectedItem || undefined)}
            style={{ minWidth: "16rem" }}
          />
          <Dropdown
            id="discrepancy-status"
            titleText="Status"
            label={statusFilter}
            items={DISCREPANCY_STATUS_FILTERS}
            selectedItem={statusFilter}
            onChange={({ selectedItem }) => setStatusFilter(selectedItem ?? DISCREPANCY_STATUS_FILTERS[0])}
            style={{ minWidth: "10rem" }}
          />
        </div>
      </div>

      {discrepancies.isError && (
        <InlineNotification
          kind="error"
          title="Could not load discrepancies"
          subtitle={getErrorMessage(discrepancies.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          className="cg-panel-notification"
        />
      )}

      <div className="cg-section">
        {discrepancies.isLoading ? (
          <div className="cg-placeholder">
            <p>Loading discrepancies…</p>
          </div>
        ) : discrepancies.data && discrepancies.data.length > 0 ? (
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Asset</th>
                <th>Classification</th>
                <th>Status</th>
                <th>Raised by</th>
                <th>Date</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {discrepancies.data.map((d) => (
                <tr key={d.id}>
                  <td className="cg-table__mono">{d.asset_code}</td>
                  <td>
                    <Tag type={statusTagColor(d.type)}>{formatStatusLabel(d.type)}</Tag>
                  </td>
                  <td>
                    <Tag type={statusTagColor(d.status)}>{formatStatusLabel(d.status)}</Tag>
                  </td>
                  <td className="cg-table__muted">{d.is_automatic ? "System (auto)" : d.raised_by_email ?? "—"}</td>
                  <td className="cg-table__muted">{new Date(d.created_at).toLocaleDateString()}</td>
                  <td>
                    {d.status === "Open" && (
                      <Button kind="ghost" size="sm" onClick={() => setResolvingDiscrepancy(d)}>
                        Resolve
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="cg-placeholder">
            <p>No discrepancies match these filters.</p>
          </div>
        )}
      </div>

      {resolvingDiscrepancy && (
        <ResolveDiscrepancyModal
          discrepancy={resolvingDiscrepancy}
          onClose={() => setResolvingDiscrepancy(null)}
          onResolved={() => {
            setResolvingDiscrepancy(null);
            discrepancies.refetch();
            campaigns.refetch();
          }}
        />
      )}
    </>
  );
}
