export default function Architecture() {
  return (
    <section className="section section-shaded" id="architecture">
      <div className="wrap">
        <p className="section-kicker">§02 — System Architecture</p>
        <h1 className="section-title">
          One authoritative API. No client touches the data or the agents directly.
        </h1>
        <p className="section-lede">
          Both client applications talk only to the ASP.NET Core Web API, and the agentic service is
          reachable only from that API on a private network path. No client holds a database connection
          string, an AI service address, or a third-party key — which is what lets one consistent set of
          business rules and audit records apply no matter which client initiated the action.
        </p>

        <div
          className="arch-diagram"
          role="img"
          aria-label="React SPA and Flutter mobile call the ASP.NET Core Web API only. The API reaches PostgreSQL and the LangGraph agent service on a private network, and WSO2 Asgardeo and the email provider externally."
        >
          <div className="arch-tier arch-tier-clients">
            <div className="arch-node">
              <span className="arch-tag">Client</span>
              <h4>React SPA</h4>
              <p>Management &amp; control centre — admin, audit, approval</p>
            </div>
            <div className="arch-node">
              <span className="arch-tag">Client</span>
              <h4>Flutter Mobile</h4>
              <p>Field operations — scan, verify, report</p>
            </div>
          </div>

          <div className="arch-connector" aria-hidden="true">
            <span>HTTPS · REST · JWT</span>
          </div>

          <div className="arch-tier arch-tier-core">
            <div className="arch-node arch-node-primary">
              <span className="arch-tag">Sole authority</span>
              <h4>ASP.NET Core Web API</h4>
              <p>AuthN/AuthZ · validation · business rules · persistence · AI gateway · audit logging</p>
            </div>
          </div>

          <div className="arch-connector arch-connector-split" aria-hidden="true">
            <span>private network</span>
          </div>

          <div className="arch-tier arch-tier-data">
            <div className="arch-node">
              <span className="arch-tag">Store</span>
              <h4>PostgreSQL</h4>
              <p>Assets · lifecycle · workflow state (JSONB)</p>
            </div>
            <div className="arch-node">
              <span className="arch-tag">Internal only</span>
              <h4>LangGraph Agent Service</h4>
              <p>Planner · Maintenance · Budget · Policy · HITL</p>
            </div>
          </div>

          <div className="arch-connector" aria-hidden="true">
            <span>HTTPS — server-mediated only</span>
          </div>

          <div className="arch-tier arch-tier-ext">
            <div className="arch-node arch-node-ext">
              <span className="arch-tag">External</span>
              <h4>WSO2 Asgardeo</h4>
              <p>OIDC · organisations · roles · SCIM 2.0</p>
            </div>
            <div className="arch-node arch-node-ext">
              <span className="arch-tag">External</span>
              <h4>Email Provider</h4>
              <p>Transactional notifications</p>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
