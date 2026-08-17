import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import {
  activateAssetAttributeDefinition,
  activateAssetCategory,
  activateAssetType,
  createAsset,
  createAssetAttributeDefinition,
  createAssetCategory,
  createAssetType,
  deleteAssetAttributeDefinition,
  deleteAssetCategory,
  deleteAssetType,
  getAsset,
  getAssetByQrCode,
  getAssetHistory,
  getAssetTypeAttributes,
  getOrganizationCode,
  listAssetCategories,
  listAssetTypes,
  listAssets,
  listDepartments,
  listLocations,
  updateAsset,
  updateAssetAttributeDefinition,
  updateAssetCategory,
  updateAssetCondition,
  updateAssetType,
} from "../api/assets";
import type {
  Asset,
  AssetAttributeDefinition,
  AssetCategory,
  AssetDetail,
  AssetHistoryEntry,
  AssetQueryParameters,
  AssetType,
  CreateAssetAttributeDefinitionRequest,
  CreateAssetCategoryRequest,
  CreateAssetRequest,
  CreateAssetTypeRequest,
  Department,
  Location,
  OrganizationCode,
  PagedResult,
  UpdateAssetAttributeDefinitionRequest,
  UpdateAssetCategoryRequest,
  UpdateAssetConditionRequest,
  UpdateAssetRequest,
  UpdateAssetTypeRequest,
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

export function useAssetHistory(assetId: string | undefined, page: number, pageSize: number) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<PagedResult<AssetHistoryEntry>>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!assetId) return;
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getAssetHistory(assetId, { page, pageSize }, token))
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
  }, [assetId, page, pageSize, attempt, getAccessToken]);

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

// The organisation's short code (e.g. "MOTAHSL") — same prefix asset codes
// use, shown for real in the asset registration form's code preview.
export function useOrganizationCode() {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<OrganizationCode>();

  useEffect(() => {
    let cancelled = false;
    getAccessToken()
      .then(getOrganizationCode)
      .then((result) => {
        if (!cancelled) setData(result);
      })
      .catch(() => {
        // Best-effort — the preview falls back to a placeholder.
      });
    return () => {
      cancelled = true;
    };
  }, [getAccessToken]);

  return { data };
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

export function useUpdateAssetCategory() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: UpdateAssetCategoryRequest }, AssetCategory>(
    async ({ id, payload }) => {
      const accessToken = await getAccessToken();
      return updateAssetCategory(id, payload, accessToken);
    },
  );
}

// Result is undefined when the category was hard-deleted (gone); otherwise
// it's the now-deactivated category (still referenced by an AssetType).
export function useDeleteAssetCategory() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<string, AssetCategory | undefined>(async (id) => {
    const accessToken = await getAccessToken();
    return deleteAssetCategory(id, accessToken);
  });
}

export function useActivateAssetCategory() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<string, AssetCategory>(async (id) => {
    const accessToken = await getAccessToken();
    return activateAssetCategory(id, accessToken);
  });
}

export function useCreateAssetType() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<CreateAssetTypeRequest, AssetType>(async (payload) => {
    const accessToken = await getAccessToken();
    return createAssetType(payload, accessToken);
  });
}

export function useUpdateAssetType() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: UpdateAssetTypeRequest }, AssetType>(
    async ({ id, payload }) => {
      const accessToken = await getAccessToken();
      return updateAssetType(id, payload, accessToken);
    },
  );
}

// Result is undefined when the type was hard-deleted (gone); otherwise it's
// the now-deactivated type (still referenced by an Asset).
export function useDeleteAssetType() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<string, AssetType | undefined>(async (id) => {
    const accessToken = await getAccessToken();
    return deleteAssetType(id, accessToken);
  });
}

export function useActivateAssetType() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<string, AssetType>(async (id) => {
    const accessToken = await getAccessToken();
    return activateAssetType(id, accessToken);
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

export function useUpdateAssetAttributeDefinition() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<
    { assetTypeId: string; attributeId: string; payload: UpdateAssetAttributeDefinitionRequest },
    AssetAttributeDefinition
  >(async ({ assetTypeId, attributeId, payload }) => {
    const accessToken = await getAccessToken();
    return updateAssetAttributeDefinition(assetTypeId, attributeId, payload, accessToken);
  });
}

// Result is undefined when the attribute was hard-deleted (gone); otherwise
// it's the now-deactivated definition (existing Assets still have values for it).
export function useDeleteAssetAttributeDefinition() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<
    { assetTypeId: string; attributeId: string },
    AssetAttributeDefinition | undefined
  >(async ({ assetTypeId, attributeId }) => {
    const accessToken = await getAccessToken();
    return deleteAssetAttributeDefinition(assetTypeId, attributeId, accessToken);
  });
}

export function useActivateAssetAttributeDefinition() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<
    { assetTypeId: string; attributeId: string },
    AssetAttributeDefinition
  >(async ({ assetTypeId, attributeId }) => {
    const accessToken = await getAccessToken();
    return activateAssetAttributeDefinition(assetTypeId, attributeId, accessToken);
  });
}

export function useUpdateAsset() {
  const { getAccessToken } = useThunderID();
  return useStubMutation<{ id: string; payload: UpdateAssetRequest }, AssetDetail>(
    async ({ id, payload }) => {
      const accessToken = await getAccessToken();
      return updateAsset(id, payload, accessToken);
    },
  );
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
