import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Tag, Button, InlineNotification, SkeletonText } from "@carbon/react";
import { ArrowLeft, Checkmark, Play, TrashCan } from "@carbon/icons-react";
import { useMaintenanceDetail, useStartMaintenance } from "../hooks/useMaintenance";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";

import ApproveMaintenanceModal from "../components/ApproveMaintenanceModal";
import CompleteMaintenanceModal from "../components/CompleteMaintenanceModal";
import CancelMaintenanceModal from "../components/CancelMaintenanceModal";

export default function MaintenanceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: record, isLoading, isError, error, refetch } = useMaintenanceDetail(id);
  const startMaintenance = useStartMaintenance();

  const [isApproveOpen, setApproveOpen] = useState(false);
  const [isCompleteOpen, setCompleteOpen] = useState(false);
  const [isCancelOpen, setCancelOpen] = useState(false);

  const handleStart = () => {
    if (!id) return;
    startMaintenance.mutate(id, {
      onSuccess: () => refetch(),
    });
  };

  if (isLoading) {
    return (
      <div className="cg-page">
        <SkeletonText paragraph lineCount={5} />
      </div>
    );
  }

  if (isError || !record) {
    return (
      <div className="cg-page">
        <InlineNotification
          kind="error"
          title="Error loading maintenance record"
          subtitle={getErrorMessage(error, "Could not load record.")}
          lowContrast
          hideCloseButton
        />
        <Button onClick={() => navigate("..")} renderIcon={ArrowLeft}>Back</Button>
      </div>
    );
  }

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <Button
            kind="ghost"
            onClick={() => navigate("..")}
            renderIcon={ArrowLeft}
            style={{ marginBottom: "1rem" }}
          >
            Back to Maintenance
          </Button>
          <h1 className="cg-page__title">
            Maintenance Record {record.id.split("-")[0]}
          </h1>
          <p className="cg-page__subtitle">
            Asset: {record.asset_code} - {record.asset_name}
          </p>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          {record.status === "REQUESTED" && (
            <Button kind="primary" onClick={() => setApproveOpen(true)}>
              Approve
            </Button>
          )}
          {record.status === "APPROVED" && (
            <Button kind="primary" renderIcon={Play} disabled={startMaintenance.isPending} onClick={handleStart}>
              {startMaintenance.isPending ? "Starting..." : "Start Work"}
            </Button>
          )}
          {record.status === "IN_PROGRESS" && (
            <Button kind="primary" renderIcon={Checkmark} onClick={() => setCompleteOpen(true)}>
              Complete Work
            </Button>
          )}
          {["REQUESTED", "APPROVED", "IN_PROGRESS"].includes(record.status) && (
            <Button kind="danger" renderIcon={TrashCan} onClick={() => setCancelOpen(true)}>
              Cancel
            </Button>
          )}
        </div>
      </div>

      <div className="cg-section">
        <div className="cg-kv-grid cg-kv-grid--three" style={{ marginBottom: "2rem" }}>
          <div className="cg-kv-item">
            <p className="cg-kv-item__label">Status</p>
            <div>
              <Tag type={statusTagColor(record.status)}>{formatStatusLabel(record.status)}</Tag>
            </div>
          </div>
          <div className="cg-kv-item">
            <p className="cg-kv-item__label">Type</p>
            <p className="cg-kv-item__value">{record.type === "CORRECTIVE" ? "Corrective" : "Preventive"}</p>
          </div>
          <div className="cg-kv-item">
            <p className="cg-kv-item__label">Priority</p>
            <div>
              <Tag type={statusTagColor(record.priority)}>{formatStatusLabel(record.priority)}</Tag>
            </div>
          </div>
          <div className="cg-kv-item">
            <p className="cg-kv-item__label">Assignee</p>
            <p className="cg-kv-item__value">{record.assignee_email || "Unassigned"}</p>
          </div>
          <div className="cg-kv-item">
            <p className="cg-kv-item__label">Estimated Cost</p>
            <p className="cg-kv-item__value">{record.estimated_cost ? `$${record.estimated_cost.toLocaleString()}` : "N/A"}</p>
          </div>
          <div className="cg-kv-item">
            <p className="cg-kv-item__label">Actual Cost</p>
            <p className="cg-kv-item__value">{record.actual_cost ? `$${record.actual_cost.toLocaleString()}` : "N/A"}</p>
          </div>
          <div className="cg-kv-item">
            <p className="cg-kv-item__label">Observed Condition</p>
            <p className="cg-kv-item__value">{record.observed_condition ? formatStatusLabel(record.observed_condition) : "Not provided"}</p>
          </div>
          {record.resulting_condition && (
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Resulting Condition</p>
              <p className="cg-kv-item__value">{formatStatusLabel(record.resulting_condition)}</p>
            </div>
          )}
          <div className="cg-kv-item">
            <p className="cg-kv-item__label">Date Created</p>
            <p className="cg-kv-item__value">{new Date(record.created_at).toLocaleDateString()}</p>
          </div>
        </div>

        <p className="cg-section__title" style={{ marginTop: "1.5rem" }}>Description</p>
        <p className="cg-table__muted" style={{ whiteSpace: "pre-wrap", marginBottom: "1.5rem" }}>{record.description}</p>

        {record.work_performed && (
          <>
            <p className="cg-section__title" style={{ marginTop: "1.5rem" }}>Work Performed</p>
            <p className="cg-table__muted" style={{ whiteSpace: "pre-wrap", marginBottom: "1.5rem" }}>{record.work_performed}</p>
          </>
        )}
      </div>

      <ApproveMaintenanceModal
        isOpen={isApproveOpen}
        onClose={() => setApproveOpen(false)}
        record={record}
        onSuccess={refetch}
      />
      <CompleteMaintenanceModal
        isOpen={isCompleteOpen}
        onClose={() => setCompleteOpen(false)}
        record={record}
        onSuccess={refetch}
      />
      <CancelMaintenanceModal
        isOpen={isCancelOpen}
        onClose={() => setCancelOpen(false)}
        record={record}
        onSuccess={refetch}
      />
    </div>
  );
}
