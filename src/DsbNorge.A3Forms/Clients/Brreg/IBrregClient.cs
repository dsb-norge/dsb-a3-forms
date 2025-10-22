namespace DsbNorge.A3Forms.Clients.Brreg;

public interface IBrregClient
{
    public Task<BrregOrganizationStatus> GetOrganizationStatus(string organizationNumber);
}