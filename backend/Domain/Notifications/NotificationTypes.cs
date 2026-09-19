namespace CoreGrid.Api.Domain;

// Notification.Type values (Phase 4, §6.1). Currently only Maintenance
// writes these (MaintenanceService); consolidated here so any future
// caller reuses the same identifiers instead of inventing its own.
public static class NotificationTypes
{
    public const string MaintenanceAssigned = "MAINTENANCE_ASSIGNED";
    public const string MaintenanceCompleted = "MAINTENANCE_COMPLETED";
    public const string MaintenanceCancelled = "MAINTENANCE_CANCELLED";
}
