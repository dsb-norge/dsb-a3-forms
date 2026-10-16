using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Clients.Brreg;

public interface IBrregClient
{
    /// <summary>
    /// Returns the organization, whether it is a main entity (hovedenhet) or a sub entity (underenhet).
    /// Main entities include their sub entities, sub entities include the org form of their main entity.
    /// Never returns null; check <see cref="BrregOrganization.LookupStatus"/> for not found, deleted or failed lookups.
    /// </summary>
    public Task<BrregOrganization> GetOrganization(string organizationNumber);
}
