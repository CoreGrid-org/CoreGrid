import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import { createCampaign, listCampaigns } from "../api/campaigns";
import type { Campaign, CreateCampaignRequest } from "../api/campaigns";

export function useCampaignsList() {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<Campaign[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then(listCampaigns)
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

export function useCreateCampaign() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<CreateCampaignRequest, Campaign>(async (payload) => {
    const accessToken = await getAccessToken();
    return createCampaign(payload, accessToken);
  });
}
