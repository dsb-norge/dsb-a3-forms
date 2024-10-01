using DsbA3Forms.Models.Address;

namespace DsbA3Forms.Clients;
public interface IGeoNorgeClient
{
    public Task<List<GeoNorgeAdresse>> GetAddresses(string searchString, int hitsPerPage);

    public Task<List<Municipality>> GetMunicipalities();
}
