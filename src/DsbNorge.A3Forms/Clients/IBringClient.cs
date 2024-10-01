namespace DsbNorge.A3Forms.Clients;
public interface IBringClient
{
    public Task<string> GetCity(string postalCode);
}
