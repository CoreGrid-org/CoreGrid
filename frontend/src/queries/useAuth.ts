import { useCallback, useState } from "react";
import { forgotPassword, resetPassword } from "../services/auth";
import type { ForgotPasswordRequest, ForgotPasswordResponse, ResetPasswordRequest } from "../services/auth";

interface MutationState<TData> {
  data?: TData;
  error: unknown;
  isPending: boolean;
  isError: boolean;
}

function useStubMutation<TRequest, TData>(mutationFn: (payload: TRequest) => Promise<TData>) {
  const [state, setState] = useState<MutationState<TData>>({
    data: undefined,
    error: undefined,
    isPending: false,
    isError: false,
  });

  const mutate = useCallback(
    (payload: TRequest, options?: { onSuccess?: (data: TData) => void }) => {
      setState({ data: undefined, error: undefined, isPending: true, isError: false });
      mutationFn(payload)
        .then((data) => {
          setState({ data, error: undefined, isPending: false, isError: false });
          options?.onSuccess?.(data);
        })
        .catch((error: unknown) => {
          setState({ data: undefined, error, isPending: false, isError: true });
        });
    },
    [mutationFn]
  );

  return { ...state, mutate };
}

export function useForgotPassword() {
  return useStubMutation<ForgotPasswordRequest, ForgotPasswordResponse>(forgotPassword);
}

export function useResetPassword() {
  return useStubMutation<ResetPasswordRequest, void>(resetPassword);
}
