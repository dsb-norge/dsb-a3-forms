using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Clients.Brreg;

public interface IBrregClient
{
    public Task<BrregOrganizationStatus> GetOrganizationStatus(string organizationNumber);
    public Task<BrregOrgForm?> GetOrgForm(string code);
    public Task<BrregOrg?> GetOrg(string orgNumber);
}