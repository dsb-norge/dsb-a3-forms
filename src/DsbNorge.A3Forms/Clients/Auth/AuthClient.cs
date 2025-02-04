using Altinn.App.Core.Internal.Secrets;
using DsbNorge.A3Forms.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DsbNorge.A3Forms.Clients.Auth;

public class AuthClient(
    ILogger<IAuthClient> logger,
    HttpClient client,
    IConfiguration config,
    ISecretsClient secretsClient
    ) : IAuthClient
{
    /// <summary>Get an access token from the token endpoint.</summary>
    /// <param name="clientIdKey">The key in the app configuration that holds the client id.</param>
    /// <param name="clientSecretName">Name of client secret in Azure keyvault</param>
    /// <param name="tokenEndpointKey">The key in the app configuration that holds the token endpoint.</param>
    public async Task<string> GetToken(string clientIdKey, string clientSecretName, string tokenEndpointKey)
    {
        try
        {
            var clientId = config[clientIdKey] ?? throw new SystemException("Missing configuration for " + clientIdKey);
            var clientSecret = await secretsClient.GetSecretAsync(clientSecretName);
            var form = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };
            
            if (!client.DefaultRequestHeaders.Contains("cache-control"))
            {
                client.DefaultRequestHeaders.Add("cache-control", "no-cache");
            }

            var tokenEndpoint = config[tokenEndpointKey] ?? throw new SystemException("Missing configuration for " + tokenEndpointKey);
            var tokenResponse = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            
            var tok = JsonSerializer.Deserialize<Token>(jsonContent);
            if (tok?.AccessToken is null or "")
            {
                throw new SystemException("No token returned!");
            }
            return tok.AccessToken;
        }
        catch (Exception e)
        {
            logger.LogError("{Message}", e.Message);
            throw new SystemException("Failed to get auth token", e);
        }
    }
}