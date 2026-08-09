namespace CoreGrid.Api.Domain;

// The four CoreGrid application roles (SRS §4.6). Values match the literal
// `roles` claim strings configured in ThunderID — see doc/setup/ThunderID.md.
public enum CoreGridRole
{
    Staff,
    InventoryOfficer,
    Auditor,
    Administrator,
}
