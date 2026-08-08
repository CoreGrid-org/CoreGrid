import { useState } from "react";
import { navLinks } from "../data/content.js";
import { useActiveSection } from "../hooks/useActiveSection.js";

const sectionIds = navLinks.map((link) => link.href.replace("#", ""));

export default function Header() {
  const [open, setOpen] = useState(false);
  const activeId = useActiveSection(sectionIds);

  return (
    <header className="site-header" id="top">
      <div className="wrap header-row">
        <a className="brand" href="#top" onClick={() => setOpen(false)}>
          <img src="/CoreGrid.png" alt="CoreGrid" className="brand-mark" width="36" height="36" />
          <span className="brand-word">CoreGrid</span>
        </a>

        <nav className={`site-nav ${open ? "is-open" : ""}`} aria-label="Primary">
          {navLinks.map((link) => (
            <a
              key={link.href}
              href={link.href}
              onClick={() => setOpen(false)}
              className={activeId === link.href.replace("#", "") ? "is-active" : ""}
            >
              {link.label}
            </a>
          ))}
          <a
            className="nav-cta"
            href="https://github.com/CoreGrid-org/CoreGrid/blob/main/doc/SRS/00-front-matter.md"
            target="_blank"
            rel="noreferrer"
            onClick={() => setOpen(false)}
          >
            Read the SRS →
          </a>
        </nav>

        <button
          className="nav-toggle"
          aria-expanded={open}
          aria-controls="site-nav"
          aria-label="Toggle navigation"
          onClick={() => setOpen((v) => !v)}
        >
          <span></span>
          <span></span>
          <span></span>
        </button>
      </div>
    </header>
  );
}
