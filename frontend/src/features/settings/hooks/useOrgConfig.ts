import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import type { Department, Location } from "@/features/assets/types/asset";
import {
  createDepartment,
  createLocation,
  createOrganizationPolicy,
  listOrganizationPolicies,
  setDepartmentActive,
  setLocationActive,
  updateDepartment,
  updateLocation,
  updateOrganizationPolicy,
} from "../api/orgConfig";
import type {
  OrganizationPolicy,
  SaveDepartmentRequest,
  SaveLocationRequest,
  SaveOrganizationPolicyRequest,
} from "../api/orgConfig";

export function useOrganizationPolicies() {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<OrganizationPolicy[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then(listOrganizationPolicies)
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

export function useCreateDepartment() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<SaveDepartmentRequest, Department>(async (payload) => {
    const accessToken = await getAccessToken();
    return createDepartment(payload, accessToken);
  });
}

export function useUpdateDepartment() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: SaveDepartmentRequest }, Department>(async ({ id, payload }) => {
    const accessToken = await getAccessToken();
    return updateDepartment(id, payload, accessToken);
  });
}

export function useSetDepartmentActive() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; isActive: boolean }, Department>(async ({ id, isActive }) => {
    const accessToken = await getAccessToken();
    return setDepartmentActive(id, isActive, accessToken);
  });
}

export function useCreateLocation() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<SaveLocationRequest, Location>(async (payload) => {
    const accessToken = await getAccessToken();
    return createLocation(payload, accessToken);
  });
}

export function useUpdateLocation() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: SaveLocationRequest }, Location>(async ({ id, payload }) => {
    const accessToken = await getAccessToken();
    return updateLocation(id, payload, accessToken);
  });
}

export function useSetLocationActive() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; isActive: boolean }, Location>(async ({ id, isActive }) => {
    const accessToken = await getAccessToken();
    return setLocationActive(id, isActive, accessToken);
  });
}

export function useCreateOrganizationPolicy() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<SaveOrganizationPolicyRequest, OrganizationPolicy>(async (payload) => {
    const accessToken = await getAccessToken();
    return createOrganizationPolicy(payload, accessToken);
  });
}

export function useUpdateOrganizationPolicy() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: SaveOrganizationPolicyRequest }, OrganizationPolicy>(
    async ({ id, payload }) => {
      const accessToken = await getAccessToken();
      return updateOrganizationPolicy(id, payload, accessToken);
    },
  );
}
