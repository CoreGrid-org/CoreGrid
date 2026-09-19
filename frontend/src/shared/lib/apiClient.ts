// Structurally matches every feature's own PagedResult<T> (assets,
// transfers, audit, workflows, notifications, ...) without importing any of
// them — the backend's paging shape (items/total_pages) is the same on the
// wire everywhere, so one helper works for all of them.
interface PagedLike<T> {
  items: T[];
  total_pages: number;
}

// Reference-data pickers (asset categories/types, departments, locations,
// organization policies, ...) need the complete list, not one page of it.
// Since Phase 3 paginated those endpoints at the database, this walks every
// page and returns the flattened result, so a picker's own code never has
// to know — or change — because the endpoint underneath it is paginated.
export async function fetchAllPages<T>(
  fetchPage: (page: number) => Promise<PagedLike<T>>,
): Promise<T[]> {
  const first = await fetchPage(1);
  if (first.total_pages <= 1) {
    return first.items;
  }

  const items = [...first.items];
  for (let page = 2; page <= first.total_pages; page++) {
    const next = await fetchPage(page);
    items.push(...next.items);
  }
  return items;
}
