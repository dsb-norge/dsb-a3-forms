using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Clients.Brreg;

public interface IBrregClient
{
    public Task<BrregOrganizationStatus> GetOrganizationStatus(string organizationNumber);
    public Task<BrregOrgForm?> GetOrgForm(string code);
    [Obsolete("GetOrg is deprecated. Use GetEntity instead.")]
    public Task<BrregOrg?> GetOrg(string organizationNumber);

    public Task<BrregOrg?> GetEntity(string organizationNumber);
    public Task<BrregSubEntity?> GetSubEntity(string organizationNumber);

    /// <summary>
    /// Returns the sub entities (underenheter) of the given main entity.
    /// Returns an empty list when the entity has no sub entities, and null when the lookup fails.
    /// </summary>
    public Task<List<BrregSubEntity>?> GetSubEntities(string organizationNumber);
    
    /// <summary>
    /// Returns the legal organisasjonsform (ENK, AS, NUF, ...) of the actor.
    /// Underenheter always have form BEDR/AAFY, so when the org is an underenhet
    /// this resolves and returns the form of its hovedenhet (overordnetEnhet).
    /// </summary>
    public Task<BrregOrgForm?> GetLegalOrgForm(string organizationNumber);
}