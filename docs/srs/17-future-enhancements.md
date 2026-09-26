# 17. Future Enhancements

The capabilities below are deliberately excluded from the baseline. They are recorded here because the architecture was shaped to accommodate them, and because a clear statement of what comes next is part of defending what was built now.

| Horizon | Enhancement | What the current architecture already provides |
|---|---|---|
| Near | Offline field capture with deferred synchronisation. | The Flutter data layer is provider-mediated, so a local queue can be inserted beneath it without changing screens. |
| Near | Configurable lifecycle workflows per organisation. | State machines are already centralised and guarded in the application layer rather than scattered through controllers. |
| Near | Push notification alongside email. | `INotificationService` abstracts the channel; a second implementation is additive. |
| Near | Scheduled preventive maintenance generation at scale. | Asset types already carry a maintenance interval and maintenance history is complete. |
| Medium | **M1** — shared multi-tenant SaaS delivery with per-tenant plans and billing, replacing M0's one-deployment-per-customer model as the platform's next stage. | Every entity is already organisation-scoped with a global query filter (Section 4.5) that is already tenant-count-agnostic — it isolates one `Organizations` row today and would isolate many without change. ThunderID needs no rework either way: it has no organisation construct in M0 and needs none in M1 (Section 4.2). The actual M1 work is: lifting `SetupController`'s restriction to exactly one `Organizations` row (Section 4.7) in favour of self-service organisation signup, and adding per-tenant billing. |
| Medium | Additional agents — procurement recommendation, warranty analysis, fleet-level optimisation. | The graph, tool allow-list mechanism, state schema and validation gate are agent-agnostic. |
| Medium | Trained predictive failure models replacing statistical projection. | The Maintenance Analysis Agent already produces a typed artefact behind a stable contract; the computation can be replaced without affecting downstream agents. |
| Medium | Computer-vision condition assessment from captured photographs. | Photographs are already captured, compressed and associated with maintenance records. |
| Far | Enterprise resource-planning and financial-system integration. | All external access is mediated by the API behind interfaces; no client would be affected. |
| Far | Sovereign-cloud / data-residency deployment options for customers with regulatory data-location requirements. | No provider-proprietary service is used, and every component is container-ready with environment-supplied configuration. |
| Far | Delegated administration hierarchies and access-review governance. | ThunderID's organisation model supports nesting; the baseline uses none of it (Section 4.2), so this would be new configuration, not an extension of existing nesting. |
