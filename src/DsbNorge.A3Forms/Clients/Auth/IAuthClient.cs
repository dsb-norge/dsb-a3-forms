namespace DsbNorge.A3Forms.Clients.Auth;

public abstract class IAuthClient
{
    public Task<string> GetToken(string clientIdConfig, string clientSecretConfig, string clientAuthUrlConfig);
}