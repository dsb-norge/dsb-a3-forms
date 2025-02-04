using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Altinn.App.Core.Internal.Secrets;
using DsbNorge.A3Forms.Clients.Auth;
using DsbNorge.A3Forms.Tests.resources;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
public class AuthClientTests
{
    private MockHttpMessageHandler _httpMessageHandler;
    private Mock<ILogger<IAuthClient>> _loggerMock;
    private HttpClient _httpClient;
    private AuthClient _authClient;
    private IConfiguration _configuration;
    private ISecretsClient _secretsClient;

    [SetUp]
    public void Setup()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "TestApp:Auth:ClientId", "mock-client-id" },
                { "TestApp:Auth:ClientSecretName", "mock-client-secret" },
                { "TestApp:Auth:Url", "https://mockaddress.com/token" }
            }!)
            .Build();
        _httpMessageHandler = new MockHttpMessageHandler();
        _loggerMock = new Mock<ILogger<IAuthClient>>();
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://mockaddress.com/")
        };
        _secretsClient = new Mock<ISecretsClient>().Object;
        
        _authClient = new AuthClient(
            _loggerMock.Object,
            _httpClient,
            _configuration,
            _secretsClient
        );
    }

    [Test]
    public async Task GetToken_should_supply_token()
    {
        var mockTokenResponse = new
        {
            access_token = "eyJ0eXAiOiJKV1QJIUzI1NiJ9",
            expires_in = 300,
            refresh_expires_in = 0,
            token_type = "Bearer",
            not_before_policy = 0,
            scope = "profile email"
        };

        var jsonResponse = JsonSerializer.Serialize(mockTokenResponse);
        _httpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        });
        
        var token = await _authClient.GetToken(
            "TestApp:Auth:ClientId",
            "TestApp:Auth:ClientSecretName",
            "TestApp:Auth:Url"
            );
        
        Assert.That(token, Is.Not.Null);
        Assert.That(token, Is.EqualTo("eyJ0eXAiOiJKV1QJIUzI1NiJ9"));
    }

    [Test]
    public void GetToken_should_fail_if_config_is_missing()
    {
        var exception = Assert.ThrowsAsync<SystemException>(async () => 
            await _authClient.GetToken("Does:Not:Exist", "Does:Not:Exist", "Does:Not:Exist")
        );
        Assert.That(exception.Message, Is.EqualTo("Failed to get auth token"));

        _loggerMock.VerifyErrorLogging("Missing configuration for Does:Not:Exist");
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _httpMessageHandler.Dispose();
    }
}
