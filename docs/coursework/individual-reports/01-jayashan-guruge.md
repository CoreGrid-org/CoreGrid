# Individual Report — Jayashan Guruge (Student 1)

| Item | Detail |
|---|---|
| Student ID | ⟦ID⟧ |
| GitHub | `jguruge` (commits as `Jayashan`) |
| Primary component | **A — Asset Registry & QR Identification** (FR-016 – FR-032) |
| Agent owned | **Planner Agent** (Node 1) |
| Business-specific operation | `POST /api/assets/{id}/verify` (FR-031) · `GET /api/assets/qr/{code}` (FR-024/025) |
| Golden cases owned (Team Roster §18.7) | GC-06 (allow-list), GC-07 (prompt injection) |
| Git footprint | CoreGrid: 58 commits · coregrid-mobile: 17 commits |

## 1. Contribution Statement

I owned Component A, the asset registry that every other component depends on. This covers:
- asset categories, asset types and their ordered custom-attribute definitions;
- asset registration with unique, human-readable codes and QR labels;
- amendment with per-field history;
- condition recording and depreciation;
- search, filtering, sorting and pagination.

I built the React asset configuration and registry screens, and the Flutter asset search, detail, QR lookup, condition-update and verification screens. I integrated the Planner Agent into workflow creation and later migrated it from a standalone service into the .NET backend. I also produced Component A's verification evidence package, its React and Flutter tests, and the group's ER diagram and relational schema.

## 2. Owned Work (Team Roster §18.3)

### 2.1 Backend

| Area | Work |
|---|---|
| Controllers | `AssetCategoriesController`, `AssetTypesController` (incl. `/attributes` sub-resource), `AssetsController` |
| Endpoints | Category/type CRUD + activate/deactivate (safe soft-delete); attribute-definition CRUD; asset `GET` list (server-side search, filter, sort, paging), `GET {id}`, `POST`, `PUT`, `PATCH {id}/condition`, `GET {id}/history`, `GET qr/{code}`, `POST {id}/verify`, `GET organization-code` |
| Business rules | FR-019 attribute validation (`AttributeValidationRuleEngine`: min/max, minLength/maxLength, maxDate); FR-022 org-prefixed monotonic asset code; FR-026 one `FIELD_AMENDMENT` history entry per changed field; FR-030 server-side straight-line depreciation; FR-032 no asset delete path |

### 2.2 Database

Entities `AssetCategories`, `AssetTypes`, `AssetAttributeDefinitions`, `AssetAttributeValues`, `Assets`, `AssetHistory`. `(OrganizationId, AssetCode)` is unique; indexes back the FR-028 filters; a check constraint allows exactly one typed value column per attribute value, and a unique index allows one value per asset and definition. Migrations: `AddAssetSchema`, `AddIsActiveToAssetCategoryTypeAttribute`. I authored the group's conceptual ER diagram (`docs/coursework/diagrams/er-diagram.html`) and relational schema (`docs/coursework/diagrams/relational-schema.png`).

### 2.3 React

Asset list with searchable pickers and filters; asset detail with history timeline; register/update forms whose fields render purely from the selected type's attribute definitions (FR-020); category/type/attribute configuration with universal search; printable QR label download (FR-023); Reports → Asset Inventory tab with filters and export; user profile page.

### 2.4 Flutter

`asset_search_screen`, `asset_lookup_screen` (manual code entry), `asset_detail_screen` with attribute list and actions, `condition_update_sheet`, `asset_verification_screen` (including ad-hoc verification without a pending task), the QR scanning flow, report-fault and staff-dashboard UI, and the orange/white theme.

### 2.5 Agentic AI — Planner Agent

| Aspect | Detail |
|---|---|
| Responsibility | Decide whether an objective is in scope; produce an ordered execution plan delegating to the other three agents |
| Input | `EvaluationScope` (asset type, optional single asset, organisation) + objective text |
| Output | `PlannerExecutionPlan { inScope, steps[], rejectionReason? }` |
| Tool permissions | `get_asset_type_summary` through `IPlannerTools` only (read-only; organisation taken from persisted state) |
| Validation | `PlannerScopeGuard.RejectionReason` (forbidden phrases, required lifecycle terms) before any model call; `ValidatePlan` rejects out-of-scope plans that contain steps, and plans delegating to agents outside {Maintenance, Budget, Policy, DeterministicGate} |
| Failure handling | Non-2xx (incl. 429), timeout or invalid JSON → retry on the optional fallback provider (Groq `gpt-oss-120b`) → deterministic fallback plan; each step logged with the model name |
| Model | Gemini via OpenAI-compatible endpoint (`LlmSettings`, overridable per agent), optional Groq fallback (`LlmFallback`), both through the shared `ILlmClient` |

### 2.6 Tests

| Suite | Tests authored |
|---|---|
| React | `AssetComponents.test.tsx` (10), `AssetModals.test.tsx` (8, incl. form validation), `format.test.ts` (7), `qrcode.test.ts` (2) |
| Flutter | `scan_asset_screen_test` (7: camera success, permission refusal, unknown code, offline recovery, manual-entry fallback), `asset_detail_screen_test` (8), `asset_condition_test`, `asset_lookup_screen_test` (incl. non-leaking cross-org message, offline state), `asset_search_screen_test`, `asset_verification_test`, `fault_detail_screen_test`, `report_fault_screen_test` |
| Backend | ⟦Component A is covered by `AssetServiceTests`; list any backend tests you add for FR-019 or `GET qr/{code}` AC1–AC4⟧ |
| Evidence | [`evidence/component-a-test-results.md`](../evidence/component-a-test-results.md): React and Flutter test output, scanner tests, `flutter analyze`, Pixel 6 emulator walkthroughs |

Run of 2026-10-04: backend 452/452, React 115/115, Flutter 69/69.

### 2.7 Git evidence

Branch prefixes `feature/asset-*`, `feat/component-a-*`. PRs: CoreGrid #16, #17, #18, #21, #23, #25, #26, #28, #30, #31, #35, #36, #39 · mobile #2, #3, #4, #6, #7, #11. ⟦Add links and reviewers.⟧

### 2.8 Documentation

README Component A section (scan/lookup/verification, ADR-006 rationale), ADR-006 input on attribute-value storage, the Component A test-results document, the ER diagram and relational schema, and the Appendix E AI log.

## 3. Key Commits

| Commit | Date | Description |
|---|---|---|
| `d15d89b` | 2026-08-15 | Backend and frontend for Component A |
| `f14e6b9`, `5b39b92` | 2026-08-16 | Asset update; QR payload |
| `156b292`, `31ecaa4` | 2026-08-17 | Safe soft-delete; asset history timeline |
| `bc168d7`, `699b96b` | 2026-09-10 | Asset inventory report and exports |
| `6812cc5`, `fb94a30` | 2026-09-14/15 | Planner integration; migration to .NET |
| `426c9eb` | 2026-09-15 | FR-019 attribute validation |
| `ae49c1a`, `4ad9977` | 2026-09-17 | Printable QR labels; server-side depreciation |
| `7f258ee` | 2026-09-24 | Asset filters, Carbon dropdowns, category modal |
| `363687d` | 2026-09-29 | SRS/ADR updates and asset test files |
| `36e7ebf` | 2026-10-03 | Relational schema and ER diagram |
| mobile `ee8003f` | 2026-09-09 | FR-020/028/029/031 on mobile |
| mobile `0bd5ae8`, `878282c` | 2026-09-16 | Verification workflow; QR scanning |
| mobile `a607e9c` | 2026-09-25 | Report fault on mobile |
| mobile `3636c5e` | 2026-09-29 | QR scan test coverage |

## 4. Challenges and Learning

⟦Write in your own words. Things from your history you could discuss:
- The Planner began as a separate Python/FastAPI service: the service-token 401s, the external 429/502 errors, and why it moved in-process to C#.
- Designing attribute validation so the server, not the form, is the source of truth.
- Reaching a local HTTPS API from a physical device or emulator (ADB, certificates).⟧

## 5. Individual AI Usage Log

| Date / period | Tool and model | Task and section | What the tool produced | What was changed or rejected | How it was verified |
|---|---|---|---|---|---|
| 2026-08-15 – 08-31 | ChatGPT GPT-5.6 Luna; Claude Opus 5 | Backend foundation, DI, EF Core relationships/migrations, asset services, validation, authN/authZ, ThunderID, asset history | Explanations and suggested code | Adapted to CoreGrid's architecture and ThunderID environment; rejected generic fixes | `dotnet build`/`run`, migrations, DB checks, authenticated API requests |
| 2026-09-01 – 09-10 | ChatGPT GPT-5.6 Luna; Claude Opus 5 | Verification API, Planner integration, service auth, error handling, R2 | Guidance on validation, Planner communication, service tokens, HTTP errors, S3 config | Adjusted to the actual endpoint and credentials; external quota errors not treated as code defects | API requests, logs, storage tests |
| 2026-09-10 – 09-18 | ChatGPT GPT-5.6 Luna; Claude Opus 5 | QR flows, Flutter setup, mobile API/auth, Planner tools, backend tests | QR, Flutter, auth and agent-tool guidance | Adapted to CoreGrid routes and device environment | `flutter run`, QR scans, `dotnet build`, tests |
| 2026-09-16 – 09-25 | ChatGPT GPT-5.6 Luna; Claude Opus 5 | React asset pages, routing, role UI, mobile screens, ADB networking, CI, docs | Components, API integration, Flutter screens | Modified to match React/Riverpod conventions and real API responses | Browser tests, frontend tests, device tests, CI |
| 2026-09-26 | ChatGPT GPT-5.6 Luna | Mobile local HTTPS, backend config | Certificate and config guidance | Development-only configuration; fixed confirmed issues only | Local connectivity, logs |
| 2026-09-29 – 10-03 | ⟦tool/model⟧ | Asset React/Flutter tests, test-results document, ER diagram, relational schema | ⟦⟧ | ⟦⟧ | ⟦⟧ |

The full date-by-date log is in [`ai-usage-disclosure.md`](../ai-usage-disclosure.md).

## 6. AI Reflection (≈ 1 page — must be written by the student)

> A reflection that is AI-generated, or that does not match your Git history and AI log, receives no credit.

**Which AI tools were used, and at which stages?** ⟦⟧

**What did they do well, and what did they get wrong?** ⟦⟧

**What did you change, add or reject, and why?** ⟦⟧

**What did you learn about your own skills and understanding?** ⟦⟧

## 7. Declaration

I confirm that this section describes my own contribution, that all AI use is disclosed above, and that I can explain, test, modify and debug the work submitted under my name. I will not use any external AI tool during the demonstration or viva.

| Name | Student ID | Signature | Date |
|---|---|---|---|
| Jayashan Guruge | ⟦⟧ | | |
