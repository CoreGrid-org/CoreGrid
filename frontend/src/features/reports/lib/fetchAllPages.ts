interface Page<T> {
  items: T[];
  total_pages: number;
}

const PAGE_SIZE = 100;
const CONCURRENCY = 4;

// Every item of a server-paged list endpoint, for a report's stats and
// export. Fetches page 1 to learn the page count, then the rest a few at a
// time in parallel instead of one after another; order is preserved.
export async function fetchAllPages<T>(fetchPage: (page: number, pageSize: number) => Promise<Page<T>>): Promise<T[]> {
  const first = await fetchPage(1, PAGE_SIZE);
  const pages: T[][] = [first.items];

  for (let start = 2; start <= first.total_pages; start += CONCURRENCY) {
    const batch = [];
    for (let page = start; page < start + CONCURRENCY && page <= first.total_pages; page += 1) {
      batch.push(fetchPage(page, PAGE_SIZE).then((result) => result.items));
    }
    pages.push(...(await Promise.all(batch)));
  }

  return pages.flat();
}
