# CoreGrid Frontend

A public, informational site introducing the CoreGrid platform — architecture, user roles, the agentic AI
workflow and the mandated technology stack, drawn from the [SRS](../docs/srs/00-front-matter.md).

React + TypeScript + Vite, with client-side routing (React Router). This is a public overview site, not the
authenticated React management application described in the SRS (Section 2.2, F1–F9) — there is no sign-in,
no personal data and no asset data here.

## Pages

| Route | Content |
|---|---|
| `/` | Hero, the three platform commitments, quick links |
| `/platform` | System architecture, the nine function groups, the tech stack |
| `/roles` | The four user classes and their permissions |
| `/agentic-ai` | The four-agent, human-approved decision workflow |
| `/about` | Project scope, team roster, data/access notice |

## Develop

```bash
npm install
npm run dev
```

## Build

```bash
npm run build   # runs tsc -b, then vite build
npm run preview
```

## Report exports

The Asset Inventory report uses the `jspdf` npm package for direct PDF downloads. CSV exports are generated from
the same filtered asset data in the browser. `npm install` or `npm ci` installs `jspdf` from `package.json` and
`package-lock.json`.

## Assets

- `public/CoreGrid.png` — official logo (full colour, hexagon skyline mark).
- `public/assets/w-coregrid.webp` — pure white, transparent-background version of the logo, used on dark surfaces and in the root README.
