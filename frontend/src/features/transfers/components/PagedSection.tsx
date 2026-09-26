import type { ReactNode } from "react";
import { InlineNotification, Pagination } from "@carbon/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { PagedResult } from "../types";

const PAGE_SIZES = [10, 20, 50, 100];

interface PagedSectionProps<T> {
  result: PagedResult<T> | undefined;
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  /** Plural noun for the loading/empty/error copy, e.g. "transfers". */
  noun: string;
  onPageChange: (page: number, pageSize: number) => void;
  children: (items: T[]) => ReactNode;
}

// The load-error / loading / empty / table + pagination wrapper every
// server-paged list on the Transfers & Disposals page shares.
export default function PagedSection<T>({ result, isLoading, isError, error, noun, onPageChange, children }: PagedSectionProps<T>) {
  return (
    <>
      {isError && (
        <InlineNotification
          kind="error"
          title={`Could not load ${noun}`}
          subtitle={getErrorMessage(error, `Something went wrong loading ${noun}.`)}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div className="cg-section">
        {isLoading ? (
          <div className="cg-placeholder">
            <p>Loading {noun}…</p>
          </div>
        ) : result && result.items.length > 0 ? (
          <>
            {children(result.items)}
            <Pagination
              page={result.page}
              pageSize={result.page_size}
              pageSizes={PAGE_SIZES}
              totalItems={result.total_count}
              onChange={({ page, pageSize }) => onPageChange(page, pageSize)}
              style={{ marginTop: "1rem" }}
            />
          </>
        ) : (
          <div className="cg-placeholder">
            <p>No {noun} found.</p>
          </div>
        )}
      </div>
    </>
  );
}
