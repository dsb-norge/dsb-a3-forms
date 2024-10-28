using System.Net;
using System.Text.Json;
using DsbNorge.A3Forms.Clients.GeoNorge;
using DsbNorge.A3Forms.Models;
using DsbNorge.A3Forms.OptionsProviders;
using DsbNorge.A3Forms.Tests.resources;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
public class GeoNorgeClientTests
{
    private Mock<ILogger<IGeoNorgeClient>> _loggerMock;
    private MockHttpMessageHandler _mockHttpMessageHandler;
    private MunicipalitiesProvider _municipalitiesProvider;
    private AddressSearchHitsProvider _addressSearchHitsProvider;
    private IMemoryCache _memoryCache;
    private HttpClient _httpClient;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<IGeoNorgeClient>>();
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://mockaddress.com/")
        };
        
        var geoNorgeClient = new GeoNorgeClient(
            _httpClient,
            _loggerMock.Object,
            _memoryCache
        );
        _municipalitiesProvider = new MunicipalitiesProvider(geoNorgeClient);
        _addressSearchHitsProvider = new AddressSearchHitsProvider(geoNorgeClient);
    }

    [Test]
    public async Task GetAddresses_should_return_address_when_valid_search()
    {
        var mockAddressResponse = new GeoNorgeAdresseRespons
        {
            Adresser = new List<GeoNorgeAdresse>
            {
                new GeoNorgeAdresse { Adressetekst = "Testveien 1", Postnummer = "1234", Poststed = "Oslo" },
                new GeoNorgeAdresse { Adressetekst = "Testveien 2", Postnummer = "5678", Poststed = "Bergen" }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(mockAddressResponse);
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        });
        
        var result = await _addressSearchHitsProvider.GetAddresses("Testveien", 10);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ListItems, Has.Count.EqualTo(2));

        var firstResult = result.ListItems[0] as AddressSearchHit;
        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Address, Is.EqualTo("Testveien 1"));
            Assert.That(firstResult.PostalCode, Is.EqualTo("1234"));
            Assert.That(firstResult.PostalCity, Is.EqualTo("Oslo"));
        });
    }

    [Test]
    public async Task GetAddresses_should_return_emptyList_when_search_is_empty()
    {
        var result = await _addressSearchHitsProvider.GetAddresses("", 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ListItems, Is.Empty);
    }

    [Test]
    public async Task GetAddresses_should_return_emptyList_when_call_fails()
    {
        const string searchString = "Oslo";

        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest
        });

        var result = await _addressSearchHitsProvider.GetAddresses(searchString, 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ListItems, Is.Empty);
    }

    [Test]
    public async Task GetMunicipalities_should_return_municipalities()
    {
        var mockMunicipalitiesResponse = new[]
        {
            new Municipality { Kommunenummer = "1234", KommunenavnNorsk = "Oslo" },
            new Municipality { Kommunenummer = "5678", KommunenavnNorsk = "Bergen" }
        };

        var responseContent = JsonSerializer.Serialize(mockMunicipalitiesResponse);
        
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent)
        });
        
        var result = await _municipalitiesProvider.GetMunicipalitiesOptions();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Options.Count, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Options[0].Label, Is.EqualTo("Bergen - 5678"));
            Assert.That(result.Options[0].Value, Is.EqualTo("5678"));
            Assert.That(result.Options[1].Label, Is.EqualTo("Oslo - 1234"));
            Assert.That(result.Options[1].Value, Is.EqualTo("1234"));
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
