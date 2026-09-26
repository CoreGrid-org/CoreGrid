import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { listAuditLog } from "../api/auditLog";
import type { AuditLogEntry, AuditLogQuery, PagedResult } from "../api/auditLog";

// Pass `enabled: false` to hold off while the filters are invalid.
export function useAuditLog(query: AuditLogQuery, enabled = true) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<PagedResult<AuditLogEntry>>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  // eslint-disable-next-line react-hooks/exhaustive-deps -- keyed by value, see useAssetsList
  const queryKey = JSON.stringify(query);

  useEffect(() => {
    if (!enabled) return;
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listAuditLog(query, token))
      .then((result) => {
        if (!cancelled) {
          setData(result);
          setIsLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err);
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [queryKey, attempt, getAccessToken, enabled]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}
