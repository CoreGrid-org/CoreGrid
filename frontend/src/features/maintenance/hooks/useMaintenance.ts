import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import {
  listMaintenanceRecords,
  getMaintenanceRecord,
  reportFault,
  createMaintenance,
  approveMaintenance,
  getCostSuggestion,
  startMaintenance,
  completeMaintenance,
  cancelMaintenance,
  uploadMaintenancePhoto,
} from "../api/maintenance";
import type {
  MaintenanceRecord,
  MaintenanceQueryParameters,
  PagedMaintenanceRecords,
  ReportFaultRequest,
  CreateMaintenanceRequest,
  ApproveMaintenanceRequest,
  MaintenanceCostSuggestion,
  CompleteMaintenanceRequest,
  CancelMaintenanceRequest,
} from "../types/maintenance";

const EMPTY_PAGE: PagedMaintenanceRecords = { items: [], total_count: 0, page: 1, page_size: 20, total_pages: 0 };

export function useMaintenanceList(params: MaintenanceQueryParameters) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<PagedMaintenanceRecords>(EMPTY_PAGE);
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  const paramsKey = JSON.stringify(params);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listMaintenanceRecords(params, token))
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
  }, [paramsKey, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useMaintenanceDetail(id: string | undefined) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<MaintenanceRecord>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);
    setData(undefined);

    getAccessToken()
      .then((token) => getMaintenanceRecord(id, token))
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
  }, [id, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useCostSuggestion(id: string | undefined, enabled = true) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<MaintenanceCostSuggestion>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!id || !enabled) return;
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getCostSuggestion(id, token))
      .then((result) => {
        if (!cancelled) setData(result);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [id, enabled, getAccessToken]);

  return { data, error, isError: error !== undefined, isLoading };
}

// A form submission with an optional photo. The photo is uploaded only here,
// as part of the submit, so nothing reaches storage for an abandoned form.
interface WithPhoto<T> {
  payload: T;
  photo?: File | null;
}

async function withUploadedPhoto<T extends { photo_url?: string }>(
  { payload, photo }: WithPhoto<T>,
  accessToken: string,
): Promise<T> {
  if (!photo) return payload;
  return { ...payload, photo_url: await uploadMaintenancePhoto(photo, accessToken) };
}

export function useReportFault() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<WithPhoto<ReportFaultRequest>, MaintenanceRecord>(async (request) => {
    const accessToken = await getAccessToken();
    return reportFault(await withUploadedPhoto(request, accessToken), accessToken);
  });
}

export function useCreateMaintenance() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<WithPhoto<CreateMaintenanceRequest>, MaintenanceRecord>(async (request) => {
    const accessToken = await getAccessToken();
    return createMaintenance(await withUploadedPhoto(request, accessToken), accessToken);
  });
}

export function useApproveMaintenance() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: ApproveMaintenanceRequest }, MaintenanceRecord>(
    async ({ id, payload }) => {
      const accessToken = await getAccessToken();
      return approveMaintenance(id, payload, accessToken);
    },
  );
}

export function useStartMaintenance() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<string, MaintenanceRecord>(async (id) => {
    const accessToken = await getAccessToken();
    return startMaintenance(id, accessToken);
  });
}

export function useCompleteMaintenance() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: CompleteMaintenanceRequest }, MaintenanceRecord>(
    async ({ id, payload }) => {
      const accessToken = await getAccessToken();
      return completeMaintenance(id, payload, accessToken);
    },
  );
}

export function useCancelMaintenance() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: CancelMaintenanceRequest }, MaintenanceRecord>(
    async ({ id, payload }) => {
      const accessToken = await getAccessToken();
      return cancelMaintenance(id, payload, accessToken);
    },
  );
}
