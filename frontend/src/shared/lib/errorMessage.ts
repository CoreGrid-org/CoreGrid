export function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof Error && error.message) {
    try {
      const parsed = JSON.parse(error.message);
      if (parsed && typeof parsed === "object") {
        if (parsed.message) {
          return parsed.message;
        }
        if (parsed.title) {
          if (parsed.errors && typeof parsed.errors === "object") {
            const list = Object.values(parsed.errors)
              .flat()
              .filter(Boolean)
              .join(", ");
            return list ? `${parsed.title}: ${list}` : parsed.title;
          }
          return parsed.title;
        }
      }
    } catch {
      // Ignored: error.message is not a JSON string, fallback to returning it raw
    }
    return error.message;
  }
  return fallback;
}
