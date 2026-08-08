import { functionGroups } from "../data/content";

export default function Capabilities() {
  return (
    <section className="section" id="capabilities">
      <div className="wrap">
        <p className="section-kicker">§05 — Platform Capabilities</p>
        <h2 className="section-title">Nine function groups, one consistent rulebook.</h2>

        <div className="fn-grid">
          {functionGroups.map((fn) => (
            <div className="fn-item" key={fn.id}>
              <span>{fn.id}</span>
              <h5>{fn.name}</h5>
              <p>{fn.body}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
