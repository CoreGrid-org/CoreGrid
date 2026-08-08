import PageIntro from "../components/PageIntro";
import { inScope, outOfScope, team } from "../data/content";

export default function About() {
  return (
    <>
      <PageIntro
        kicker="§07 — About This Project"
        title="A seven-week academic build, scoped like a real system."
        lede="CoreGrid is the SE3090 (Software Engineering Frameworks) baseline project at SLIIT — a four-person team implementing a single departmental domain end to end, while proving through configuration that additional domains require no application code."
      />

      <section className="section">
        <div className="wrap">
          <p className="section-kicker">Scope</p>
          <h2 className="section-title">What&rsquo;s in the baseline release, and what isn&rsquo;t.</h2>

          <div className="scope-grid">
            <div className="scope-col">
              <h5 className="scope-heading scope-heading-in">In scope</h5>
              <ul>
                {inScope.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
            <div className="scope-col">
              <h5 className="scope-heading scope-heading-out">Out of scope for the baseline</h5>
              <ul>
                {outOfScope.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      </section>

      <section className="section section-shaded">
        <div className="wrap">
          <p className="section-kicker">Team</p>
          <h2 className="section-title">Four component owners, one submission.</h2>

          <div className="team-grid">
            {team.map((member) => (
              <div className="team-card" key={member.name}>
                <h4>{member.name}</h4>
                <p>{member.role}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="section">
        <div className="wrap">
          <p className="section-kicker">Data &amp; access</p>
          <h2 className="section-title">This site is informational only.</h2>
          <p className="section-lede" style={{ marginBottom: 0 }}>
            Nothing on this site collects, stores or displays personal data or asset records — it exists to
            explain what CoreGrid is and how it works. Sign-in, organisation data and workflow actions belong
            to the authenticated application described in the SRS, which is a separate piece of work not
            covered by this public site.
          </p>
        </div>
      </section>
    </>
  );
}
