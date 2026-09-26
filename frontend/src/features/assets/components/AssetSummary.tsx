import type { ReactNode } from "react";

export interface AssetSummaryItem {
  label: string;
  value: ReactNode;
}

// Compact read-only facts about the asset just picked in a form's asset
// ComboBox, so the user can confirm it's the right one before submitting.
export default function AssetSummary({ items }: { items: AssetSummaryItem[] }) {
  return (
    <dl className="cg-asset-summary">
      {items.map((item) => (
        <div key={item.label}>
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}
