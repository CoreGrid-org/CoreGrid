import { useState, useMemo } from "react";
import {
  Modal,
  Tag,
  InlineNotification,
  Search,
  ContentSwitcher,
  Switch,
} from "@carbon/react";
import { useCampaignTasks } from "../hooks/useCampaignTasks";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";

interface CampaignTasksModalProps {
  campaignId: string;
  campaignName: string;
  onClose: () => void;
}

export default function CampaignTasksModal({
  campaignId,
  campaignName,
  onClose,
}: CampaignTasksModalProps) {
  const { data: tasks, isLoading, isError, error } = useCampaignTasks(campaignId);
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | "pending" | "completed">("all");

  const filteredTasks = useMemo(() => {
    if (!tasks) return [];
    return tasks.filter((t) => {
      if (statusFilter === "pending" && t.status !== "Pending") return false;
      if (statusFilter === "completed" && t.status !== "Completed") return false;

      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase();
        const codeMatch = t.asset_code.toLowerCase().includes(q);
        const nameMatch = t.asset_name.toLowerCase().includes(q);
        const emailMatch = t.assigned_to_email?.toLowerCase().includes(q) ?? false;
        return codeMatch || nameMatch || emailMatch;
      }
      return true;
    });
  }, [tasks, statusFilter, searchQuery]);

  const totalCount = tasks?.length ?? 0;
  const completedCount = tasks?.filter((t) => t.status === "Completed").length ?? 0;
  const pendingCount = totalCount - completedCount;

  return (
    <Modal
      open
      modalHeading={`Tasks — ${campaignName}`}
      modalLabel="Verification Campaign"
      passiveModal
      onRequestClose={onClose}
      size="lg"
    >
      <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
        {isError && (
          <InlineNotification
            kind="error"
            title="Could not load verification tasks"
            subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
            lowContrast
            hideCloseButton
            style={{ maxWidth: "100%" }}
          />
        )}

        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: "1rem",
          }}
        >
          <div style={{ display: "flex", gap: "0.5rem", alignItems: "center" }}>
            <Tag type="blue">Total: {totalCount}</Tag>
            <Tag type="teal">Completed: {completedCount}</Tag>
            <Tag type="cool-gray">Pending: {pendingCount}</Tag>
          </div>

          <ContentSwitcher
            selectedIndex={statusFilter === "all" ? 0 : statusFilter === "pending" ? 1 : 2}
            onChange={(e) => {
              const name = e.name as "all" | "pending" | "completed";
              if (name) setStatusFilter(name);
            }}
            size="sm"
          >
            <Switch name="all" text="All" />
            <Switch name="pending" text="Pending" />
            <Switch name="completed" text="Completed" />
          </ContentSwitcher>
        </div>

        <Search
          id="campaign-tasks-search"
          size="sm"
          labelText="Search tasks"
          placeholder="Filter by asset code, asset name, or assigned officer email..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />

        {isLoading ? (
          <div className="cg-placeholder">
            <p>Loading verification tasks…</p>
          </div>
        ) : filteredTasks.length > 0 ? (
          <div style={{ maxHeight: "420px", overflowY: "auto" }}>
            <table className="cg-table cg-table--no-hover">
              <thead>
                <tr>
                  <th>Asset</th>
                  <th>Assigned Officer</th>
                  <th>Due Date</th>
                  <th>Status</th>
                  <th>Assertion Details</th>
                </tr>
              </thead>
              <tbody>
                {filteredTasks.map((t) => (
                  <tr key={t.id}>
                    <td>
                      <div>
                        <strong>{t.asset_code}</strong>
                        <div className="cg-table__muted" style={{ fontSize: "0.8125rem" }}>
                          {t.asset_name}
                        </div>
                      </div>
                    </td>
                    <td className="cg-table__muted">
                      {t.assigned_to_email || <em style={{ color: "#8d8d8d" }}>Unassigned</em>}
                    </td>
                    <td className="cg-table__muted">{t.due_date}</td>
                    <td>
                      <Tag type={statusTagColor(t.status)}>{formatStatusLabel(t.status)}</Tag>
                    </td>
                    <td className="cg-table__muted" style={{ fontSize: "0.8125rem" }}>
                      {t.status === "Completed" ? (
                        t.asserted_present ? (
                          <span>
                            Found • Condition: <strong>{t.asserted_condition ?? "N/A"}</strong>
                            {t.asserted_location_name && (
                              <span> • Location: {t.asserted_location_name}</span>
                            )}
                          </span>
                        ) : (
                          <span style={{ color: "#da1e28", fontWeight: 600 }}>
                            Asserted Missing
                          </span>
                        )
                      ) : (
                        <span style={{ color: "#8d8d8d" }}>Awaiting verification</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="cg-placeholder">
            <p>No verification tasks found.</p>
          </div>
        )}
      </div>
    </Modal>
  );
}
