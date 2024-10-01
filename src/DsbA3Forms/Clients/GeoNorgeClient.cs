using System.Text.Json;
using Altinn.App.Core.Extensions;
using DsbA3Forms.Models.Address;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DsbA3Forms.Clients;

public class GeoNorgeClient : IGeoNorgeClient
{
    private readonly HttpClient _client;
    private readonly ILogger<IGeoNorgeClient> _logger;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public GeoNorgeClient(HttpClient client, ILogger<IGeoNorgeClient> logger, IMemoryCache memoryCache)
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
            return new List<GeoNorgeAdresse>();
        }

        var searchWithWildcards = Wildcardify(searchString);
        var query = $"adresser/v1/sok?treffPerSide={hitsPerPage}&sok={searchWithWildcards}";
        var res = await _client.GetAsync(query);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Retrieving addresses for search string '{searchString}' failed with status code {statusCode}",
                searchString, res.StatusCode);
            return new List<GeoNorgeAdresse>();
        }

        try
        {
            var resString = await res.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<GeoNorgeAdresseRespons>(resString, _serializerOptions);
            return response?.Adresser ?? new List<GeoNorgeAdresse>();
        }
        catch (Exception e)
        {
            _logger.LogError("Exception thrown when retrieving addresses for search string '{searchString}': {message}", searchString, e.Message);
            return new List<GeoNorgeAdresse>();
        }
    }

    public async Task<List<Municipality>> GetMunicipalities()
    {
        string uniqueCacheKey = "municipalities";
        if (_memoryCache.TryGetValue(uniqueCacheKey, out List<Municipality> cachedMunicipalities))
        {
            return cachedMunicipalities;
        }

        var query = "kommuneinfo/v1/kommuner";
        var res = await _client.GetAsync(query);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Retrieving municipalities failed with status code {statusCode}", res.StatusCode);
            return new List<Municipality>();
        }

        try
        {
            var resString = await res.Content.ReadAsStringAsync();
            var municipalityResponse = JsonSerializer.Deserialize<List<Municipality>>(resString, _serializerOptions);

            if (municipalityResponse != null)
            {
                _memoryCache.Set(uniqueCacheKey, municipalityResponse, _cacheOptions);
            }

            return municipalityResponse ?? new List<Municipality>();
        }
        catch (Exception e)
        {
            _logger.LogError("Exception thrown when retrieving municipalities: {message}", e.Message);
            return new List<Municipality>();
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
