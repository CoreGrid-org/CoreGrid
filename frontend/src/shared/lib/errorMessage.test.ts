import { describe, expect, it } from "vitest";
import { getErrorMessage } from "./errorMessage";

describe("getErrorMessage", () => {
  it("returns the fallback for a non-Error value", () => {
    expect(getErrorMessage("not an error", "fallback")).toBe("fallback");
    expect(getErrorMessage(undefined, "fallback")).toBe("fallback");
  });

  it("returns the raw message when it isn't JSON", () => {
    expect(getErrorMessage(new Error("plain text failure"), "fallback")).toBe("plain text failure");
  });

  it("extracts .message from a JSON error body", () => {
    const error = new Error(JSON.stringify({ message: "Could not save policy." }));
    expect(getErrorMessage(error, "fallback")).toBe("Could not save policy.");
  });

  it("extracts a bare .title from an ASP.NET ProblemDetails body", () => {
    const error = new Error(JSON.stringify({ title: "Not Found" }));
    expect(getErrorMessage(error, "fallback")).toBe("Not Found");
  });

  it("joins .title with flattened validation .errors", () => {
    const error = new Error(
      JSON.stringify({
        title: "Validation failed",
        errors: { Name: ["Name is required."], Code: ["Code must be unique."] },
      }),
    );
    expect(getErrorMessage(error, "fallback")).toBe("Validation failed: Name is required., Code must be unique.");
  });

  it("falls back to the raw message when JSON has neither message nor title", () => {
    const error = new Error(JSON.stringify({ somethingElse: true }));
    expect(getErrorMessage(error, "fallback")).toBe(JSON.stringify({ somethingElse: true }));
  });
});
