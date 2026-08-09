import { useStubMutation } from "./useStubMutation";
import { forgotPassword, resetPassword } from "../services/auth";
import type { ForgotPasswordRequest, ForgotPasswordResponse, ResetPasswordRequest } from "../services/auth";

export function useForgotPassword() {
  return useStubMutation<ForgotPasswordRequest, ForgotPasswordResponse>(forgotPassword);
}

export function useResetPassword() {
  return useStubMutation<ResetPasswordRequest, void>(resetPassword);
}
