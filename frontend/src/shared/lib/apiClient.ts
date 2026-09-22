// Defines the common paginated response shape.
interface PagedLike<T> {
  items: T[];
  total_pages: number;
}

// Retrieves and combines all pages into a single list.
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
