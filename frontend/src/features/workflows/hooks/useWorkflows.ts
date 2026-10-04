import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import {
  createWorkflow,
  decideWorkflow,
  evaluatePolicy,
  listWorkflows,
  resumeWorkflow,
  runMaintenanceAgent,
  runPolicyAgent,
} from "../api/workflows";
import type {
  AgentWorkflow,
  CreateWorkflowRequest,
  DecideWorkflowRequest,
  EvaluatePolicyRequest,
} from "../api/workflows";

// `poll` refreshes silently in the background (no loading flash) every
// `everyMs` for as long as `while(items)` holds, e.g. while any evaluation
// is still in flight.
export function useWorkflowsList(
  status?: string,
  poll?: { everyMs: number; while: (items: AgentWorkflow[]) => boolean },
) {
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

  const pollMs = poll && data && poll.while(data) ? poll.everyMs : undefined;
  useEffect(() => {
    if (!pollMs) return;
    const timer = window.setInterval(() => {
      getAccessToken()
        .then((token) => listWorkflows(status, token))
        .then(setData)
        .catch(() => undefined); // a missed poll is retried on the next tick
    }, pollMs);
    return () => window.clearInterval(timer);
  }, [pollMs, status, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useResumeWorkflow() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string }, AgentWorkflow>(async ({ id }) => {
    const accessToken = await getAccessToken();
    return resumeWorkflow(id, accessToken);
  });
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

export function useRunPolicyAgent() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string }, AgentWorkflow>(async ({ id }) => {
    const accessToken = await getAccessToken();
    return runPolicyAgent(id, accessToken);
  });
}

export function useRunMaintenanceAgent() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string }, AgentWorkflow>(async ({ id }) => {
    const accessToken = await getAccessToken();
    return runMaintenanceAgent(id, accessToken);
  });
}

export function useDecideWorkflow() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: DecideWorkflowRequest }, AgentWorkflow>(async ({ id, payload }) => {
    const accessToken = await getAccessToken();
    return decideWorkflow(id, payload, accessToken);
  });
}
