import { useState } from "react";
import { Tag, Dropdown, DatePicker, DatePickerInput, Pagination, InlineNotification } from "@carbon/react";
import { useAuditLog } from "../hooks/useAuditLog";
import { getErrorMessage } from "@/shared/lib/errorMessage";

const ENTITY_TYPES = [
  "Asset",
  "AssetTransfer",
  "DisposalRequest",
  "Department",
  "Location",
  "OrganizationPolicy",
  "User",
  "VerificationCampaign",
  "VerificationTask",
  "Discrepancy",
];
const OPERATIONS = ["Create", "Update", "Delete"];
const OPERATION_TAG: Record<string, "green" | "blue" | "red"> = { Create: "green", Update: "blue", Delete: "red" };

export default function AuditLogPanel() {
  const [entityType, setEntityType] = useState<string | undefined>();
  const [operation, setOperation] = useState<string | undefined>();
  const [from, setFrom] = useState<string | undefined>();
  const [to, setTo] = useState<string | undefined>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const auditLog = useAuditLog({ entityType, operation, from, to, page, pageSize });

  return (
    <>
      <p className="cg-table__muted cg-panel-intro">
        Every state-changing operation writes an immutable entry (actor, entity, operation, before/after
        values, timestamp and correlation id). Not editable or deletable through any API.
      </p>

      <div className="cg-section">
        <div className="cg-toolbar">
          <Dropdown
            id="audit-entity-type"
            titleText="Entity"
            label="All entities"
            items={["", ...ENTITY_TYPES]}
            itemToString={(item) => (item ? item : "All entities")}
            selectedItem={entityType ?? ""}
            onChange={({ selectedItem }) => {
              setEntityType(selectedItem || undefined);
              setPage(1);
            }}
            style={{ minWidth: "12rem" }}
          />
          <Dropdown
            id="audit-operation"
            titleText="Operation"
            label="All operations"
            items={["", ...OPERATIONS]}
            itemToString={(item) => (item ? item : "All operations")}
            selectedItem={operation ?? ""}
            onChange={({ selectedItem }) => {
              setOperation(selectedItem || undefined);
              setPage(1);
            }}
            style={{ minWidth: "10rem" }}
          />
          <DatePicker
            datePickerType="single"
            dateFormat="Y-m-d"
            onChange={([date]) => {
              setFrom(date ? date.toISOString() : undefined);
              setPage(1);
            }}
          >
            <DatePickerInput id="audit-from" labelText="From" placeholder="yyyy-mm-dd" />
          </DatePicker>
          <DatePicker
            datePickerType="single"
            dateFormat="Y-m-d"
            onChange={([date]) => {
              setTo(date ? date.toISOString() : undefined);
              setPage(1);
            }}
          >
            <DatePickerInput id="audit-to" labelText="To" placeholder="yyyy-mm-dd" />
          </DatePicker>
        </div>
      </div>

      {auditLog.isError && (
        <InlineNotification
          kind="error"
          title="Could not load the audit log"
          subtitle={getErrorMessage(auditLog.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          className="cg-panel-notification"
        />
      )}

      <div className="cg-section">
        {auditLog.isLoading ? (
          <div className="cg-placeholder">
            <p>Loading the audit log…</p>
          </div>
        ) : auditLog.data && auditLog.data.items.length > 0 ? (
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Actor</th>
                <th>Entity</th>
                <th>Operation</th>
                <th>Correlation ID</th>
              </tr>
            </thead>
            <tbody>
              {auditLog.data.items.map((e) => (
                <tr key={e.id}>
                  <td className="cg-table__mono">{new Date(e.created_at).toLocaleString()}</td>
                  <td>{e.actor_email ?? "System"}</td>
                  <td className="cg-table__muted">
                    {e.entity_type}
                    {e.entity_id ? ` (${e.entity_id.slice(0, 8)})` : ""}
                  </td>
                  <td>
                    <Tag type={OPERATION_TAG[e.operation] ?? "gray"}>{e.operation}</Tag>
                  </td>
                  <td className="cg-table__mono">{e.correlation_id.slice(0, 8)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="cg-placeholder">
            <p>No audit log entries match these filters.</p>
          </div>
        )}
      </div>

      {auditLog.data && auditLog.data.total_count > 0 && (
        <Pagination
          page={page}
          pageSize={pageSize}
          pageSizes={[10, 25, 50, 100]}
          totalItems={auditLog.data.total_count}
          onChange={({ page: nextPage, pageSize: nextPageSize }) => {
            setPage(nextPage);
            setPageSize(nextPageSize);
          }}
        />
      )}
    </>
  );
}
