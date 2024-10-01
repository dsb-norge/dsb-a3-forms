using DsbNorge.A3Forms.Clients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
[Category("Integration")]
public class GeoNorgeClientIT
{
    private IGeoNorgeClient _geoNorgeClient;

    [SetUp]
    public void Setup()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://ws.geonorge.no/kommuneinfo/v1/")
        };
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = new LoggerFactory().CreateLogger<IGeoNorgeClient>();

        _geoNorgeClient = new GeoNorgeClient(httpClient, logger, memoryCache);
    }

    [Test]
    public async Task GetMunicipalities_should_return_result()
    {
        var municipalities = await _geoNorgeClient.GetMunicipalities();

        Assert.IsNotNull(municipalities);
        Assert.IsNotEmpty(municipalities);
    }

    [Test]
    public async Task GetMunicipalities_should_contain_Oslo()
    {
        var municipalities = await _geoNorgeClient.GetMunicipalities();

        Assert.IsTrue(municipalities.Exists(m => m.KommunenavnNorsk == "Oslo"));
    }

    [Test]
    public async Task GetAddresses_should_contain_correct_address()
    {
        var searchString = "Rambergveien 9";

        var addresses = await _geoNorgeClient.GetAddresses(searchString, 5);

        Assert.IsTrue(addresses.Any(a => a.Postnummer == "3115"));
    }
}