namespace DsbNorge.A3Forms.Clients.Auth;

public interface IAuthClient
{
    Task<string> GetToken(string formIdConfig, string clientSecretConfig, string clientAuthUrlConfig);
}