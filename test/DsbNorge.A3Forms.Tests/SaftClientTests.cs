using System.Net;
using System.Text.Json;
using DsbNorge.A3Forms.Clients.Saft;
using DsbNorge.A3Forms.Tests.resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
public class SaftClientTests
{
    private Mock<ILogger<ISaftClient>> _loggerMock;
    private MockHttpMessageHandler _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private IConfiguration _configuration;
    private SaftClient _saftClient;

    [SetUp]
    public void Setup()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "BaseUrl", "https://mockauth.com/" }
            }!)
            .Build();
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _loggerMock = new Mock<ILogger<ISaftClient>>();
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri(_configuration["BaseUrl"]!)
        };

        _saftClient = new SaftClient(
            _httpClient,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task GetPlisPerson_success_should_return_metadata()
    {
        SetupMockPlisPersonResponse(true);

        var result = await _saftClient.GetPlisPerson("mock-token", "12345678901");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PlisId, Is.Not.Null);
    }

    [Test]
    public async Task GetPlisPerson_failure_should_not_return_metadata()
    {
        SetupMockPlisPersonResponse(false);

        var result = await _saftClient.GetPlisPerson("mock-token", "12345678901");
        
        Assert.That(result, Is.Null);
    }

    private void SetupMockPlisPersonResponse(bool success)
    {
        var mockPlisPerson = new SaftClient.PlisPersonDetails(
            "12345678901",
            "Mock Mockesen",
            "type",
            "YES",
            "12345",
            "XSFD",
            "2025/0"
            );
        var mockResponse = success
            ? new SaftClient.GetPersonResponse(true, null, mockPlisPerson)
            : new SaftClient.GetPersonResponse(false, "Error message", null);

        var responseContent = JsonSerializer.Serialize(mockResponse);

        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent)
        });
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _mockHttpMessageHandler.Dispose();
    }
}