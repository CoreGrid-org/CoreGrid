namespace CoreGrid.Api.Domain;

// All seven values CK_AssetHistory_EventType (Data/CoreGridDbContext.cs)
// allows, consolidated (Phase 4, §6.1) so every component that writes an
// AssetHistory row — Assets, Maintenance, Transfers, Disposals,
// Verification, Agents — shares one source of truth instead of each
// declaring its own literal.
public static class AssetHistoryEventTypes
{
    public const string StatusChange = "STATUS_CHANGE";
    public const string FieldAmendment = "FIELD_AMENDMENT";
    public const string Verification = "VERIFICATION";
    public const string Maintenance = "MAINTENANCE";
    public const string Transfer = "TRANSFER";
    public const string Disposal = "DISPOSAL";
    public const string AgentRecommendation = "AGENT_RECOMMENDATION";
}
