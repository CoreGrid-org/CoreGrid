# Appendix B — Route-Level Authorisation Map

The permission matrix in Section 4.6 states who may do what. This appendix maps those permissions onto the ASP.NET Core policy names that guard each route, so that a reviewer can verify enforcement by reading attributes rather than by tracing logic.

| Policy name | Satisfied by | Additional resource condition |
|---|---|---|
| CanReadAssets | Staff, Officer, Auditor, Administrator, Agent principal | Staff are restricted to their own department by a service-layer filter. |
| CanManageAssets | Officer, Administrator | Asset must not be in a terminal state. |
| CanVerifyAssets | Officer, Auditor | — |
| CanRequestMaintenance | Staff, Officer, Administrator | — |
| CanManageMaintenance | Officer, Administrator | Transition must be legal for the current status. |
| CanRequestTransfer | Officer, Administrator | Asset must be ACTIVE. |
| CanApproveTransfer | Administrator | Approver must not be the requester. |
| CanConfirmReceipt | Officer | Caller must belong to the destination department. |
| CanRequestDisposal | Officer, Administrator | Asset must be CONDEMNED. |
| CanApproveDisposal | Administrator | Approver must not be the requester; preconditions P1–P6 must hold. |
| CanManageCampaigns | Auditor, Administrator | — |
| CanResolveDiscrepancy | Auditor, Administrator | Discrepancy must be OPEN or UNDER_REVIEW. |
| CanReadAuditLog | Auditor, Administrator | Read-only; no mutating verb is routed to this resource. |
| CanManageConfiguration | Administrator | — |
| CanManageUsers | Administrator | Cannot deactivate own account. |
| CanInitiateWorkflow | Officer, Administrator | No workflow already running for the asset. |
| CanApproveWorkflow | Administrator | Workflow must be AWAITING_APPROVAL. |
| AgentToolAccess | Agent service principal only | Read-only tool routes; every write policy explicitly denies this principal. |
