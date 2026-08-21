import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import { listDiscrepancies, resolveDiscrepancy } from "../api/discrepancies";
import type { Discrepancy, ResolveDiscrepancyRequest } from "../api/discrepancies";

export function useDiscrepanciesList(params: { campaignId?: string; onlyOpen?: boolean }) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<Discrepancy[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  // eslint-disable-next-line react-hooks/exhaustive-deps -- keyed by value, see useAssetsList
  const paramsKey = JSON.stringify(params);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listDiscrepancies(params, token))
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
  }, [paramsKey, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useResolveDiscrepancy() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: ResolveDiscrepancyRequest }, Discrepancy>(async ({ id, payload }) => {
    const accessToken = await getAccessToken();
    return resolveDiscrepancy(id, payload, accessToken);
  });
}
