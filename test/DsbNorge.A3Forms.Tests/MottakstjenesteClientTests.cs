using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DsbNorge.A3Forms.Clients.Mottakstjeneste;
using DsbNorge.A3Forms.Models;
using DsbNorge.A3Forms.Providers;
using DsbNorge.A3Forms.Tests.resources;
using Moq;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
public class MottakstjenesteClientTests
{
    private Mock<ILogger<IMottakstjenesteClient>> _loggerMock;
    private MockHttpMessageHandler _mockHttpMessageHandler;
    private MemoryCache _memoryCache;
    private HttpClient _httpClient;
    private IConfiguration _configuration;
    private NationalitiesProvider _nationalitiesProvider;

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
        _loggerMock = new Mock<ILogger<IMottakstjenesteClient>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri(_configuration["BaseUrl"]!)
        };

        var mottakstjenesteClient = new MottakstjenesteClient(
            _httpClient,
            _loggerMock.Object,
            _memoryCache
        );
        _nationalitiesProvider = new NationalitiesProvider(mottakstjenesteClient);
    }

    [Test]
    public Task NationalitiesProvider_default_id()
    {
        Assert.That(_nationalitiesProvider.Id, Is.EqualTo("nationalities"));
        return Task.CompletedTask;
    }

    [Test]
    public async Task GetNationalities_should_return_nationalities_in_elulykke_form()
    {
        SetupMockNationalitiesResponse();

        var result = await _nationalitiesProvider.GetNationalities("melding-om-elulykke");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task GetNationalities_should_return_nationality_options_in_elulykke_form()
    {
        SetupMockNationalitiesResponse();

        var result = await _nationalitiesProvider.GetAppOptionsAsync(
            null,
            new Dictionary<string, string> { ["formName"] = "melding-om-elulykke" }
            );
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Options, Has.Count.EqualTo(4));
    }

    private void SetupMockNationalitiesResponse()
    {
        var mockNationalitiesResponse = new[]
        {
            new Nationality { Name = "Andorra", CountryCode = "AND", A2CountryCode = "AD" },
            new Nationality { Name = "De forente arabiske emirater", CountryCode = "ARE", A2CountryCode = "AE" },
            new Nationality { Name = "Afghanistan", CountryCode = "AFG", A2CountryCode = "AF" },
            new Nationality { Name = "Antigua og Barbuda", CountryCode = "ATG", A2CountryCode = "AG" }
        };

        var responseContent = JsonSerializer.Serialize(mockNationalitiesResponse);

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
        _memoryCache.Dispose();
        _mockHttpMessageHandler.Dispose();
    }
}