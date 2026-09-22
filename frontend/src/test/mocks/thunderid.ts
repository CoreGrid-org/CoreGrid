// Provides a shared ThunderID test double.
export const TEST_ACCESS_TOKEN = "test-access-token";

// Keeps the access token function stable across renders.
async function getAccessToken(): Promise<string> {
  return TEST_ACCESS_TOKEN;
}
async function signOut(): Promise<void> {}

const DOUBLE = {
  isSignedIn: true,
  isLoading: false,
  getAccessToken,
  signOut,
};

export function thunderIDTestDouble() {
  return DOUBLE;
}
