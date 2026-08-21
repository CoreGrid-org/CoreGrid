import { useState, type MouseEvent } from "react";
import { Button } from "@carbon/react";

export interface LinePoint {
  label: string;
  value: number;
}

interface LineChartProps {
  data: LinePoint[];
  valueFormatter?: (value: number) => string;
  color?: string;
}

const WIDTH = 480;
const HEIGHT = 180;
const PADDING_LEFT = 44;
const PADDING_RIGHT = 12;
const PADDING_TOP = 16;
const PADDING_BOTTOM = 24;

function niceCeil(value: number): number {
  if (value <= 0) return 1;
  const exponent = Math.floor(Math.log10(value));
  const magnitude = 10 ** exponent;
  const fraction = value / magnitude;
  const niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
  return niceFraction * magnitude;
}

// Single-series trend line (dataviz skill: "trend over time" → line, one
// hue). Crosshair + tooltip snaps to the nearest point; the last point also
// carries a direct label per "lines → value at the end".
export default function LineChart({ data, valueFormatter = (v) => v.toLocaleString(), color = "#2a78d6" }: LineChartProps) {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const [showTable, setShowTable] = useState(false);

  if (data.length === 0) {
    return <p className="cg-table__muted">No data yet.</p>;
  }

  const plotWidth = WIDTH - PADDING_LEFT - PADDING_RIGHT;
  const plotHeight = HEIGHT - PADDING_TOP - PADDING_BOTTOM;
  const niceMax = niceCeil(Math.max(...data.map((d) => d.value), 1));

  const points = data.map((d, i) => ({
    ...d,
    x: PADDING_LEFT + (data.length === 1 ? 0 : (i / (data.length - 1)) * plotWidth),
    y: PADDING_TOP + plotHeight - (d.value / niceMax) * plotHeight,
  }));

  const path = points.map((p, i) => `${i === 0 ? "M" : "L"}${p.x},${p.y}`).join(" ");
  const ticks = [0, niceMax / 2, niceMax];

  const handleMove = (event: MouseEvent<SVGSVGElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const relativeX = ((event.clientX - rect.left) / rect.width) * WIDTH;
    const ratio = (relativeX - PADDING_LEFT) / plotWidth;
    const index = Math.round(ratio * (data.length - 1));
    setHoverIndex(Math.min(Math.max(index, 0), data.length - 1));
  };

  const hovered = hoverIndex !== null ? points[hoverIndex] : undefined;
  const last = points[points.length - 1];

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
              <th>Month</th>
              <th>Cost</th>
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
        <div style={{ position: "relative" }}>
          <svg
            viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
            width="100%"
            height={HEIGHT}
            role="img"
            aria-label="Line chart"
            onMouseMove={handleMove}
            onMouseLeave={() => setHoverIndex(null)}
          >
            {ticks.map((tick) => {
              const y = PADDING_TOP + plotHeight - (tick / niceMax) * plotHeight;
              return (
                <g key={tick}>
                  <line x1={PADDING_LEFT} y1={y} x2={WIDTH - PADDING_RIGHT} y2={y} stroke="#e1e0d9" strokeWidth={1} />
                  <text x={PADDING_LEFT - 8} y={y} textAnchor="end" dominantBaseline="middle" fontSize="10" fill="#898781">
                    {valueFormatter(tick)}
                  </text>
                </g>
              );
            })}

            <line
              x1={PADDING_LEFT}
              y1={PADDING_TOP + plotHeight}
              x2={WIDTH - PADDING_RIGHT}
              y2={PADDING_TOP + plotHeight}
              stroke="#c3c2b7"
              strokeWidth={1}
            />

            {points.map((p, i) => (
              <text
                key={p.label}
                x={p.x}
                y={HEIGHT - 6}
                textAnchor="middle"
                fontSize="9"
                fill="#898781"
                opacity={i % 2 === 0 || data.length <= 6 ? 1 : 0}
              >
                {p.label}
              </text>
            ))}

            {hoverIndex !== null && (
              <line
                x1={points[hoverIndex].x}
                y1={PADDING_TOP}
                x2={points[hoverIndex].x}
                y2={PADDING_TOP + plotHeight}
                stroke="#c3c2b7"
                strokeWidth={1}
              />
            )}

            <path d={path} fill="none" stroke={color} strokeWidth={2} strokeLinejoin="round" strokeLinecap="round" />

            {points.map((p, i) => (
              <circle
                key={p.label}
                cx={p.x}
                cy={p.y}
                r={hoverIndex === i ? 5 : 4}
                fill={color}
                stroke="#fcfcfb"
                strokeWidth={2}
              />
            ))}

            <text x={last.x} y={last.y - 10} textAnchor="end" fontSize="11" fontWeight={600} fill="#0b0b0b">
              {valueFormatter(last.value)}
            </text>
          </svg>

          {hovered && (
            <div
              className="cg-chart-tooltip"
              style={{
                left: `${(hovered.x / WIDTH) * 100}%`,
                top: `${(hovered.y / HEIGHT) * 100}%`,
              }}
            >
              <span className="cg-chart-tooltip__label">{hovered.label}</span>
              <strong className="cg-chart-tooltip__value">{valueFormatter(hovered.value)}</strong>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
