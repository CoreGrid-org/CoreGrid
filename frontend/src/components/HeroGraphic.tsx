const COLS = 6;
const ROWS = 6;
const CELL = 56;
const GAP = 8;
const SIZE = CELL * COLS + GAP * (COLS - 1);

const filled: Record<string, string> = {
  "1,1": "var(--orange)",
  "3,2": "var(--teal)",
  "4,4": "var(--magenta)",
  "1,4": "var(--teal)",
};

export default function HeroGraphic() {
  const cells = [];
  for (let row = 0; row < ROWS; row++) {
    for (let col = 0; col < COLS; col++) {
      const key = `${col},${row}`;
      const x = col * (CELL + GAP);
      const y = row * (CELL + GAP);
      const color = filled[key];
      cells.push(
        <rect
          key={key}
          x={x}
          y={y}
          width={CELL}
          height={CELL}
          className={color ? "hero-graphic-cell hero-graphic-cell-filled" : "hero-graphic-cell"}
          style={color ? { fill: color } : undefined}
        />
      );
    }
  }

  return (
    <svg className="hero-graphic" viewBox={`0 0 ${SIZE} ${SIZE}`} role="img" aria-labelledby="heroGraphicTitle">
      <title id="heroGraphicTitle">A grid of tracked assets</title>
      {cells}
    </svg>
  );
}
