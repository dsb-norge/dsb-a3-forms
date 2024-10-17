using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Clients.Mottakstjeneste;

public interface IMottakstjenesteClient
{
    Task<List<Nationality>> GetNationalities(string formName);
}