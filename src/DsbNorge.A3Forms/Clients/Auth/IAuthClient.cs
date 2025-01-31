namespace DsbNorge.A3Forms.Clients.Auth;

public interface IAuthClient
{
    Task<string> GetToken(string clientId, string clientSecret, string tokenEndpoint);
}