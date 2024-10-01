using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DsbNorge.A3Forms.Clients;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
[Category("Integration")]
public class BringClientIT
{
    private IBringClient _bringClient;

    [SetUp]
    public void Setup()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.bring.com/")
        };
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = new LoggerFactory().CreateLogger<IBringClient>();

        _bringClient = new BringClient(httpClient, logger, memoryCache);
    }

    [Test]
    public async Task GetCity_should_be_oslo()
    {
        var postalCode = "0182";

        var city = await _bringClient.GetCity(postalCode);

        Assert.AreEqual("OSLO", city);
    }
}