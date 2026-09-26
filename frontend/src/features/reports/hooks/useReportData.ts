import { useEffect, useRef, useState } from "react";
import { useThunderID } from "@thunderid/react";

interface ReportState<T> {
  key: string | null;
  data: T[] | undefined;
  error: unknown;
}

// Loads a report's full record set whenever `key` (a serialisation of the
// active filters) changes. The previous result stays on screen while the
// next one loads, so the filters and table don't flash away on every
// change. Pass `enabled: false` (e.g. an invalid date range) to hold off.
export function useReportData<T>(key: string, enabled: boolean, load: (accessToken: string) => Promise<T[]>) {
  const { getAccessToken } = useThunderID();
  const [state, setState] = useState<ReportState<T>>({ key: null, data: undefined, error: undefined });
  const loadRef = useRef(load);

  useEffect(() => {
    loadRef.current = load;
  });

  useEffect(() => {
    if (!enabled) return;
    let cancelled = false;

    getAccessToken()
      .then((token) => loadRef.current(token))
      .then((data) => {
        if (!cancelled) setState({ key, data, error: undefined });
      })
      .catch((error: unknown) => {
        if (!cancelled) setState((prev) => ({ key, data: prev.data, error }));
      });

    return () => {
      cancelled = true;
    };
  }, [key, enabled, getAccessToken]);

  const isCurrent = state.key === key;
  return {
    data: state.data,
    error: isCurrent ? state.error : undefined,
    /** No result yet at all — show a skeleton. */
    isInitialLoading: enabled && state.data === undefined && !(isCurrent && state.error),
    /** A newer filter set is loading over the previous result. */
    isRefreshing: enabled && !isCurrent && state.data !== undefined,
  };
}
