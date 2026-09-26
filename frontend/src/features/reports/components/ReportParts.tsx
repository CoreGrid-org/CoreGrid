import type { ReactNode } from "react";
import { Button, InlineLoading, InlineNotification, Pagination, SkeletonText, Tag } from "@carbon/react";
import { DocumentExport, DocumentPdf, FilterReset } from "@carbon/icons-react";
import { useClientPagination } from "@/shared/hooks/useClientPagination";
import { getErrorMessage } from "@/shared/lib/errorMessage";

// Building blocks every report panel on the Reports page is made of:
// filters card → stats → export bar → summary table → paged detail table.

export function ReportFilters({
  activeCount,
  onClear,
  isRefreshing = false,
  children,
}: {
  activeCount: number;
  onClear: () => void;
  isRefreshing?: boolean;
  children: ReactNode;
}) {
  return (
    <section className="cg-section cg-report__filters">
      <header className="cg-section__header">
        <div className="cg-report__filters-title">
          <p className="cg-section__title">Filters</p>
          {activeCount > 0 && <Tag size="sm" type="blue">{activeCount} active</Tag>}
          {isRefreshing && <InlineLoading description="Updating…" className="cg-report__refreshing" />}
        </div>
        <Button kind="ghost" size="sm" renderIcon={FilterReset} disabled={activeCount === 0} onClick={onClear}>
          Clear filters
        </Button>
      </header>
      <div className="cg-section__body cg-report__filter-grid">{children}</div>
    </section>
  );
}

export function ReportStatus({ title, error, isInitialLoading }: { title: string; error: unknown; isInitialLoading: boolean }) {
  if (error) {
    return (
      <InlineNotification
        kind="error"
        title={title}
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        lowContrast
        hideCloseButton
        style={{ maxWidth: "100%" }}
      />
    );
  }
  if (isInitialLoading) {
    return (
      <div className="cg-section cg-section__body">
        <SkeletonText paragraph lineCount={4} />
      </div>
    );
  }
  return null;
}

export function ReportStats({ stats }: { stats: { label: string; value: ReactNode }[] }) {
  return (
    <div className="cg-report__stats">
      {stats.map((stat) => (
        <div key={stat.label} className="cg-report__stat">
          <p className="cg-report__stat-label">{stat.label}</p>
          <p className="cg-report__stat-value">{stat.value}</p>
        </div>
      ))}
    </div>
  );
}

export function ReportExportBar({
  count,
  noun,
  onPdf,
  onCsv,
  disabledReason,
  isExporting = false,
}: {
  count: number;
  noun: string;
  onPdf: () => void;
  onCsv: () => void;
  /** Why export is unavailable right now (invalid filters, nothing to export…); enabled when absent. */
  disabledReason?: string;
  isExporting?: boolean;
}) {
  const disabled = Boolean(disabledReason) || isExporting;
  return (
    <div className="cg-report__export">
      <p className="cg-report__export-text">
        {disabledReason ?? `Export ${count.toLocaleString()} ${noun} matching these filters.`}
      </p>
      <div className="cg-report__export-actions">
        <Button kind="tertiary" size="sm" renderIcon={DocumentPdf} disabled={disabled} onClick={onPdf}>
          Export PDF
        </Button>
        <Button kind="tertiary" size="sm" renderIcon={DocumentExport} disabled={disabled} onClick={onCsv}>
          Export CSV
        </Button>
      </div>
    </div>
  );
}

export interface ReportColumn<T> {
  header: string;
  render: (row: T) => ReactNode;
  muted?: boolean;
}

// A report table. Detail tables paginate client-side over the already-loaded
// record set (the same set the stats and export use, so they always agree);
// give it a `key` of the active filters so a filter change resets to page 1.
export function ReportTable<T>({
  title,
  columns,
  rows,
  rowKey,
  emptyText,
  pageable = true,
}: {
  title: string;
  columns: ReportColumn<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  emptyText: string;
  pageable?: boolean;
}) {
  const { pageItems, page, pageSize, total, setPage, setPageSize } = useClientPagination(rows);
  const visible = pageable ? pageItems : rows;

  return (
    <section className="cg-section cg-report__table">
      <header className="cg-section__header">
        <div>
          <p className="cg-section__title">{title}</p>
          {pageable && (
            <p className="cg-report__table-subtitle">
              Showing {visible.length.toLocaleString()} of {total.toLocaleString()} filtered records
            </p>
          )}
        </div>
      </header>
      <div style={{ overflowX: "auto" }}>
        <table className="cg-table cg-table--no-hover">
          <thead>
            <tr>
              {columns.map((c) => (
                <th key={c.header}>{c.header}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {visible.map((row) => (
              <tr key={rowKey(row)}>
                {columns.map((c) => (
                  <td key={c.header} className={c.muted ? "cg-table__muted" : undefined}>
                    {c.render(row)}
                  </td>
                ))}
              </tr>
            ))}
            {visible.length === 0 && (
              <tr>
                <td colSpan={columns.length} className="cg-table__muted">
                  {emptyText}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      {pageable && total > 0 && (
        <Pagination
          page={page}
          pageSize={pageSize}
          pageSizes={[10, 20, 50, 100]}
          totalItems={total}
          onChange={({ page: nextPage, pageSize: nextPageSize }) => {
            setPage(nextPage);
            setPageSize(nextPageSize);
          }}
        />
      )}
    </section>
  );
}
