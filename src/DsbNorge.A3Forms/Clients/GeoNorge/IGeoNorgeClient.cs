using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Clients.GeoNorge;

public interface IGeoNorgeClient
{
    public Task<List<GeoNorgeAdresse>> GetAddresses(string searchString, int? radius, int hitsPerPage);

    public Task<List<Municipality>> GetMunicipalities();
}
