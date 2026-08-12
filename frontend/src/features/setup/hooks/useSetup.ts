import { useCallback, useEffect, useState } from "react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import { getSetupStatus, completeSetup } from "../services/setup";
import type { SetupStatus, CompleteSetupRequest, CompleteSetupResponse } from "../services/setup";

export function useSetupStatus() {
  const [data, setData] = useState<SetupStatus>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getSetupStatus()
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
  }, [attempt]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useCompleteSetup() {
  return useStubMutation<CompleteSetupRequest, CompleteSetupResponse>(completeSetup);
}
