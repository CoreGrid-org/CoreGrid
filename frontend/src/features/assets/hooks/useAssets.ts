import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import {
  createAsset,
  createAssetAttributeDefinition,
  createAssetCategory,
  createAssetType,
  getAsset,
  getAssetByQrCode,
  getAssetTypeAttributes,
  listAssetCategories,
  listAssetTypes,
  listAssets,
  listDepartments,
  listLocations,
  updateAssetCondition,
} from "../api/assets";
import type {
  Asset,
  AssetAttributeDefinition,
  AssetCategory,
  AssetDetail,
  AssetQueryParameters,
  AssetType,
  CreateAssetAttributeDefinitionRequest,
  CreateAssetCategoryRequest,
  CreateAssetRequest,
  CreateAssetTypeRequest,
  Department,
  Location,
  PagedResult,
  UpdateAssetConditionRequest,
} from "../types/asset";

export function useAssetsList(params: AssetQueryParameters) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<PagedResult<Asset>>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  // eslint-disable-next-line react-hooks/exhaustive-deps -- params is a plain
  // object rebuilt each render by the caller; stringify to keep the effect
  // keyed to its actual values instead of refetching on every render.
  const paramsKey = JSON.stringify(params);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listAssets(params, token))
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
  }, [paramsKey, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useAssetDetail(id: string | undefined) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<AssetDetail>();
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
      .then((token) => getAsset(id, token))
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

export function useAssetByQrCode() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<string, AssetDetail>(async (code) => {
    const accessToken = await getAccessToken();
    return getAssetByQrCode(code, accessToken);
  });
}

export function useAssetCategories() {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<AssetCategory[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then(listAssetCategories)
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

export function useAssetTypes() {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<AssetType[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then(listAssetTypes)
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

export function useAssetTypeAttributes(assetTypeId: string) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<AssetAttributeDefinition[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getAssetTypeAttributes(assetTypeId, token))
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
  }, [assetTypeId, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useDepartments() {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<Department[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then(listDepartments)
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

export function useLocations(departmentId: string | undefined) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<Location[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listLocations(departmentId, token))
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
  }, [departmentId, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useCreateAsset() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<CreateAssetRequest, AssetDetail>(async (payload) => {
    const accessToken = await getAccessToken();
    return createAsset(payload, accessToken);
  });
}

export function useCreateAssetCategory() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<CreateAssetCategoryRequest, AssetCategory>(async (payload) => {
    const accessToken = await getAccessToken();
    return createAssetCategory(payload, accessToken);
  });
}

export function useCreateAssetType() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<CreateAssetTypeRequest, AssetType>(async (payload) => {
    const accessToken = await getAccessToken();
    return createAssetType(payload, accessToken);
  });
}

export function useCreateAssetAttributeDefinition() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<
    { assetTypeId: string; payload: CreateAssetAttributeDefinitionRequest },
    AssetAttributeDefinition
  >(async ({ assetTypeId, payload }) => {
    const accessToken = await getAccessToken();
    return createAssetAttributeDefinition(assetTypeId, payload, accessToken);
  });
}

export function useUpdateAssetCondition() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: UpdateAssetConditionRequest }, void>(
    async ({ id, payload }) => {
      const accessToken = await getAccessToken();
      return updateAssetCondition(id, payload, accessToken);
    },
  );
}
