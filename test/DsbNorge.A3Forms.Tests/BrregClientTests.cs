using System.Net;
using System.Reflection;
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
    public void GetOrg_should_have_obsolete_attribute()
    {
        var method = typeof(BrregClient).GetMethod("GetOrg");
        var obsoleteAttr = method?.GetCustomAttribute<ObsoleteAttribute>();

        Assert.That(obsoleteAttr, Is.Not.Null);
        Assert.That(obsoleteAttr!.Message, Does.Contain("GetEntity"));
    }
    
    [Test]
    public async Task GetEntity_should_return_org_with_business_address()
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

        var result = await _brregClient.GetEntity("987654321");

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
    public async Task GetSubEntity_should_correctly_parse_full_brreg_json_response()
    {
        var mockData = await File.ReadAllTextAsync("resources/MockSubEntity.json");
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockData)
        });

        var result = await _brregClient.GetSubEntity("509100675");

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Organisasjonsnummer, Is.EqualTo("509100675"));
            Assert.That(result.Navn, Is.EqualTo("Sesam stasjon"));
            Assert.That(result.Beliggenhetsadresse, Is.Not.Null);
            Assert.That(result.Beliggenhetsadresse!.Kommune, Is.EqualTo("Oslo"));
            Assert.That(result.Beliggenhetsadresse.Land, Is.EqualTo("Norge"));
            Assert.That(result.Beliggenhetsadresse.Postnummer, Is.EqualTo("0010"));
            Assert.That(result.Postadresse, Is.Not.Null);
            Assert.That(result.Postadresse!.Poststed, Is.EqualTo("Oslo"));
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
    
    [Test]
      public async Task GetLegalOrgForm_returns_form_directly_for_hovedenhet()
      {
          _mockHttpMessageHandler.SetResponder(req =>
          {
              // /underenheter/{org} -> 404 (actor is a main entity)
              if (req.RequestUri!.AbsolutePath.Contains("/underenheter/"))
                  return new HttpResponseMessage(HttpStatusCode.NotFound);

              // /enheter/{org} -> entity with organisasjonsform
              var json = JsonSerializer.Serialize(new
              {
                  organisasjonsnummer = "987654321",
                  navn = "Ola Nordmann",
                  organisasjonsform = new { kode = "ENK", beskrivelse = "Enkeltpersonforetak" }
              });
              return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
          });

          var result = await _brregClient.GetLegalOrgForm("987654321");

          Assert.That(result, Is.Not.Null);
          Assert.That(result!.Code, Is.EqualTo("ENK"));
      }

      [Test]
      public async Task GetLegalOrgForm_resolves_hovedenhet_when_actor_is_underenhet()
      {
          _mockHttpMessageHandler.SetResponder(req =>
          {
              if (req.RequestUri!.AbsolutePath.Contains("/underenheter/"))
              {
                  // sub entity: own form is BEDR, but it points at its parent
                  var sub = JsonSerializer.Serialize(new
                  {
                      organisasjonsnummer = "111111111",
                      navn = "Underenhet",
                      organisasjonsform = new { kode = "BEDR", beskrivelse = "Bedrift" },
                      overordnetEnhet = "987654321"
                  });
                  return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(sub) };
              }

              // ain entity carries the legal form
              var entity = JsonSerializer.Serialize(new
              {
                  organisasjonsnummer = "987654321",
                  navn = "Ola Nordmann",
                  organisasjonsform = new { kode = "ENK", beskrivelse = "Enkeltpersonforetak" }
              });
              return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(entity) };
          });
  
          var result = await _brregClient.GetLegalOrgForm("111111111");

          Assert.That(result, Is.Not.Null);
          Assert.That(result!.Code, Is.EqualTo("ENK"));
      }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _mockHttpMessageHandler.Dispose();
        _memoryCache.Dispose();
    }
}
