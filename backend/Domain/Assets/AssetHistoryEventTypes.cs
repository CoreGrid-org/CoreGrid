namespace CoreGrid.Api.Domain;

// Defines the supported asset history event types.
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
