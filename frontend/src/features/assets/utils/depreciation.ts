export interface DepreciationPreview {
  /** Straight-line depreciation per year (cost ÷ useful life). */
  annualDepreciation: number;
  /** Fractional years since the purchase date (365.25-day years). */
  elapsedYears: number;
  /** Depreciation accumulated so far, capped at the cost. */
  accumulatedDepreciation: number;
  /** Cost minus accumulated depreciation, never below 0. */
  residualValue: number;
  /** Share of the cost already written off, 0–1. */
  depreciatedFraction: number;
}

// Client-side preview of the residual value the backend stores on save —
// mirrors StraightLineDepreciation.CalculateResidualValue
// (backend/Features/Shared/Finance/StraightLineDepreciation.cs): straight
// line to zero over the asset type's useful life, fractional years, rounded
// to 2 dp. Keep the two in sync.
export function previewDepreciation(
  acquisitionCost: number,
  acquisitionDate: string,
  usefulLifeYears: number,
  asOf: Date = new Date(),
): DepreciationPreview {
  const cost = Math.max(0, acquisitionCost);
  const round2 = (n: number) => Math.round(n * 100) / 100;

  // "YYYY-MM-DD" parses as UTC midnight, matching the backend's
  // DateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).
  const acquired = new Date(acquisitionDate);
  const elapsedYears = isNaN(acquired.getTime())
    ? 0
    : Math.max(0, (asOf.getTime() - acquired.getTime()) / (1000 * 60 * 60 * 24) / 365.25);

  if (usefulLifeYears <= 0 || cost === 0) {
    return { annualDepreciation: 0, elapsedYears, accumulatedDepreciation: 0, residualValue: round2(cost), depreciatedFraction: 0 };
  }

  const annualDepreciation = cost / usefulLifeYears;
  const accumulatedDepreciation = Math.min(cost, annualDepreciation * elapsedYears);
  const residualValue = Math.max(0, round2(cost - accumulatedDepreciation));

  return {
    annualDepreciation: round2(annualDepreciation),
    elapsedYears,
    accumulatedDepreciation: round2(accumulatedDepreciation),
    residualValue,
    depreciatedFraction: accumulatedDepreciation / cost,
  };
}

/** Today's date in the user's local timezone as "YYYY-MM-DD" (for `<input type="date" max>`). */
export function localTodayIso(now: Date = new Date()): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}
