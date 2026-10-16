namespace DsbNorge.A3Forms.Models;

public enum LookupStatus
{
    Ok,
    LookupFailed,
    OrganizationNotFound,
    OrganizationDeleted // slettedato is in the past
}
