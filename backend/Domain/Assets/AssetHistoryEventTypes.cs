namespace CoreGrid.Api.Domain;

// Event types Component A's own Asset writes use. CK_AssetHistory_EventType
// (Data/CoreGridDbContext.cs) also allows VERIFICATION, MAINTENANCE, TRANSFER,
// DISPOSAL and AGENT_RECOMMENDATION for other components' writes — not listed
// here since Component A never writes those.
public static class AssetHistoryEventTypes
{
    public const string StatusChange = "STATUS_CHANGE";
    public const string FieldAmendment = "FIELD_AMENDMENT";
}
