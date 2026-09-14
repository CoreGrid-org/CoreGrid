import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";

// Testing Library's own auto-cleanup only kicks in when `afterEach` is a
// true test-runner global; this project's vite.config.ts test block runs
// without `globals: true` (explicit imports everywhere else), so it never
// registers itself — without this, every render() in a file keeps piling
// onto the same document and later tests see leftover elements from
// earlier ones.
afterEach(cleanup);

// jsdom implements neither — several Carbon components (Tabs, OverflowMenu,
// ...) use both unconditionally and throw on mount without them.
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}
if (typeof window !== "undefined") {
  window.ResizeObserver ??= ResizeObserverStub;
  window.matchMedia ??= (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  });
}
