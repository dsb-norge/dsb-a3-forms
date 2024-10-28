using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using DsbNorge.A3Forms.Clients.Auth;
using DsbNorge.A3Forms.Tests.resources;
using Moq;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
public class AuthClientTests
{
    private MockHttpMessageHandler _httpMessageHandler;
    private Mock<ILogger<IAuthClient>> _loggerMock;
    private HttpClient _httpClient;
    private AuthClient _authClient;

    [SetUp]
    public void Setup()
    {
        _httpMessageHandler = new MockHttpMessageHandler();
        _loggerMock = new Mock<ILogger<IAuthClient>>();
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://mockaddress.com/")
        };
        
        _authClient = new AuthClient(
            _loggerMock.Object,
            _httpClient
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
            "mock-client-id",
            "mock-client-secret",
            "https://mockaddress.com/getauthtoken"
            );
        
        Assert.That(token, Is.Not.Null);
        Assert.That(token, Is.EqualTo("eyJ0eXAiOiJKV1QJIUzI1NiJ9"));
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _httpMessageHandler.Dispose();
    }
}
