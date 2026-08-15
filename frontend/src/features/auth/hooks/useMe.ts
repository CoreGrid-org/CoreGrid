import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { getMe } from "../services/me";
import type { MeResponse } from "../services/me";

export function useMe() {
  const { isSignedIn, getAccessToken } = useThunderID();
  const [data, setData] = useState<MeResponse>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!isSignedIn) {
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then(getMe)
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
  }, [attempt, isSignedIn, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}
