import { useEffect, useMemo, useState } from "react";

// Provides client-side pagination for a complete item list.
export function useClientPagination<T>(items: T[] | undefined, defaultPageSize = 20) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(defaultPageSize);

  const total = items?.length ?? 0;

  const pageItems = useMemo(() => {
    if (!items) return [];
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [items, page, pageSize]);

// Resets to the first page when the current page becomes invalid.
  useEffect(() => {
    if (total > 0 && (page - 1) * pageSize >= total) {
      setPage(1);
    }
  }, [total, page, pageSize]);

  return { pageItems, page, pageSize, total, setPage, setPageSize };
}
