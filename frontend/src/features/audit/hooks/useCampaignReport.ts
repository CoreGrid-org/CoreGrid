import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import { downloadCampaignReportExport, getCampaignReport } from "../api/campaignReport";
import type { CampaignReport } from "../api/campaignReport";

export function useCampaignReport(campaignId: string | undefined) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<CampaignReport>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!campaignId) return;

    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getCampaignReport(campaignId, token))
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

export function useExportCampaignReport() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ campaignId: string; format: "pdf" | "csv" }, void>(async ({ campaignId, format }) => {
    const accessToken = await getAccessToken();
    return downloadCampaignReportExport(campaignId, format, accessToken);
  });
}
