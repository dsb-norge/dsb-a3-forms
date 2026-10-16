using System.Net;
using System.Text.Json;
using DsbNorge.A3Forms.Clients.Brreg;
using DsbNorge.A3Forms.Models;
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
    public async Task GetOrganization_should_return_main_entity_with_business_address_and_sub_entities()
    {
        _mockHttpMessageHandler.SetResponder(req =>
        {
            // sub entity list lookup: /underenheter?overordnetEnhet=...
            if (!string.IsNullOrEmpty(req.RequestUri!.Query))
            {
                var list = JsonSerializer.Serialize(new
                {
                    _embedded = new
                    {
                        underenheter = new[]
                        {
                            new
                            {
                                organisasjonsnummer = "315829186",
                                navn = "ALFABETISK SIGEN TIGER AS",
                                organisasjonsform = new
                                {
                                    kode = "BEDR",
                                    beskrivelse = "Underenhet til næringsdrivende og offentlig forvaltning"
                                },
                                overordnetEnhet = "987654321",
                                beliggenhetsadresse = new { postnummer = "0010", poststed = "Oslo" }
                            }
                        }
                    }
                });
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(list) };
            }

            // the org itself is not a sub entity
            if (req.RequestUri.AbsolutePath.Contains("/underenheter/"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var entity = JsonSerializer.Serialize(new
            {
                organisasjonsnummer = "987654321",
                navn = "Test Bedrift AS",
                organisasjonsform = new { kode = "AS", beskrivelse = "Aksjeselskap" },
                forretningsadresse = new
                {
                    postnummer = "0123",
                    poststed = "Oslo",
                    adresse = new[] { "Testveien 1" }
                }
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(entity) };
        });

        var result = await _brregClient.GetOrganization("987654321");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.Ok));
            Assert.That(result.Organisasjonsnummer, Is.EqualTo("987654321"));
            Assert.That(result.Navn, Is.EqualTo("Test Bedrift AS"));
            Assert.That(result.Type, Is.EqualTo(EntityType.Main));
            Assert.That(result.OverordnetEnhet, Is.Null);
            Assert.That(result.Organisasjonsform?.Code, Is.EqualTo("AS"));
            Assert.That(result.ForretningsAdresse, Is.Not.Null);
            Assert.That(result.ForretningsAdresse!.Postnummer, Is.EqualTo("0123"));
            Assert.That(result.ForretningsAdresse.Poststed, Is.EqualTo("Oslo"));
            Assert.That(result.ForretningsAdresse.AdresseLinjer, Is.EqualTo(new[] { "Testveien 1" }));
            Assert.That(result.Underenheter, Has.Count.EqualTo(1));
        });

        var sub = result.Underenheter![0];
        Assert.Multiple(() =>
        {
            Assert.That(sub.Organisasjonsnummer, Is.EqualTo("315829186"));
            Assert.That(sub.Navn, Is.EqualTo("ALFABETISK SIGEN TIGER AS"));
            Assert.That(sub.Type, Is.EqualTo(EntityType.Sub));
            Assert.That(sub.Organisasjonsform?.Code, Is.EqualTo("BEDR"));
            Assert.That(sub.OverordnetEnhet, Is.EqualTo("987654321"));
            Assert.That(sub.OverordnetOrganisasjonsform?.Code, Is.EqualTo("AS"));
            Assert.That(sub.Beliggenhetsadresse?.Postnummer, Is.EqualTo("0010"));
        });
    }

    [Test]
    public async Task GetOrganization_should_return_empty_sub_entity_list_when_main_entity_has_none()
    {
        _mockHttpMessageHandler.SetResponder(req =>
        {
            // BRREG omits _embedded entirely when there are no hits
            if (!string.IsNullOrEmpty(req.RequestUri!.Query))
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"page":{"size":20,"totalElements":0,"totalPages":0,"number":0}}""")
                };

            if (req.RequestUri.AbsolutePath.Contains("/underenheter/"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var entity = JsonSerializer.Serialize(new { organisasjonsnummer = "312954672", navn = "Hovedenhet AS" });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(entity) };
        });

        var result = await _brregClient.GetOrganization("312954672");

        Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.Ok));
        Assert.That(result.Underenheter, Is.Not.Null);
        Assert.That(result.Underenheter, Is.Empty);
    }

    [Test]
    public async Task GetOrganization_should_return_Ok_with_empty_sub_entity_list_when_sub_entity_list_is_not_found()
    {
        _mockHttpMessageHandler.SetResponder(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("/underenheter"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var entity = JsonSerializer.Serialize(new { organisasjonsnummer = "312954672", navn = "Hovedenhet AS" });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(entity) };
        });

        var result = await _brregClient.GetOrganization("312954672");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.Ok));
            Assert.That(result.Underenheter, Is.Not.Null);
            Assert.That(result.Underenheter, Is.Empty);
        });
    }

    [Test]
    public async Task GetOrganization_should_request_correct_sub_entity_list_url()
    {
        var requestedUris = new List<Uri>();
        _mockHttpMessageHandler.CaptureRequest(req => requestedUris.Add(req.RequestUri!));
        _mockHttpMessageHandler.SetResponder(req =>
            req.RequestUri!.AbsolutePath.Contains("/underenheter/")
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"organisasjonsnummer":"312954672","navn":"Hovedenhet AS"}""") });

        await _brregClient.GetOrganization("312954672");

        Assert.That(requestedUris.Select(u => u.PathAndQuery), Does.Contain(
            "/enhetsregisteret/api/underenheter?overordnetEnhet=312954672&size=500&sort=navn,DESC"));
    }

    [Test]
    public async Task GetOrganization_should_return_LookupFailed_when_sub_entity_list_lookup_fails()
    {
        _mockHttpMessageHandler.SetResponder(req =>
        {
            if (!string.IsNullOrEmpty(req.RequestUri!.Query))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            if (req.RequestUri.AbsolutePath.Contains("/underenheter/"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var entity = JsonSerializer.Serialize(new { organisasjonsnummer = "312954672", navn = "Hovedenhet AS" });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(entity) };
        });

        var result = await _brregClient.GetOrganization("312954672");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.LookupFailed));
            Assert.That(result.Navn, Is.EqualTo("Hovedenhet AS"));
            Assert.That(result.Underenheter, Is.Null);
        });
    }

    [Test]
    public async Task GetOrganization_should_correctly_parse_full_brreg_sub_entity_json_response()
    {
        var mockSubEntity = await File.ReadAllTextAsync("resources/MockSubEntity.json");
        _mockHttpMessageHandler.SetResponder(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("/underenheter/"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(mockSubEntity) };

            var parent = JsonSerializer.Serialize(new
            {
                organisasjonsnummer = "376181782",
                navn = "Sesam AS",
                organisasjonsform = new { kode = "AS", beskrivelse = "Aksjeselskap" }
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(parent) };
        });

        var result = await _brregClient.GetOrganization("509100675");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.Ok));
            Assert.That(result.Organisasjonsnummer, Is.EqualTo("509100675"));
            Assert.That(result.Navn, Is.EqualTo("Sesam stasjon"));
            Assert.That(result.Type, Is.EqualTo(EntityType.Sub));
            Assert.That(result.OverordnetEnhet, Is.EqualTo("376181782"));
            Assert.That(result.Organisasjonsform?.Code, Is.EqualTo("BEDR"));
            Assert.That(result.OverordnetOrganisasjonsform?.Code, Is.EqualTo("AS"));
            Assert.That(result.ForretningsAdresse, Is.Null);
            Assert.That(result.Underenheter, Is.Null);
            Assert.That(result.Beliggenhetsadresse, Is.Not.Null);
            Assert.That(result.Beliggenhetsadresse!.Kommune, Is.EqualTo("Oslo"));
            Assert.That(result.Beliggenhetsadresse.Land, Is.EqualTo("Norge"));
            Assert.That(result.Beliggenhetsadresse.Postnummer, Is.EqualTo("0010"));
            Assert.That(result.Postadresse, Is.Not.Null);
            Assert.That(result.Postadresse!.Poststed, Is.EqualTo("Oslo"));
        });
    }

    [Test]
    public async Task GetOrganization_should_return_OrganizationNotFound_when_not_found()
    {
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _brregClient.GetOrganization("123456789");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.OrganizationNotFound));
            Assert.That(result.Organisasjonsnummer, Is.EqualTo("123456789"));
            Assert.That(result.Type, Is.Null);
        });
    }

    [Test]
    public async Task GetOrganization_should_return_LookupFailed_when_sub_entity_and_entity_lookups_fail()
    {
        _mockHttpMessageHandler.SetHttpResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await _brregClient.GetOrganization("123456789");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.LookupFailed));
            Assert.That(result.Organisasjonsnummer, Is.EqualTo("123456789"));
        });
    }

    [Test]
    public async Task GetOrganization_should_return_LookupFailed_without_further_lookups_when_sub_entity_lookup_fails()
    {
        var requestedUris = new List<Uri>();
        _mockHttpMessageHandler.CaptureRequest(req => requestedUris.Add(req.RequestUri!));
        _mockHttpMessageHandler.SetResponder(BuildEntityWithSubEntitiesResponder(subEntityCount: 0, subEntityStatus: HttpStatusCode.InternalServerError));

        var result = await _brregClient.GetOrganization("312954672");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.LookupFailed));
            Assert.That(result.Type, Is.Null);
            Assert.That(requestedUris.Select(u => u.AbsolutePath), Is.EqualTo(new[] { "/enhetsregisteret/api/underenheter/312954672" }));
        });
    }

    [Test]
    public async Task GetOrganization_should_return_LookupFailed_when_sub_entity_is_not_found_and_entity_lookup_fails()
    {
        _mockHttpMessageHandler.SetResponder(req => req.RequestUri!.AbsolutePath.Contains("/underenheter/")
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await _brregClient.GetOrganization("123456789");

        Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.LookupFailed));
    }

    [Test]
    public async Task GetOrganization_should_return_LookupFailed_when_main_entity_of_sub_entity_lookup_fails()
    {
        _mockHttpMessageHandler.SetResponder(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("/underenheter/"))
            {
                var sub = JsonSerializer.Serialize(new
                {
                    organisasjonsnummer = "111111111",
                    navn = "Underenhet",
                    organisasjonsform = new { kode = "BEDR", beskrivelse = "Bedrift" },
                    overordnetEnhet = "987654321"
                });
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(sub) };
            }
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        var result = await _brregClient.GetOrganization("111111111");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.LookupFailed));
            Assert.That(result.Type, Is.EqualTo(EntityType.Sub));
            Assert.That(result.OverordnetOrganisasjonsform, Is.Null);
        });
    }

    [Test]
    public async Task GetOrganization_should_return_OrganizationDeleted_when_sub_entity_deletion_date_is_in_past()
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

        var result = await _brregClient.GetOrganization("123456789");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.OrganizationDeleted));
            Assert.That(result.Type, Is.EqualTo(EntityType.Sub));
            Assert.That(result.Navn, Is.EqualTo("Slettet bedrift"));
        });
    }

    [Test]
    public async Task GetOrganization_should_return_OrganizationDeleted_when_main_entity_deletion_date_is_in_past()
    {
        var pastDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
        _mockHttpMessageHandler.SetResponder(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("/underenheter"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var entity = JsonSerializer.Serialize(new
            {
                organisasjonsnummer = "987654321",
                navn = "Slettet AS",
                slettedato = pastDate
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(entity) };
        });

        var result = await _brregClient.GetOrganization("987654321");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.OrganizationDeleted));
            Assert.That(result.Type, Is.EqualTo(EntityType.Main));
            Assert.That(result.Underenheter, Is.Null);
        });
    }

    [Test]
    public async Task GetOrganization_should_return_Ok_sub_entity_when_deletion_date_is_in_future()
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

        var result = await _brregClient.GetOrganization("123456789");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.Ok));
            Assert.That(result.Type, Is.EqualTo(EntityType.Sub));
        });
    }

    [Test]
    public async Task GetOrganization_should_return_main_entity_with_sub_entities()
    {
        _mockHttpMessageHandler.SetResponder(BuildEntityWithSubEntitiesResponder(subEntityCount: 2));

        var result = await _brregClient.GetOrganization("312954672");

        Assert.Multiple(() =>
        {
            Assert.That(result.LookupStatus, Is.EqualTo(LookupStatus.Ok));
            Assert.That(result.Type, Is.EqualTo(EntityType.Main));
            Assert.That(result.Underenheter, Has.Count.EqualTo(2));
        });
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> BuildEntityWithSubEntitiesResponder(
        int subEntityCount, HttpStatusCode subEntityStatus = HttpStatusCode.NotFound)
    {
        return req =>
        {
            // sub entity list lookup: /underenheter?overordnetEnhet=...
            if (!string.IsNullOrEmpty(req.RequestUri!.Query))
            {
                var subEntities = Enumerable.Range(0, subEntityCount).Select(i => new
                {
                    organisasjonsnummer = $"31582918{i}",
                    navn = $"Underenhet {i}",
                    organisasjonsform = new { kode = "BEDR", beskrivelse = "Bedrift" }
                });
                var list = JsonSerializer.Serialize(new { _embedded = new { underenheter = subEntities } });
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(list) };
            }

            // the org itself is not a sub entity
            if (req.RequestUri.AbsolutePath.Contains("/underenheter/"))
                return new HttpResponseMessage(subEntityStatus);

            // active main entity
            var entity = JsonSerializer.Serialize(new
            {
                organisasjonsnummer = "312954672",
                navn = "Hovedenhet AS"
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(entity) };
        };
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _mockHttpMessageHandler.Dispose();
        _memoryCache.Dispose();
    }
}
