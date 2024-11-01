using System.Text.Json;
using Altinn.App.Core.Extensions;
using DsbNorge.A3Forms.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DsbNorge.A3Forms.Clients.GeoNorge;

public class GeoNorgeClient : IGeoNorgeClient
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly ILogger<IGeoNorgeClient> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly HttpClient _client;

    public GeoNorgeClient(
        HttpClient client,
        ILogger<IGeoNorgeClient> logger,
        IMemoryCache memoryCache
        )
    {
        _logger = logger;
        _client = client;
        _client.BaseAddress = new Uri("https://ws.geonorge.no/");
        _memoryCache = memoryCache;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
    }

    public async Task<List<GeoNorgeAdresse>> GetAddresses(string searchString, int hitsPerPage)
    {
        if (searchString.IsNullOrEmpty())
        {
            return [];
        }

        var searchWithWildcards = Wildcardify(searchString);
        var query = $"adresser/v1/sok?treffPerSide={hitsPerPage}&sok={searchWithWildcards}";
        var res = await _client.GetAsync(query);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Retrieving addresses for search string '{searchString}' failed with status code {statusCode}",
                searchString, res.StatusCode);
            return [];
        }

        try
        {
            var resString = await res.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<GeoNorgeAdresseRespons>(resString, _serializerOptions);
            return response?.Adresser ?? [];
        }
        catch (Exception e)
        {
            _logger.LogError("Exception thrown when retrieving addresses for search string '{searchString}': {message}", searchString, e.Message);
            return [];
        }
    }

    public async Task<List<Municipality>> GetMunicipalities()
    {
        const string uniqueCacheKey = "municipalities";
        if (_memoryCache.TryGetValue(uniqueCacheKey, out List<Municipality> cachedMunicipalities))
        {
            return cachedMunicipalities;
        }

        var res = await _client.GetAsync("kommuneinfo/v1/kommuner");

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Retrieving municipalities failed with status code {statusCode}", res.StatusCode);
            return [];
        }

        try
        {
            var resString = await res.Content.ReadAsStringAsync();
            var municipalityResponse = JsonSerializer.Deserialize<List<Municipality>>(resString, _serializerOptions);

            if (municipalityResponse != null)
            {
                _memoryCache.Set(uniqueCacheKey, municipalityResponse, _cacheOptions);
            }

            return municipalityResponse ?? [];
        }
        catch (Exception e)
        {
            _logger.LogError("Exception thrown when retrieving municipalities: {message}", e.Message);
            return [];
        }
    }

    private static string Wildcardify(string searchString)
    {
        if (searchString.DoesNotContain(' '))
        {
            return searchString + "*";
        }

        var modifiedStrings = searchString.Split(' ').Select(s => s + "*");
        return string.Join(" ", modifiedStrings);
    }
}
