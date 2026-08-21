# Appendix D — Architecture Decision Record Index

Each decision is documented on a single page recording context, options considered, the decision taken and its consequences. The ADR set is the primary written evidence for learning outcome LO4 and is referenced during the viva.

| ADR | Decision | Principal alternatives considered |
|---|---|---|
| ADR-001 | Layered architecture with a single authoritative ASP.NET Core API as the only public backend. | Direct client-to-database access; a backend-for-frontend per client; microservices per component. |
| ADR-002 | Delegate authentication and user directory to ThunderID; retain authorisation in the API. | ASP.NET Core Identity with local credential storage; a custom JWT issuer; a different hosted identity provider. |
| ADR-003 | React state management: TanStack Query for server state, Zustand for client state. | Redux Toolkit throughout; Context API only; server state held in component state. |
| ADR-004 | Flutter state management: Riverpod. | BLoC; Provider; setState with a service locator. |
| ADR-005 | LangGraph as the agentic framework and orchestration method. | Microsoft Agent Framework; a custom orchestrator in C#; a linear prompt chain. |
| ADR-006 | Attribute-value tables for custom asset attributes; JSONB for agent workflow state. | JSONB for both; typed columns per domain; a document database alongside PostgreSQL. |
| ADR-007 | Deployment platform and container strategy for the five components. | Alternative no-cost hosting platforms; a single combined deployment; local-only execution. |
| ADR-008 | IBM Carbon Design System as the React client's component library and visual language. | A custom component library; Material UI; Tailwind CSS with hand-built components; Ant Design. |
| ADR-009 | Cloudflare R2 (S3-compatible) as the default photographic-evidence object store for CoreGrid-operated editions, behind an `IBlobStorageService` abstraction (Section 11.3). | AWS S3; Azure Blob Storage; storing the object in PostgreSQL as a `bytea` column; mandating a single fixed provider with no self-hosted alternative. |
