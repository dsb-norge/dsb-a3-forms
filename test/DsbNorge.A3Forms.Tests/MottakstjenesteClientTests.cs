using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DsbNorge.A3Forms.Clients.Mottakstjeneste;
using DsbNorge.A3Forms.Models;
using DsbNorge.A3Forms.OptionsProviders;
using Moq;

namespace DsbNorge.A3Forms.Tests;

public class MottakstjenesteClientTests
{
    private Mock<ILogger<IMottakstjenesteClient>> _loggerMock;
    private TestHttpMessageHandler _testHttpMessageHandler;
    private NationalitiesOptions _nationalitiesOptions;
    private IMemoryCache _memoryCache;
    private HttpClient _httpClient;
    private IConfiguration _configuration;

    [SetUp]
    public void Setup()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "https://testprod.dsb.no", "https://testprod.dsb.no" }
            }!)
            .Build();

        _testHttpMessageHandler = new TestHttpMessageHandler();
        _loggerMock = new Mock<ILogger<IMottakstjenesteClient>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _httpClient = new HttpClient(_testHttpMessageHandler)
        {
            BaseAddress = new Uri(_configuration["https://testprod.dsb.no"])
        };

        var mottakstjenesteClient = new MottakstjenesteClient(
            _httpClient,
            _loggerMock.Object,
            _configuration,
            _memoryCache
        );
        _nationalitiesOptions = new NationalitiesOptions(mottakstjenesteClient);
    }

    [Test]
    public async Task GetNationalities_as_elulykke_form()
    {
        var mockNationalitiesResponse = new List<Nationality>
        {
            new Nationality { Name = "Country B", CountryCode = "SE", A2CountryCode = "SE" },
            new Nationality { Name = "Country A", CountryCode = "NO", A2CountryCode = "NO" },
            new Nationality { Name = "Country C", CountryCode = "DK", A2CountryCode = "DK" }
        };
        var responseContent = JsonSerializer.Serialize(mockNationalitiesResponse);
        
        _testHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent)
        });
        const string formName = "melding-om-elulykke";
        var result = await _nationalitiesOptions.GetNationalitiesOptions(formName);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Options.Count, Is.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Options[0].Label, Is.EqualTo("Country A - NO"));
            Assert.That(result.Options[1].Label, Is.EqualTo("Country B - SE"));
            Assert.That(result.Options[2].Label, Is.EqualTo("Country C - DK"));
        });
    }
    
    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _memoryCache.Dispose();
        _testHttpMessageHandler.Dispose();
    }

    private class TestHttpMessageHandler : HttpMessageHandler, IDisposable
    {
        private HttpResponseMessage _httpResponseMessage;

        public void SetHttpResponse(HttpResponseMessage responseMessage)
        {
            _httpResponseMessage = responseMessage;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(_httpResponseMessage);
        }

        public void Dispose()
        {
            _httpResponseMessage?.Dispose();
        }
    }
}