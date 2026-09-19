import { useEffect, useMemo, useState } from "react";

// §9 item 5 (backend refactor plan): Phase 5 made every one of these lists
// correct again by fetching every page from the server (fetchAllPages) —
// this adds the piece that was still missing, an actual Next/Previous
// control, by paginating client-side over that already-complete array.
// Preferred over switching back to server-paged fetches: the list is
// already fully loaded, so slicing it in memory needs no extra round trip
// per page click and no change to the (already correct, already tested)
// data-fetching layer.
export function useClientPagination<T>(items: T[] | undefined, defaultPageSize = 20) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(defaultPageSize);

  const total = items?.length ?? 0;

  const pageItems = useMemo(() => {
    if (!items) return [];
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [items, page, pageSize]);

  // If the underlying list shrinks (a filter changes, a row is deleted) and
  // the current page no longer exists, fall back to page 1 instead of
  // showing an empty page that looks like "no results."
  useEffect(() => {
    if (total > 0 && (page - 1) * pageSize >= total) {
      setPage(1);
    }
  }, [total, page, pageSize]);

  return { pageItems, page, pageSize, total, setPage, setPageSize };
}
