import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { getDashboardSummary } from "../api/dashboard";
import type { DashboardSummary } from "../api/dashboard";

export function useDashboardSummary() {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<DashboardSummary>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then(getDashboardSummary)
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
  }, [attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}
