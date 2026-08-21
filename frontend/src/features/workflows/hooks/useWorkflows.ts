import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import { createWorkflow, decideWorkflow, evaluatePolicy, listWorkflows } from "../api/workflows";
import type {
  AgentWorkflow,
  CreateWorkflowRequest,
  DecideWorkflowRequest,
  EvaluatePolicyRequest,
} from "../api/workflows";

export function useWorkflowsList(status?: string) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<AgentWorkflow[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listWorkflows(status, token))
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
  }, [status, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useCreateWorkflow() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<CreateWorkflowRequest, AgentWorkflow>(async (payload) => {
    const accessToken = await getAccessToken();
    return createWorkflow(payload, accessToken);
  });
}

export function useEvaluatePolicy() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: EvaluatePolicyRequest }, AgentWorkflow>(async ({ id, payload }) => {
    const accessToken = await getAccessToken();
    return evaluatePolicy(id, payload, accessToken);
  });
}

export function useDecideWorkflow() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: DecideWorkflowRequest }, AgentWorkflow>(async ({ id, payload }) => {
    const accessToken = await getAccessToken();
    return decideWorkflow(id, payload, accessToken);
  });
}
