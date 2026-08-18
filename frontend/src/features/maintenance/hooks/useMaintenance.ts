import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import {
  listMaintenanceRecords,
  getMaintenanceRecord,
  reportFault,
  createMaintenance,
  approveMaintenance,
  startMaintenance,
  completeMaintenance,
  cancelMaintenance,
} from "../api/maintenance";
import type {
  MaintenanceRecord,
  MaintenanceQueryParameters,
  ReportFaultRequest,
  CreateMaintenanceRequest,
  ApproveMaintenanceRequest,
  CompleteMaintenanceRequest,
  CancelMaintenanceRequest,
} from "../types/maintenance";

export function useMaintenanceList(params: MaintenanceQueryParameters) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<MaintenanceRecord[]>([]);
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

export function useReportFault() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<ReportFaultRequest, MaintenanceRecord>(async (payload) => {
    const accessToken = await getAccessToken();
    return reportFault(payload, accessToken);
  });
}

export function useCreateMaintenance() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<CreateMaintenanceRequest, MaintenanceRecord>(async (payload) => {
    const accessToken = await getAccessToken();
    return createMaintenance(payload, accessToken);
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
