import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import {
  approveTransfer,
  confirmTransferReceipt,
  getTransferById,
  getTransferHistoryForAsset,
  initiateTransfer,
  listTransfers,
} from "../services/transfers";
import type {
  InitiateTransferRequest,
  PagedResult,
  TransferQueryParameters,
  TransferResponse,
} from "../types";

export function useTransfersList(params?: TransferQueryParameters) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<PagedResult<TransferResponse>>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listTransfers(params, token))
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
  }, [attempt, getAccessToken, params?.status, params?.departmentId, params?.page, params?.pageSize]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useTransferDetail(id: string | null) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<TransferResponse>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(Boolean(id));
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!id) {
      setData(undefined);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getTransferById(id, token))
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

export function useTransferHistoryForAsset(assetId: string | null) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<TransferResponse[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(Boolean(assetId));
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!assetId) {
      setData(undefined);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getTransferHistoryForAsset(assetId, token))
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
  }, [assetId, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useInitiateTransfer() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<InitiateTransferRequest, TransferResponse>(async (payload) => {
    const token = await getAccessToken();
    return initiateTransfer(payload, token);
  });
}

export function useApproveTransfer() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<string, TransferResponse>(async (id) => {
    const token = await getAccessToken();
    return approveTransfer(id, token);
  });
}

export function useConfirmTransferReceipt() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<string, TransferResponse>(async (id) => {
    const token = await getAccessToken();
    return confirmTransferReceipt(id, token);
  });
}
