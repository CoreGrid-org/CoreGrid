export default function Footer() {
  return (
    <footer className="site-footer">
      <div className="wrap footer-row">
        <div className="footer-brand">
          <img src="/assets/w-coregrid.webp" alt="CoreGrid" width="28" height="31" />
          <div>
            <p className="footer-title">CoreGrid</p>
            <p className="footer-sub">Intelligent Asset Lifecycle Management Platform</p>
          </div>
        </div>

        <div className="footer-meta">
          <p>SE3090 — Software Engineering Frameworks · BSc (Hons) IT, SLIIT</p>
          <p>Hasitha Erandika (Lead) · Jayashan Guruge · Seneja Ramanayaka · Bhanuka Samarasinghe</p>
        </div>

        <div className="footer-links">
          <a href="https://github.com/CoreGrid-org/CoreGrid/blob/main/doc/SRS/00-front-matter.md" target="_blank" rel="noreferrer">
            Full SRS
          </a>
          <a href="https://github.com/CoreGrid-org/CoreGrid/blob/main/LICENSE" target="_blank" rel="noreferrer">
            MIT License
          </a>
        </div>
      </div>
      <div className="wrap footer-copy">
        <p>© 2026 CoreGrid-org. Documentation baselined 2026‑08‑08.</p>
      </div>
    </footer>
  );
}
