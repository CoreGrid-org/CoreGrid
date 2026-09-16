import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { listCampaignTasks } from "../api/verificationTasks";
import type { VerificationTask } from "../api/verificationTasks";

export function useCampaignTasks(campaignId: string | null) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<VerificationTask[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(false);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!campaignId) {
      setData(undefined);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listCampaignTasks(campaignId, token))
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
  }, [campaignId, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}
