import { useState } from "react";
import { Tag, Dropdown, Pagination, InlineNotification, Button } from "@carbon/react";
import { FilterReset } from "@carbon/icons-react";
import DateRangeFilter from "@/shared/components/DateRangeFilter";
import { endOfDayIso, formatDateTime, hasDateRangeErrors, startOfDayIso, validateDateRange, type DateRange } from "@/shared/lib/dates";
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
  const [range, setRange] = useState<DateRange>({});
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const rangeErrors = validateDateRange(range);
  const rangeValid = !hasDateRangeErrors(rangeErrors);
  // The endpoint filters on exact timestamps: send the whole local day on
  // both ends so "to 2026-09-10" includes everything logged on the 10th.
  const auditLog = useAuditLog(
    {
      entityType,
      operation,
      from: range.from ? startOfDayIso(range.from) : undefined,
      to: range.to ? endOfDayIso(range.to) : undefined,
      page,
      pageSize,
    },
    rangeValid,
  );
  const hasFilters = Boolean(entityType || operation || range.from || range.to);

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
          <DateRangeFilter
            idPrefix="audit"
            value={range}
            onChange={(r) => {
              setRange(r);
              setPage(1);
            }}
            errors={rangeErrors}
          />
          <Button
            kind="ghost"
            size="md"
            renderIcon={FilterReset}
            disabled={!hasFilters}
            onClick={() => {
              setEntityType(undefined);
              setOperation(undefined);
              setRange({});
              setPage(1);
            }}
          >
            Clear filters
          </Button>
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
                  <td className="cg-table__mono">{formatDateTime(e.created_at)}</td>
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
