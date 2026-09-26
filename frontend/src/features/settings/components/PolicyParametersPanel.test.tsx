import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { OrganizationPolicy } from "../api/orgConfig";

const values = {
  repair_to_replace_cost_threshold: 0.65,
  minimum_service_life_years: 5,
  max_acceptable_failure_frequency: 3,
  valuation_validity_window_days: 90,
  confidence_floor: 0.7,
  cost_variance_tolerance_percent: 15,
  outstanding_transfer_days: 7,
  approval_overdue_period_hours: 48,
};
const POLICIES: OrganizationPolicy[] = [
  { id: "p-default", asset_type_id: null, asset_type_name: null, ...values },
  { id: "p-laptop", asset_type_id: "t-laptop", asset_type_name: "Laptop", ...values, confidence_floor: 0.8 },
  { id: "p-vehicle", asset_type_id: "t-vehicle", asset_type_name: "Vehicle", ...values },
];

vi.mock("../hooks/useOrgConfig", () => ({
  useOrganizationPolicies: () => ({ data: POLICIES, isLoading: false, isError: false, error: undefined, refetch: () => {} }),
  useUpdateOrganizationPolicy: () => ({ mutate: () => {}, reset: () => {}, isPending: false, isError: false, error: undefined }),
}));
vi.mock("./PolicyModal", () => ({ default: () => null }));

import PolicyParametersPanel from "./PolicyParametersPanel";

describe("PolicyParametersPanel search", () => {
  it("filters to policies whose asset type matches, showing all their parameters", async () => {
    const user = userEvent.setup();
    render(<PolicyParametersPanel />);

    await user.type(screen.getByRole("searchbox", { name: "Search policies" }), "lap");

    expect(screen.getByText("Laptop")).toBeInTheDocument();
    expect(screen.queryByText("Vehicle")).not.toBeInTheDocument();
    expect(screen.queryByText("Organisation-wide default")).not.toBeInTheDocument();
    expect(screen.getByText("Minimum service life before disposal")).toBeInTheDocument();
    expect(screen.getByText("1 of 3 policies match")).toBeInTheDocument();
  });

  it("filters to matching parameters across every policy", async () => {
    const user = userEvent.setup();
    render(<PolicyParametersPanel />);

    await user.type(screen.getByRole("searchbox", { name: "Search policies" }), "confidence");

    // All three policies open, each showing only the confidence parameter.
    expect(screen.getAllByText("AI confidence floor")).toHaveLength(3);
    expect(screen.queryByText("Minimum service life before disposal")).not.toBeInTheDocument();
  });

  it("says so when nothing matches", async () => {
    const user = userEvent.setup();
    render(<PolicyParametersPanel />);

    await user.type(screen.getByRole("searchbox", { name: "Search policies" }), "zzz");

    expect(screen.getByText('No policies or parameters match "zzz".')).toBeInTheDocument();
  });
});
