import { useState } from "react";
import { NavLink } from "react-router-dom";
import { navLinks } from "../data/content";

export default function Header() {
  const [open, setOpen] = useState(false);

  return (
    <header className="site-header">
      <div className="wrap header-row">
        <NavLink className="brand" to="/" onClick={() => setOpen(false)}>
          <img src="/CoreGrid.png" alt="CoreGrid" className="brand-mark" width="36" height="36" />
          <span className="brand-word">CoreGrid</span>
        </NavLink>

        <nav className={`site-nav ${open ? "is-open" : ""}`} aria-label="Primary">
          {navLinks.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.to === "/"}
              onClick={() => setOpen(false)}
              className={({ isActive }) => (isActive ? "is-active" : "")}
            >
              {link.label}
            </NavLink>
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
