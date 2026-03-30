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
}