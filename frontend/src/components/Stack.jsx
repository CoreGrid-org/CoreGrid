import { stack } from "../data/content.js";

export default function Stack() {
  return (
    <section className="section section-shaded" id="stack">
      <div className="wrap">
        <p className="section-kicker">§06 — Mandated Stack</p>
        <h2 className="section-title">Built on a fixed, disclosed technology set.</h2>

        <ul className="stack-grid">
          {stack.map((item) => (
            <li key={item.name}>
              <strong>{item.name}</strong>
              <span>{item.detail}</span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
