import { afterEach, describe, expect, it, vi } from "vitest";
import { thunderIdRecoveryUrl } from "./recovery";

describe("thunderIdRecoveryUrl", () => {
  afterEach(() => vi.unstubAllEnvs());

  it("points at ThunderID's hosted recovery page for this application", () => {
    vi.stubEnv("VITE_THUNDERID_BASE_URL", "https://localhost:8090/");
    vi.stubEnv("VITE_THUNDERID_APPLICATION_ID", "app-123");
    expect(thunderIdRecoveryUrl()).toBe("https://localhost:8090/gate/recovery?applicationId=app-123");
  });

  it("is null until the application ID is configured", () => {
    vi.stubEnv("VITE_THUNDERID_BASE_URL", "https://localhost:8090");
    vi.stubEnv("VITE_THUNDERID_APPLICATION_ID", "");
    expect(thunderIdRecoveryUrl()).toBeNull();
  });
});
