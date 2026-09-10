import { InlineNotification, SkeletonText } from "@carbon/react";
import { useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { getInventoryAssets } from "../api/inventoryReport";
import type { Asset } from "@/features/assets/types/asset";

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR", maximumFractionDigits: 0 }).format(value);
}

function averageAgeInYears(assets: Asset[]) {
  if (assets.length === 0) return "0.0 years";
  const today = Date.now();
  const ageInYears = assets.reduce((total, asset) => {
    const acquisitionTime = new Date(asset.acquisition_date).getTime();
    return total + Math.max(0, today - acquisitionTime) / (365.25 * 24 * 60 * 60 * 1000);
  }, 0) / assets.length;
  return `${ageInYears.toFixed(1)} years`;
}

export default function InventoryReportPanel() {
  const { getAccessToken } = useThunderID();
  const [assets, setAssets] = useState<Asset[]>([]);
  const [error, setError] = useState<unknown>();
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getInventoryAssets(token))
      .then((result) => {
        if (!cancelled) {
          setAssets(result);
          setIsLoading(false);
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setError(reason);
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [getAccessToken]);

  if (isLoading) {
    return <div className="cg-section"><SkeletonText paragraph lineCount={4} /></div>;
  }

  if (error) {
    return (
      <InlineNotification
        kind="error"
        title="Could not load the asset inventory"
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        hideCloseButton
      />
    );
  }

  const totalValue = assets.reduce((total, asset) => total + asset.acquisition_cost, 0);
  const byDepartment = Array.from(
    assets.reduce((groups, asset) => {
      const current = groups.get(asset.department_name) ?? { count: 0, value: 0 };
      groups.set(asset.department_name, {
        count: current.count + 1,
        value: current.value + asset.acquisition_cost,
      });
      return groups;
    }, new Map<string, { count: number; value: number }>()),
  ).sort(([, left], [, right]) => right.count - left.count);

  return (
    <div className="cg-section">
      <div className="cg-stat-grid" style={{ padding: "1.5rem", marginBottom: 0, gridTemplateColumns: "repeat(3, 1fr)" }}>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Assets in scope</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{assets.length.toLocaleString()}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Total acquisition value</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{formatCurrency(totalValue)}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Average age</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{averageAgeInYears(assets)}</p>
        </div>
      </div>

      <table className="cg-table cg-table--no-hover">
        <thead>
          <tr><th>Department</th><th>Assets</th><th>Total value</th></tr>
        </thead>
        <tbody>
          {byDepartment.map(([department, summary]) => (
            <tr key={department}>
              <td>{department}</td>
              <td className="cg-table__muted">{summary.count}</td>
              <td className="cg-table__muted">{formatCurrency(summary.value)}</td>
            </tr>
          ))}
          {byDepartment.length === 0 && <tr><td colSpan={3} className="cg-table__muted">No assets found.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}