using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Clients.Saft;

public interface ISaftClient
{
    public Task<PlisMeta?> GetPlisPerson(string accessToken, string ssn);
}