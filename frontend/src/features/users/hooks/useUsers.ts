import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import { createUser, listUsers, setUserActive, updateUser } from "../services/users";
import type {
  CoreGridUser,
  CreateUserRequest,
  CreateUserResponse,
  PagedUsers,
  UpdateUserRequest,
  UserQueryParameters,
} from "../services/users";

// Full-roster picker — used by assignee dropdowns elsewhere (Maintenance
// approval/filters/reports). Requests a generously large page so those
// callers keep getting "everyone" without needing to know pagination
// exists; the Users & Roles admin page uses useUsersPage below instead,
// with real server-side search + pagination.
const PICKER_PAGE_SIZE = 200;

export function useUsersList() {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<CoreGridUser[]>();
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listUsers(token, { page: 1, pageSize: PICKER_PAGE_SIZE }))
      .then((result) => {
        if (!cancelled) {
          setData(result.items);
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

const EMPTY_PAGE: PagedUsers = { items: [], total_count: 0, page: 1, page_size: 20, total_pages: 0 };

// Backs the Users & Roles admin page: real server-side search + pagination.
export function useUsersPage(params: UserQueryParameters) {
  const { getAccessToken } = useThunderID();
  const [data, setData] = useState<PagedUsers>(EMPTY_PAGE);
  const [error, setError] = useState<unknown>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);

  const paramsKey = JSON.stringify(params);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => listUsers(token, params))
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
    // eslint-disable-next-line react-hooks/exhaustive-deps -- paramsKey covers params
  }, [paramsKey, attempt, getAccessToken]);

  const refetch = useCallback(() => setAttempt((n) => n + 1), []);

  return { data, error, isError: error !== undefined, isLoading, refetch };
}

export function useCreateUser() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<CreateUserRequest, CreateUserResponse>(async (payload) => {
    const accessToken = await getAccessToken();
    return createUser(payload, accessToken);
  });
}

export function useUpdateUser() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<{ id: string; payload: UpdateUserRequest }, CoreGridUser>(async ({ id, payload }) => {
    const accessToken = await getAccessToken();
    return updateUser(id, payload, accessToken);
  });
}

export function useSetUserActive() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<{ id: string; isActive: boolean }, CoreGridUser>(async ({ id, isActive }) => {
    const accessToken = await getAccessToken();
    return setUserActive(id, isActive, accessToken);
  });
}
