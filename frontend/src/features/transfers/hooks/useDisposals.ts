import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import {
  approveDisposal,
  condemnAsset,
  getDisposalById,
  listDisposals,
  requestDisposalRevision,
  submitDisposal,
} from "../services/disposals";
import type {
  CondemnAssetRequest,
  CondemnAssetResponse,
  DisposalQueryParameters,
  DisposalResponse,
  RequestDisposalRevisionRequest,
  SubmitDisposalRequest,
} from "../types";

export function useDisposalsList(params?: DisposalQueryParameters) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<DisposalResponse[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listDisposals(params, token))
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
  }, [attempt, getAccessToken, params?.status, params?.method]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useDisposalDetail(id: string | null) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<DisposalResponse>();
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
      .then((token) => getDisposalById(id, token))
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

export function useCondemnAsset() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<
    { assetId: string; payload: CondemnAssetRequest },
    CondemnAssetResponse
  >(async ({ assetId, payload }) => {
    const token = await getAccessToken();
    return condemnAsset(assetId, payload, token);
  });
}

export function useSubmitDisposal() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<SubmitDisposalRequest, DisposalResponse>(async (payload) => {
    const token = await getAccessToken();
    return submitDisposal(payload, token);
  });
}

export function useApproveDisposal() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<string, DisposalResponse>(async (id) => {
    const token = await getAccessToken();
    return approveDisposal(id, token);
  });
}

export function useRequestDisposalRevision() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<
    { id: string; payload: RequestDisposalRevisionRequest },
    DisposalResponse
  >(async ({ id, payload }) => {
    const token = await getAccessToken();
    return requestDisposalRevision(id, payload, token);
  });
}
