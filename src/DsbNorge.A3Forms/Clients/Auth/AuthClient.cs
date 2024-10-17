using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Altinn.App.Core.Internal.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DsbNorge.A3Forms.Clients.Auth;

public class AuthClient : IAuthClient
{
    private readonly ISecretsClient _secretsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IAuthClient> _logger;
    private readonly HttpClient _client;
    
    public AuthClient(
        ISecretsClient secretsService,
        IConfiguration configuration,
        ILogger<IAuthClient> logger,
        HttpClient client,
        string clientIdUrlConfig
        )
    {
        _logger = logger;
        _client = client;
        _client.BaseAddress = new Uri(configuration[clientIdUrlConfig]);
        _configuration = configuration;
        _secretsService = secretsService;
    }

    private async Task<string> GetToken(string clientIdConfig, string clientSecretConfig, string clientAuthUrlConfig)
    {
        var form = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _configuration[clientIdConfig] },
            { "client_secret", await _secretsService.GetSecretAsync(clientSecretConfig) }
        };
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("cache-control", "no-cache");

            var tokenResponse = await client.PostAsync(_configuration[clientAuthUrlConfig],
                new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            var tok = JsonSerializer.Deserialize<Token>(jsonContent);
            return tok?.AccessToken;
        }
        catch (Exception e)
        {
            _logger.LogError("{Message}", e.Message);
            throw new SystemException("Failed to get auth token");
        }
    }

    private class Token
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; }
        [JsonPropertyName("token_type")] public string TokenType { get; init; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; init; }
    }
}
