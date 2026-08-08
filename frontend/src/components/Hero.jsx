import { heroStats } from "../data/content.js";

export default function Hero() {
  return (
    <section className="hero">
      <div className="wrap hero-row">
        <div className="hero-copy">
          <p className="doc-meta">
            <span>SRS&nbsp;v1.0</span>
            <i>·</i>
            <span>Baselined&nbsp;2026‑08‑08</span>
            <i>·</i>
            <span>SE3090</span>
            <i>·</i>
            <span className="status-chip">Approved for implementation</span>
          </p>
          <h1>
            Physical assets,
            <br />
            <span className="accent-underline">digitally governed.</span>
          </h1>
          <p className="hero-lede">
            CoreGrid registers, identifies, maintains, transfers and disposes of an organisation&rsquo;s
            physical assets under one role-controlled platform — and calls on a four-agent, human-approved
            AI workflow when a lifecycle decision is too consequential to leave to a spreadsheet.
          </p>
          <div className="hero-actions">
            <a href="#architecture" className="btn btn-primary">
              <span className="btn-arrow" aria-hidden="true">→</span> Explore the architecture
            </a>
            <a href="#agentic-ai" className="btn btn-ghost">
              See the agent workflow
            </a>
          </div>
          <dl className="hero-stats">
            {heroStats.map((stat) => (
              <div key={stat.label}>
                <dt>{stat.value}</dt>
                <dd>{stat.label}</dd>
              </div>
            ))}
          </dl>
        </div>

        <div className="hero-art" aria-hidden="true">
          <div className="hero-art-frame">
            <img src="/CoreGrid.png" alt="" width="420" height="420" />
            <span className="corner corner-tl"></span>
            <span className="corner corner-tr"></span>
            <span className="corner corner-bl"></span>
            <span className="corner corner-br"></span>
            <span className="tick tick-1">01</span>
            <span className="tick tick-2">FIG.&nbsp;01</span>
          </div>
        </div>
      </div>
    </section>
  );
}
