export interface NavLink {
  to: string;
  label: string;
}

export interface Commitment {
  num: string;
  title: string;
  body: string;
}

export interface Role {
  index: string;
  name: string;
  client: string;
  blurb: string;
  privileges: string;
}

export interface Agent {
  node: string;
  name: string;
  body: string;
  tools: string[];
}

export interface FunctionGroup {
  id: string;
  name: string;
  body: string;
}

export interface StackItem {
  name: string;
  detail: string;
}

export interface HeroStat {
  value: string;
  label: string;
}

export interface TeamMember {
  name: string;
  role: string;
}

export const navLinks: NavLink[] = [
  { to: "/", label: "Home" },
  { to: "/platform", label: "Platform" },
  { to: "/roles", label: "Roles" },
  { to: "/agentic-ai", label: "Agentic AI" },
  { to: "/about", label: "About" },
];

export const commitments: Commitment[] = [
  {
    num: "01",
    title: "Identification is physical",
    body: "Every asset carries a QR label. A field officer scans it and sees the authoritative record, its maintenance history and the actions available to them — verification happens where the asset is, not at a desk afterwards.",
  },
  {
    num: "02",
    title: "Configurable, not hardcoded",
    body: "Asset categories, types, custom attributes, departments, locations and workflow behaviour are configuration. The same deployment serves a transport fleet, a hospital inventory or a rolling-stock register without a new build.",
  },
  {
    num: "03",
    title: "Intelligence is advisory",
    body: "A multi-agent workflow assembles evidence and explains a recommendation — it never changes business state on its own. A defined high-impact action always pauses for an authorised human.",
  },
];

export const roles: Role[] = [
  {
    index: "A",
    name: "Department Staff",
    client: "Flutter primary · React read-only",
    blurb: "Ordinary employees using assets day to day. Low frequency, minimal training.",
    privileges: "View assigned assets · report a fault with a photo · track requests raised.",
  },
  {
    index: "B",
    name: "Inventory Officer",
    client: "Flutter & React",
    blurb: "Custodians of the register — frequent, mobile users across stores and workshops.",
    privileges: "Register & amend assets · scan & verify · raise maintenance, transfer, disposal · initiate an evaluation.",
  },
  {
    index: "C",
    name: "Auditor",
    client: "React primary · Flutter field",
    blurb: "Independent reviewers confirming the register reflects physical reality. No write access.",
    privileges: "Run verification campaigns · record & classify discrepancies · export compliance reports.",
  },
  {
    index: "D",
    name: "Administrator",
    client: "React only",
    blurb: "Small, high-privilege, desk-based population holding approval authority.",
    privileges: "Configure the platform · manage users & roles · approve transfers, disposals & agent recommendations.",
  },
];

export const agents: Agent[] = [
  {
    node: "01",
    name: "Planner",
    body: "Confirms the objective is in scope and produces an ordered, typed plan.",
    tools: ["get_asset_summary"],
  },
  {
    node: "02",
    name: "Maintenance Analysis",
    body: "Quantifies repair count, cost trend and projected 12-month cost.",
    tools: ["get_maintenance_history", "compute_failure_statistics"],
  },
  {
    node: "03",
    name: "Budget Analysis",
    body: "Weighs residual value against repair and replacement cost, ranks the options.",
    tools: ["get_asset_financials", "compute_depreciation"],
  },
  {
    node: "04",
    name: "Policy Compliance",
    body: "Assembles policy facts; a deterministic rule engine — not the LLM — produces the verdict.",
    tools: ["get_organization_policies"],
  },
];

export const agentMay: string[] = [
  "Read data through allow-listed, read-only tools",
  "Produce a plan and delegate to specialised agents",
  "Recommend an action and explain the factors behind it",
  "Record a safe, explicit failure",
];

export const agentMayNot: string[] = [
  "Write, update or delete any business record",
  "Approve its own recommendation",
  "Invent or override an organisation policy",
  "Execute a high-impact action without human approval",
];

export const functionGroups: FunctionGroup[] = [
  { id: "F1", name: "Identity & access", body: "Asgardeo auth, RBAC, protected routes, session lifecycle." },
  { id: "F2", name: "Platform configuration", body: "Departments, locations, categories, types, custom attributes." },
  { id: "F3", name: "Asset registry & ID", body: "Registration, search, lifecycle status, QR generation & scan." },
  { id: "F4", name: "Maintenance", body: "Fault reporting with photo evidence, assignment, cost, completion." },
  { id: "F5", name: "Transfer", body: "Request, approval and physical confirmation between departments." },
  { id: "F6", name: "Disposal", body: "Condemnation, evidenced disposal approval, recorded outcome." },
  { id: "F7", name: "Audit & compliance", body: "Verification campaigns, discrepancies, immutable audit log." },
  { id: "F8", name: "Agentic decision support", body: "Four-agent workflow, deterministic validation, human approval." },
  { id: "F9", name: "Analytics & notification", body: "Role dashboards, exportable reports, transactional email." },
];

export const stack: StackItem[] = [
  { name: "ASP.NET Core", detail: ".NET 8 · sole public backend" },
  { name: "PostgreSQL", detail: "via EF Core migrations only" },
  { name: "React", detail: "management & control centre" },
  { name: "Flutter", detail: "field operations, Android" },
  { name: "LangGraph", detail: "Python — internal agent service" },
  { name: "WSO2 Asgardeo", detail: "OIDC · organisations · SCIM 2.0" },
];

export const heroStats: HeroStat[] = [
  { value: "05", label: "Cooperating system parts" },
  { value: "04", label: "Role-scoped user classes" },
  { value: "04", label: "Distinct autonomous agents" },
  { value: "01", label: "Mandatory human checkpoint" },
];

export const inScope: string[] = [
  "Organisation, department & location configuration",
  "Asset categories, types & custom attributes",
  "Registration, amendment & lifecycle status tracking",
  "QR generation & QR-based field identification",
  "Maintenance request, assignment & completion",
  "Transfer request, approval & physical confirmation",
  "Condemnation & evidenced disposal approval",
  "Verification campaigns & discrepancy resolution",
  "Immutable audit logging & role-based access control",
  "Four-agent lifecycle workflow with human approval",
];

export const outOfScope: string[] = [
  "Autonomous execution of high-impact actions by the AI",
  "Production-scale multi-tenancy with per-tenant billing",
  "ERP / national financial system integration",
  "Trained predictive ML models for failure forecasting",
  "Computer-vision damage assessment from photographs",
  "Offline synchronisation with conflict resolution",
];

export const team: TeamMember[] = [
  { name: "Hasitha Erandika", role: "Group Lead · Component D — Audit & Compliance, Policy Agent" },
  { name: "Jayashan Guruge", role: "Component A — Asset Registry & QR Identification, Planner Agent" },
  { name: "Seneja Ramanayaka", role: "Component B — Maintenance Management, Maintenance Analysis Agent" },
  { name: "Bhanuka Samarasinghe", role: "Component C — Transfer & Disposal, Budget Analysis Agent" },
];
