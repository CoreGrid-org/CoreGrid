import { useCallback, useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { useStubMutation } from "@/shared/hooks/useStubMutation";
import { createUser, listUsers } from "../services/users";
import type { CoreGridUser, CreateUserRequest, CreateUserResponse } from "../services/users";

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
      .then(listUsers)
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

export function useCreateUser() {
  const { getAccessToken } = useThunderID();

  return useStubMutation<CreateUserRequest, CreateUserResponse>(async (payload) => {
    const accessToken = await getAccessToken();
    return createUser(payload, accessToken);
  });
}
