import { agents, agentMay, agentMayNot } from "../data/content.js";

export default function AgenticAI() {
  return (
    <section className="section section-dark" id="agentic-ai">
      <div className="wrap">
        <p className="section-kicker section-kicker-light">§04 — Agentic Decision Support</p>
        <h2 className="section-title section-title-light">
          Four agents assemble the evidence. A human still decides.
        </h2>
        <p className="section-lede section-lede-light">
          Given everything the organisation knows about an asset, should it be repaired, replaced,
          transferred or disposed of — and does the answer comply with policy? The workflow assembles
          that evidence in a fixed sequence, validates it deterministically, and stops to ask a person
          before anything irreversible happens.
        </p>

        <div className="agent-flow" role="list">
          {agents.map((agent) => (
            <div className="agent-card" role="listitem" key={agent.node}>
              <span className="agent-node">{agent.node}</span>
              <h4>{agent.name}</h4>
              <p>{agent.body}</p>
              {agent.tools.map((tool) => (
                <code key={tool}>{tool}</code>
              ))}
            </div>
          ))}
        </div>

        <div className="hitl-strip">
          <div className="hitl-gate">
            <span className="hitl-label">Deterministic gate</span>
            <p>schema&nbsp;+&nbsp;rules&nbsp;+&nbsp;authorisation</p>
          </div>
          <div className="agent-arrow-divider" aria-hidden="true">→</div>
          <div className="hitl-pause">
            <span className="hitl-label">Human-in-the-loop</span>
            <p>
              Administrator: <strong>Approve</strong> · <strong>Reject</strong> · <strong>Revise</strong>
            </p>
          </div>
          <div className="agent-arrow-divider" aria-hidden="true">→</div>
          <div className="hitl-result">
            <span className="hitl-label">Outcome</span>
            <p>Executed, recorded &amp; audited — or a safe failure with no state change</p>
          </div>
        </div>

        <div className="never-grid">
          <div>
            <h5>The subsystem may</h5>
            <ul className="may-list">
              {agentMay.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </div>
          <div>
            <h5>The subsystem may never</h5>
            <ul className="may-not-list">
              {agentMayNot.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}
