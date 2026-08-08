import { roles } from "../data/content";
import PublicNotice from "./PublicNotice";

export default function Roles() {
  return (
    <section className="section" id="roles">
      <div className="wrap">
        <p className="section-kicker">§03 — User Classes</p>
        <h1 className="section-title">Four roles, genuinely different permissions.</h1>
        <p className="section-lede">
          The separation between the officer who records physical facts and the auditor who independently
          verifies them is the control that makes the audit trail meaningful.
        </p>

        <div className="role-grid">
          {roles.map((role) => (
            <article className="role-card" key={role.index}>
              <header>
                <span className="role-index">{role.index}</span>
                <h3>{role.name}</h3>
              </header>
              <p className="role-client">{role.client}</p>
              <p>{role.blurb}</p>
              <p className="role-priv">{role.privileges}</p>
            </article>
          ))}
        </div>

        <p className="role-footnote">
          A fifth, non-human actor — the <strong>Agent Service Principal</strong> — authenticates with a
          confidential client credential, holds a narrowly scoped read-and-report permission set, and is
          explicitly denied every permission that changes business state.
        </p>

        <PublicNotice />
      </div>
    </section>
  );
}
