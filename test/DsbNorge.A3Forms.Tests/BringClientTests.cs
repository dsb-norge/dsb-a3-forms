using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DsbNorge.A3Forms.Clients.Bring;
using DsbNorge.A3Forms.Models;
using DsbNorge.A3Forms.Tests.resources;
using Moq;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
public class BringClientTests
{
    private Mock<ILogger<IBringClient>> _loggerMock;
    private MockHttpMessageHandler _mockHttpMessageHandler;
    private MemoryCache _memoryCache;
    private HttpClient _httpClient;
    private BringClient _bringClient;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<IBringClient>>();
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://mockaddress.com/")
        };
        
        _bringClient = new BringClient(
            _httpClient,
            _loggerMock.Object,
            _memoryCache
        );
    }

    [Test]
    public async Task GetCity_should_return_city()
    {
        const string postalCode = "0010";
        var expectedCityResponse = new BringCityResponse
        {
            Result = "Oslo",
            Valid = true,
            PostalCodeType = "Street"
        };
        
        var jsonResponse = JsonSerializer.Serialize(expectedCityResponse);
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        });
        
        var result = await _bringClient.GetCity(postalCode);
        
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("Oslo"));
            Assert.That(_memoryCache.TryGetValue($"city-{postalCode}", out string? cachedCity), Is.True);
            Assert.That(cachedCity, Is.EqualTo("Oslo"));
        });
    }

    [Test]
    public async Task GetCity_should_return_cached_value()
    {
        const string postalCode = "0010";
        const string cachedCity = "Oslo";
        
        _memoryCache.Set($"city-{postalCode}", cachedCity);
        
        var result = await _bringClient.GetCity(postalCode);
        
        Assert.That(result, Is.EqualTo(cachedCity));
        _loggerMock.Verify(
            logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()!), 
            Times.Never
        );
    }

    [Test]
    public async Task GetCity_should_log_error_on_fail()
    {
        const string postalCode = "0010";
        
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        });
        
        var result = await _bringClient.GetCity(postalCode);
        
        Assert.That(result, Is.Null);
        
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed with status code")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once
        );
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _memoryCache.Dispose();
        _mockHttpMessageHandler.Dispose();
    }
}
