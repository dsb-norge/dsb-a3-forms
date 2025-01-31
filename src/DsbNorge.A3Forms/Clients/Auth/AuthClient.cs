using DsbNorge.A3Forms.Models;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DsbNorge.A3Forms.Clients.Auth;

public class AuthClient(
    ILogger<IAuthClient> logger,
    HttpClient client) : IAuthClient
{
    public async Task<string> GetToken(string clientId, string clientSecret, string tokenEndpoint)
    {
        try
        {
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