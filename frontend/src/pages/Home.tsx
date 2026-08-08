import { Link } from "react-router-dom";
import Hero from "../components/Hero";
import { commitments } from "../data/content";

const explore = [
  {
    to: "/platform",
    kicker: "§02",
    title: "Platform & Architecture",
    body: "The five cooperating parts, the nine function groups and the mandated technology stack.",
  },
  {
    to: "/roles",
    kicker: "§03",
    title: "User Classes",
    body: "Four roles with genuinely different permissions, and how the audit trail stays meaningful.",
  },
  {
    to: "/agentic-ai",
    kicker: "§04",
    title: "Agentic Decision Support",
    body: "The four-agent, human-approved workflow behind every repair, transfer or disposal call.",
  },
];

export default function Home() {
  return (
    <>
      <Hero />

      <section className="section" id="overview">
        <div className="wrap">
          <p className="section-kicker">§01 — Why CoreGrid</p>
          <h2 className="section-title">Registers don&rsquo;t reconcile with reality on their own.</h2>
          <p className="section-lede">
            Institutions holding large populations of vehicles, machinery, medical devices and equipment
            typically track them in disconnected spreadsheets. CoreGrid replaces that fragmented process
            with three deliberate commitments.
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

      <section className="section section-shaded">
        <div className="wrap">
          <p className="section-kicker">Explore</p>
          <h2 className="section-title">Three ways into the system.</h2>

          <div className="explore-grid">
            {explore.map((item) => (
              <Link className="explore-card" to={item.to} key={item.to}>
                <span className="explore-kicker">{item.kicker}</span>
                <h3>{item.title}</h3>
                <p>{item.body}</p>
                <span className="explore-arrow" aria-hidden="true">→</span>
              </Link>
            ))}
          </div>
        </div>
      </section>
    </>
  );
}
