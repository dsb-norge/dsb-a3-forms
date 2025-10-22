using System.Net;
using System.Text.Json;
using DsbNorge.A3Forms.Clients.Brreg;
using DsbNorge.A3Forms.Tests.resources;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
public class BrregClientTests
{
    private Mock<ILogger<IBrregClient>> _loggerMock;
    private MockHttpMessageHandler _mockHttpMessageHandler;
    private BrregClient _brregClient;
    private HttpClient _httpClient;
    private MemoryCache _memoryCache;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<IBrregClient>>();
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://brregmock.no/")
        };
        _brregClient = new BrregClient(
            _httpClient,
            _loggerMock.Object,
            _memoryCache
        );
    }

    [Test]
    public async Task GetOrg_should_return_org_with_business_address()
    {
        var mockData = JsonSerializer.Serialize(new
        {
            organisasjonsnummer = "987654321",
            navn = "Test Bedrift AS",
            forretningsadresse = new
            {
                postnummer = "0123",
                poststed = "Oslo"
            }
        });
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockData)
        });

        var result = await _brregClient.GetOrg("987654321");

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Organisasjonsnummer, Is.EqualTo("987654321"));
            Assert.That(result.Navn, Is.EqualTo("Test Bedrift AS"));
            Assert.That(result.ForretningsAdresse, Is.Not.Null);
            Assert.That(result.ForretningsAdresse!.Postnummer, Is.EqualTo("0123"));
            Assert.That(result.ForretningsAdresse.Poststed, Is.EqualTo("Oslo"));
        });
    }

    [Test]
    public async Task GetOrgForm_should_return_org_form_description()
    {
        var mockData = JsonSerializer.Serialize(new
        {
            kode = "AS",
            beskrivelse = "Aksjeselskap"
        });
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockData)
        });

        var result = await _brregClient.GetOrgForm("AS");

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("AS"));
            Assert.That(result.Description, Is.EqualTo("Aksjeselskap"));
        });
    }

    [Test]
    public async Task GetOrganizationStatus_should_return_Deleted_when_deletion_date_is_in_past()
    {
        var pastDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
        var mockData = JsonSerializer.Serialize(new
        {
            organisasjonsnummer = "123456789",
            navn = "Slettet bedrift",
            slettedato = pastDate
        });
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockData)
        });

        var status = await _brregClient.GetOrganizationStatus("987654321");

        Assert.That(status, Is.EqualTo(BrregOrganizationStatus.Deleted));
    }

    [Test]
    public async Task GetOrganizationStatus_should_return_SubEntity_when_deletion_date_is_in_future()
    {
        var futureDate = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
        var mockData = JsonSerializer.Serialize(new
        {
            organisasjonsnummer = "123456789",
            navn = "Aktiv bedrift",
            slettedato = futureDate
        });
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockData)
        });

        var status = await _brregClient.GetOrganizationStatus("123456789");

        Assert.That(status, Is.EqualTo(BrregOrganizationStatus.SubEntity));
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _mockHttpMessageHandler.Dispose();
        _memoryCache.Dispose();
    }
}
