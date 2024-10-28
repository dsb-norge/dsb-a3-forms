namespace DsbNorge.A3Forms.Clients.Bring;

public interface IBringClient
{
    public Task<string> GetCity(string postalCode);
}
