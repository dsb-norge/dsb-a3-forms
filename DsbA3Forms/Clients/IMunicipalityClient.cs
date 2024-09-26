using DsbA3Forms.Models.Address;

namespace DsbA3Forms.Clients;

public interface IMunicipalityClient
{
    public Task<List<Municipality>> GetMunicipalities();
}
