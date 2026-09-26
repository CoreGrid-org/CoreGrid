import type { ReactNode } from "react";
import { Button, InlineNotification, Search, Toggle } from "@carbon/react";
import { Add } from "@carbon/icons-react";
import { getErrorMessage } from "@/shared/lib/errorMessage";

// The card both Departments and Locations are listed in: title and counts,
// search, a "show inactive" switch, an add button, and loading / error /
// empty states around the table.
export default function SettingsListSection({
  title,
  description,
  noun,
  activeCount,
  totalCount,
  search,
  onSearchChange,
  showInactive,
  onShowInactiveChange,
  filters,
  addLabel,
  addDisabledReason,
  onAdd,
  isLoading,
  error,
  isEmpty,
  emptyText,
  notice,
  onDismissNotice,
  children,
}: {
  title: string;
  description: string;
  noun: string;
  activeCount: number;
  totalCount: number;
  search: string;
  onSearchChange: (value: string) => void;
  showInactive: boolean;
  onShowInactiveChange: (value: boolean) => void;
  /** Extra filter controls shown next to the search. */
  filters?: ReactNode;
  addLabel: string;
  addDisabledReason?: string;
  onAdd: () => void;
  isLoading: boolean;
  error: unknown;
  /** Nothing matches the current search/filters. */
  isEmpty: boolean;
  emptyText: string;
  notice: string | null;
  onDismissNotice: () => void;
  children: ReactNode;
}) {
  return (
    <div className="cg-settings-list">
      {notice && (
        <InlineNotification kind="success" title={notice} lowContrast onClose={onDismissNotice} style={{ maxWidth: "100%" }} />
      )}

      <section className="cg-section">
        <header className="cg-section__header cg-settings-list__header">
          <div>
            <p className="cg-section__title">{title}</p>
            <p className="cg-settings-list__description">{description}</p>
          </div>
          <Button size="md" renderIcon={Add} disabled={Boolean(addDisabledReason)} onClick={onAdd} title={addDisabledReason}>
            {addLabel}
          </Button>
        </header>

        <div className="cg-settings-list__toolbar">
          <Search
            id={`${noun}-search`}
            size="md"
            labelText={`Search ${noun}s`}
            placeholder={`Search ${noun}s`}
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
          />
          {filters}
          <Toggle
            id={`${noun}-show-inactive`}
            size="sm"
            labelText="Show inactive"
            hideLabel
            labelA="Show inactive"
            labelB="Show inactive"
            toggled={showInactive}
            onToggle={onShowInactiveChange}
          />
          <p className="cg-settings-list__count">
            {activeCount.toLocaleString()} active · {totalCount.toLocaleString()} total
          </p>
        </div>

        {Boolean(error) && (
          <InlineNotification
            kind="error"
            title={`Could not load ${noun}s`}
            subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
            lowContrast
            hideCloseButton
            className="cg-panel-notification cg-panel-notification--inset"
          />
        )}

        {addDisabledReason && <p className="cg-settings-list__hint">{addDisabledReason}</p>}

        {isLoading ? (
          <div className="cg-placeholder">
            <p>Loading {noun}s…</p>
          </div>
        ) : isEmpty ? (
          <div className="cg-placeholder">
            <p>{emptyText}</p>
          </div>
        ) : (
          <div style={{ overflowX: "auto" }}>{children}</div>
        )}
      </section>
    </div>
  );
}
