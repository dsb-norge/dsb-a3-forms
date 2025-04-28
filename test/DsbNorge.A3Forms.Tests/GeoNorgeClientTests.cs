using System.Net;
using System.Text.Json;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.GeoNorge;
using DsbNorge.A3Forms.Models;
using DsbNorge.A3Forms.Services;
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
    private MunicipalityService _municipalityService;
    private AddressSearchService _addressSearchService;
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
        _municipalityService = new MunicipalityService(geoNorgeClient);
        _addressSearchService = new AddressSearchService(geoNorgeClient);
    }

    [Test]
    public async Task GetAddresses_should_return_address_when_valid_search()
    {
        SetupMockAddressResponse([
            new GeoNorgeAdresse { Adressetekst = "Testveien 1", Postnummer = "1234", Poststed = "Oslo" },
            new GeoNorgeAdresse { Adressetekst = "Testveien 2", Postnummer = "5678", Poststed = "Bergen" }
        ]);

        var result = await _addressSearchService.GetAddresses("Testveien", null,10);
        
        AssertAddressSearchResult(result);
    }

    [Test]
    public async Task GetAddressOptions_should_return_address_options()
    {
        SetupMockAddressResponse([
            new GeoNorgeAdresse { Adressetekst = "Testveien 1", Postnummer = "1234", Poststed = "Oslo" },
            new GeoNorgeAdresse { Adressetekst = "Testveien 2", Postnummer = "5678", Poststed = "Bergen" }
        ]);

        var result = await _addressSearchService.GetInstanceDataListAsync(
            new Dictionary<string, string> { ["search"] = "Testveien" }
        );
        
        AssertAddressSearchResult(result);
    }

    private void SetupMockAddressResponse(List<GeoNorgeAdresse> addressList)
    {
        var mockAddressResponse = new GeoNorgeAdresseRespons
        {
            Adresser = addressList
        };

        var jsonResponse = JsonSerializer.Serialize(mockAddressResponse);
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        });
    }

    private static void AssertAddressSearchResult(List<GeoNorgeAdresse> result)
    {
        Assert.That(result, Has.Count.EqualTo(2));

        var firstResult = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Adressetekst, Is.EqualTo("Testveien 1"));
            Assert.That(firstResult.Postnummer, Is.EqualTo("1234"));
            Assert.That(firstResult.Poststed, Is.EqualTo("Oslo"));
        });
    }

    private static void AssertAddressSearchResult(DataList result)
    {
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ListItems, Has.Count.EqualTo(2));

        var firstResult = (result.ListItems[0] as AddressSearchHit)!;
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
        var result = await _addressSearchService.GetAddresses("", null, 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAddresses_should_return_emptyList_when_call_fails()
    {
        const string searchString = "Oslo";

        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest
        });

        var result = await _addressSearchService.GetAddresses(searchString, null, 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public async Task GetAddresses_should_return_address_when_valid_coordinates_search()
    {
        const string coordinates = "59.9139,10.7522";
        const int radius = 500;
        const int hitsPerPage = 5;
        
        SetupMockAddressResponse([
            new GeoNorgeAdresse { Adressetekst = "Karl Johans gate 1", Postnummer = "0010", Poststed = "Oslo" },
            new GeoNorgeAdresse { Adressetekst = "Stortingsgata 4", Postnummer = "0010", Poststed = "Oslo" }
        ]);
        
        var result = await _addressSearchService.GetAddresses(coordinates, radius, hitsPerPage);
        
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Adressetekst, Is.EqualTo("Karl Johans gate 1"));
            Assert.That(result[0].Postnummer, Is.EqualTo("0010"));
            Assert.That(result[0].Poststed, Is.EqualTo("Oslo"));
        });
    }
    
    [Test]
    public async Task GetAddresses_should_do_search_by_coordinates()
    {
        const string coordinates = "59.9139,10.7522";
        const int hitsPerPage = 5;
        const int radius = 500;
        const string path = "/adresser/v1/punktsok";
        
        SetupMockAddressResponse([]); 
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler.CaptureRequest(req => capturedRequest = req);
        
        await _addressSearchService.GetAddresses(coordinates, radius, hitsPerPage);

        Assert.That(capturedRequest?.RequestUri?.AbsolutePath, Does.StartWith(path));
    }
    
    [Test]
    public async Task GetAddresses_should_do_search_by_address()
    {
        const string searchString = "Testveien 1";
        const int hitsPerPage = 5;
        int? radius = null;
        const string path = "/adresser/v1/sok";

        SetupMockAddressResponse([]); 
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler.CaptureRequest(req => capturedRequest = req); 
        
        await _addressSearchService.GetAddresses(searchString, radius, hitsPerPage);
        
        Assert.That(capturedRequest?.RequestUri?.AbsolutePath, Does.StartWith(path));
    }
    
    [Test]
    public async Task GetMunicipalities_should_return_municipalities()
    {
        SetupMockMunicipalitiesResponse();

        var list = await _municipalityService.GetMunicipalities(); 
        var sortedList = list.OrderBy(m => m.Kommunenummer).ToList();
        
        Assert.That(sortedList, Is.Not.Null);
        Assert.That(sortedList, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(sortedList[0].KommunenavnNorsk, Is.EqualTo("Oslo"));
            Assert.That(sortedList[0].Kommunenummer, Is.EqualTo("1234"));
            Assert.That(sortedList[1].KommunenavnNorsk, Is.EqualTo("Bergen"));
            Assert.That(sortedList[1].Kommunenummer, Is.EqualTo("5678"));
        });
    }

    [Test]
    public async Task GetMunicipalityOptions_should_return_municipalityOptions()
    {
        SetupMockMunicipalitiesResponse();
        
        var result = await _municipalityService.GetAppOptionsAsync();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Options, Is.Not.Null);
        Assert.That(result.Options, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Options[0].Label, Is.EqualTo("Bergen - 5678"));
            Assert.That(result.Options[0].Value, Is.EqualTo("5678"));
            Assert.That(result.Options[1].Label, Is.EqualTo("Oslo - 1234"));
            Assert.That(result.Options[1].Value, Is.EqualTo("1234"));
        });
    }

    private void SetupMockMunicipalitiesResponse()
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
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _memoryCache.Dispose();
        _mockHttpMessageHandler.Dispose();
    }
}
