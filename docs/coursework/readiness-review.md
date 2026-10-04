# SE3090 Assignment 1 — Readiness Review (2026-10-04, updated after pulling `development` @ `36e7ebf`)

Internal checklist. **Do not include this file in the submitted PDF.**

**Verdict: NOT READY.** The software is strong: everything builds and every test passes. The submission package and viva evidence are not ready.

## What was verified by running it

| Check | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test backend.Tests` (with PostgreSQL on :5433) | 437 / 437 pass |
| `npm test` / `npm run build` (frontend) | 113 / 113 pass (23 files); build OK |
| `flutter analyze` / `flutter test` (mobile) | 0 issues; 69 / 69 pass (18 files) |

## Blockers — must fix before submission or evaluation

| # | Gap | Spec ref | Fix |
|---|---|---|---|
| B1 | **Deployment not live yet.** Platforms are chosen (API + PostgreSQL on Azure, React on Vercel, `coregrid-web` on GitHub Pages); links still need to be added. | §14, §15 | Deploy; paste URLs into group report "Submission links" and §12 |
| B2 | **Swagger is only mapped in Development** (`Program.cs:177`). The Azure API will have no `/swagger`. | §5, §14 | Map Swagger outside Development too (code change; not done) |
| B3 | **No current release APK.** `app-release.apk` is from 2026-08-18. | §14, §15 | `flutter build apk --release --dart-define=API_BASE_URL=<Azure>/api --dart-define=THUNDERID_CLIENT_ID=…` |
| B4 | ~~ADR-003 wrong~~ **Fixed:** rewritten to match the code (custom hooks + ThunderID context); SRS §3 and Appendix D updated. | | |
| B5 | ~~ADR-004 missing~~ **Fixed:** full Riverpod ADR added. ADR-011 stays platform-neutral; the Azure/Vercel/GitHub Pages rationale is in group report §12.1. | | |
| B6 | **Performance test not yet run.** Scripts now exist (`scripts/perf/`, `make perf`): seed, k6 50-VU load test, agent latency, slow queries; they produce the §11 table. | §12 | Get an Administrator token, run `make perf` against the deployed or local API, paste `results/<ts>/report.md` into group report §11 |
| B7 | **No recorded end-to-end / golden-case run.** Agent evaluation report has no PASS evidence. | §9, §12 | Record the §7.7 flow (Flutter → API → agents → React approve → Flutter status) with DB rows; fill GC-01…GC-12 |
| B8 | **Missing identity data:** group number, student IDs, names inconsistent (Seneja *Ramanayaka* vs *Thehansi*; *Nipuna Bhanuka* vs *Bhanuka Samarasinghe*). | §15 | Fix in all docs |
| B9 | **Reflections not written.** They must be the students' own words. | §18.3 | Each member writes ~1 page |
| B10 | **`main` is behind `development`** (5 commits on CoreGrid, 1 on mobile). CI is required on `main`. | §13 | Merge via PR; confirm green CI on `main`; save run links |
| B11 | **Demo video, test accounts, private-browser link check.** | §15 | Record a 10-min video; create 4 role accounts |

> The due date in the spec was **30 Sep 2026, 11:50 PM**. If that has passed and nothing was submitted, talk to the lecturer-in-charge first.

## Agentic AI — weak points an examiner will probe

| Gap | Risk | Suggested fix |
|---|---|---|
| Approval does **not execute** the approved action (`AgentWorkflowService.cs:515–527`, "stubbed") | Spec requires "an auditable result"; examiners may mark orchestration down | Either wire APPROVE → create/approve the disposal through `DisposalService`, or demo P6 gating explicitly and own the design in the ADR |
| Tool allow-list is **structural**, not enforced at runtime (each agent just calls specific methods) | "Controlled tool permissions" questions | Add a per-agent allow-list check in `IAgentToolsService` (agent name → permitted tools) that throws and logs on violation, plus a test (GC-06) |
| No explicit retry count on tool/model calls; 60 s HTTP timeout only | Spec lists "timeouts, retry limits" | Add a bounded retry (e.g. 2) with a test |
| Prompt-injection guard is phrase-based ("approve disposal" blocked, "approve" alone is not) | Easy live demo to break | Add more injection golden cases; delimit objective text in the prompt |
| Planner logs the raw model error body (`PlannerAgentService.cs:120`) | Minor secret/PII hygiene | Truncate or omit |

## Other gaps

- **No seed data.** The spec requires "suitable seed data". Add an idempotent demo seeder (org, departments, ~500 assets, maintenance history) — this also feeds the performance test.
- **Email/SMS notifications (FR-077–079)** not built; in-app only. This is fine for the spec but should be stated.
- **Mobile FR-049 condemnation** is missing from the mobile tree. It is listed under Nipuna's PRs but was never merged.
- **Mobile registration:** the spec lists "registration" for Flutter. Users are admin-invited (ThunderID); justify this in the report.
- **SQL export missing** for `AddMaintenanceReporter` (`backend/db/migrations/0016_*`); that migration also has no `.Designer.cs`.
- **Rate limiting:** only 3 routes use `[EnableRateLimiting]` (setup, photo upload, workflow initiation); report exports are not limited, although `docs/coursework/progress.md` says they are. The `Program.cs:216` comment saying policies are "not yet attached" is stale.
- ~~Test counts in `docs/coursework/progress.md` / `docs/mobile/progress.md`~~ updated to 113 / 69.
- `diagrams/relational-schema.png`: the `VerificationCampaigns` header is garbled ("VerificaanceRecoigns"), and `AgentWorkflows.BudgetAnalysis` is missing. Regenerate before the PDF.
- `diagrams/er-diagram.html` loads an icon font from a CDN; export it to PNG for the PDF.
- ~~README minimal~~ rewritten (open-source, single-tenant, setup, configuration, components A–D). Live URLs and test accounts stay out of the repository; they go in the PDF.

## Individual-mark risks (Git evidence)

| Member | Concern | Action |
|---|---|---|
| Jayashan | ~~No React tests~~ now has 4 React test files + scanner tests; still no backend test class of his own | Optional: a backend test for FR-019 / QR lookup |
| Seneja | No backend or React test files authored (Flutter 2 files only); last main-repo commit 2026-09-17 | Author `MaintenanceService` rule tests and a React maintenance test; rehearse BR1–BR3 |
| Nipuna | 1 mobile commit, no Flutter tests; mobile FR-049 missing | Add transfer widget tests; implement or drop mobile FR-049 |
| Hasitha | ~60 % of all insertions, including first versions of teammates' test classes (`AssetServiceTests`, `MaintenanceServiceTests`) and many integration merges | Make sure each owner can explain their component's code, even where you wrote the first version; ownership is checked in the viva |

Each member should rehearse: changing one validation rule live, explaining one migration, tracing their agent's input → tool → output → persisted step, and explaining one test.
