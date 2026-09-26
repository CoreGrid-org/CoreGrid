import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import { downloadAuditReportExport, getAuditReport } from "../api/auditReport";
import type { AuditReport, AuditReportQuery } from "../api/auditReport";

// Pass `enabled: false` to hold off while the filters are invalid.
export function useAuditReport(query: AuditReportQuery, enabled = true) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<AuditReport>();
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
      .then((token) => getAuditReport(query, token))
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

export function useExportAuditReport() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ query: AuditReportQuery; format: "pdf" | "csv" }, void>(async ({ query, format }) => {
    const accessToken = await getAccessToken();
    return downloadAuditReportExport(query, format, accessToken);
  });
}
