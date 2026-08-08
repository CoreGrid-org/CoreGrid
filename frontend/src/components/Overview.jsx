import { commitments } from "../data/content.js";

export default function Overview() {
  return (
    <section className="section" id="overview">
      <div className="wrap">
        <p className="section-kicker">§01 — Why CoreGrid</p>
        <h2 className="section-title">Registers don&rsquo;t reconcile with reality on their own.</h2>
        <p className="section-lede">
          Institutions holding large populations of vehicles, machinery, medical devices and equipment
          typically track them in disconnected spreadsheets. Verification is manual and infrequent,
          condition data is stale by the time it reaches a decision-maker, and the call to repair, transfer
          or dispose of an asset is made without a consistent comparison of residual value against
          projected cost. CoreGrid replaces that fragmented process with three deliberate commitments.
        </p>

        <ol className="clause-grid">
          {commitments.map((item) => (
            <li className="clause-card" key={item.num}>
              <span className="clause-num">{item.num}</span>
              <h3>{item.title}</h3>
              <p>{item.body}</p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
