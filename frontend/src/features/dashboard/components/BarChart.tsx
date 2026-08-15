import { useState } from "react";
import { Button } from "@carbon/react";

export interface BarChartDatum {
  label: string;
  value: number;
  // Per-bar colour — for an ordinal ramp (e.g. condition tiers). Omit it and
  // every bar takes the chart's single `color` instead (nominal categories,
  // where the bars are one series, not separate identities).
  color?: string;
}

interface BarChartProps {
  data: BarChartDatum[];
  color?: string;
  valueFormatter?: (value: number) => string;
}

const BAR_HEIGHT = 20;
const ROW_HEIGHT = 32;
const CHART_WIDTH = 320;
const LABEL_WIDTH = 96;
const TRACK_INSET = 8;

// Single-series horizontal bar chart (dataviz skill: nominal categorical or
// ordinal data, one colour role per bar supplied by the caller). Bars carry
// direct value labels at the tip, so every value is reachable without
// hovering — hover only adds the lift + native tooltip.
export default function BarChart({ data, color = "#2a78d6", valueFormatter = (v) => v.toLocaleString() }: BarChartProps) {
  const [hovered, setHovered] = useState<number | null>(null);
  const [showTable, setShowTable] = useState(false);

  const max = Math.max(...data.map((d) => d.value), 1);
  const plotWidth = CHART_WIDTH - LABEL_WIDTH - TRACK_INSET;
  const height = data.length * ROW_HEIGHT;

  return (
    <div>
      <div className="cg-chart__toolbar">
        <Button kind="ghost" size="sm" onClick={() => setShowTable((s) => !s)}>
          {showTable ? "View chart" : "View as table"}
        </Button>
      </div>

      {showTable ? (
        <table className="cg-table cg-table--no-hover">
          <thead>
            <tr>
              <th>Category</th>
              <th>Value</th>
            </tr>
          </thead>
          <tbody>
            {data.map((d) => (
              <tr key={d.label}>
                <td>{d.label}</td>
                <td>{valueFormatter(d.value)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <svg viewBox={`0 0 ${CHART_WIDTH} ${height}`} width="100%" height={height} role="img" aria-label="Bar chart">
          {data.map((d, i) => {
            const barWidth = Math.max((d.value / max) * plotWidth, 2);
            const y = i * ROW_HEIGHT + (ROW_HEIGHT - BAR_HEIGHT) / 2;
            const isHovered = hovered === i;

            return (
              <g
                key={d.label}
                tabIndex={0}
                onMouseEnter={() => setHovered(i)}
                onMouseLeave={() => setHovered(null)}
                onFocus={() => setHovered(i)}
                onBlur={() => setHovered(null)}
                style={{ cursor: "default" }}
              >
                <title>{`${d.label}: ${valueFormatter(d.value)}`}</title>
                <text
                  x={LABEL_WIDTH - 8}
                  y={y + BAR_HEIGHT / 2}
                  textAnchor="end"
                  dominantBaseline="middle"
                  fontSize="11"
                  fill="#52514e"
                >
                  {d.label}
                </text>
                <rect x={LABEL_WIDTH} y={y} width={plotWidth} height={BAR_HEIGHT} rx={4} fill="#f2f1ee" />
                <rect
                  x={LABEL_WIDTH}
                  y={y}
                  width={barWidth}
                  height={BAR_HEIGHT}
                  rx={4}
                  fill={d.color ?? color}
                  opacity={isHovered ? 0.82 : 1}
                />
                <text
                  x={LABEL_WIDTH + barWidth + 6}
                  y={y + BAR_HEIGHT / 2}
                  dominantBaseline="middle"
                  fontSize="11"
                  fontWeight={isHovered ? 600 : 400}
                  fill="#0b0b0b"
                >
                  {valueFormatter(d.value)}
                </text>
              </g>
            );
          })}
        </svg>
      )}
    </div>
  );
}
