import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import MockNotice from "./MockNotice";

describe("MockNotice", () => {
  it("renders the mock-data title and the given description", () => {
    render(<MockNotice>Real records will appear here once the backend exists.</MockNotice>);

    expect(screen.getByText("Mock data: not wired to the backend yet")).toBeInTheDocument();
    expect(screen.getByText("Real records will appear here once the backend exists.")).toBeInTheDocument();
  });
});
