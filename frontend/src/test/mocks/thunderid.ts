// Shared test double for @thunderid/react's useThunderID(), so every test
// that renders a data-fetching hook doesn't hand-roll its own. vi.mock()
// calls are hoisted per-file (Vitest can't hoist across files), so each
// test still needs its own `vi.mock("@thunderid/react", () => ({...}))` —
// but the shape of the double itself lives here once, not copy-pasted:
//
//   vi.mock("@thunderid/react", () => ({
//     useThunderID: () => thunderIDTestDouble(),
//   }));
export const TEST_ACCESS_TOKEN = "test-access-token";

// Every data-fetching hook in this app depends on `getAccessToken` inside a
// useEffect array. The real ThunderID provider hands out a stable function
// reference across renders; if this double recreated one on every call
// instead, that dependency would change on every render and the effect
// would loop forever, re-fetching and cancelling itself before any fetch
// ever gets to commit its result. Module-level singletons keep it stable.
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
