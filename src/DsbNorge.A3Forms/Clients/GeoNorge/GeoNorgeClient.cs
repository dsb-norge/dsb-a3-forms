using System.Text.Json;
using Altinn.App.Core.Extensions;
using DsbNorge.A3Forms.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

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

    public async Task<List<GeoNorgeAdresse>> GetAddresses(string searchString, int? radius, int hitsPerPage)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return [];
        }

        string query;
        string searchType;
        
        if (radius is > 0)
        {
            _logger.LogInformation("Searching for addresses by map lat & lon");
            var coordinates = searchString.Split(',');
            query = $"adresser/v1/punktsok?lat={coordinates[0]}&lon={coordinates[1]}&radius={radius}&treffPerSide={hitsPerPage}";
            searchType = "MAP COORDINATES";
        }
        
        else
        {
            _logger.LogInformation("Searching for addresses by address input");
            var searchWithWildcards = Wildcardify(searchString);
            query = $"adresser/v1/sok?treffPerSide={hitsPerPage}&sok={searchWithWildcards}";
            searchType = "ADDRESS INPUT";
        }
        
        return await ExecuteAddressQuery(query, searchString, searchType);
    }
    
    private async Task<List<GeoNorgeAdresse>> ExecuteAddressQuery(string query, string searchInput, string searchType)
    {
        var res = await _client.GetAsync(query);
        
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Retrieving addresses by {searchType}: '{searchInput}' failed with status code {statusCode}",
                searchType, searchInput, res.StatusCode);
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
            _logger.LogError("Exception thrown when retrieving addresses by {searchType}: '{searchInput}': {message}", 
                searchType, searchInput, e.Message);
            return [];
        }
    }

    public async Task<List<Municipality>> GetMunicipalities()
    {
        const string uniqueCacheKey = "municipalities";
        if (_memoryCache.TryGetValue(uniqueCacheKey, out List<Municipality>? cachedMunicipalities))
        {
            return cachedMunicipalities ?? [];
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
                addOtherRegions(municipalityResponse);
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

    // adds other regions as per https://snl.no/kommunenummer
    private static void addOtherRegions(List<Municipality> municipalities)
    {
        municipalities.Add(new Municipality { Kommunenummer = "2100", KommunenavnNorsk = "Svalbard" });
        municipalities.Add(new Municipality { Kommunenummer = "2211", KommunenavnNorsk = "Jan Mayen" });
        municipalities.Add(new Municipality { Kommunenummer = "2311", KommunenavnNorsk = "Sokkelen syd for 62° N" });
        municipalities.Add(new Municipality { Kommunenummer = "2321", KommunenavnNorsk = "Sokkelen nord for 62° N" });
        municipalities.Add(new Municipality { Kommunenummer = "2399", KommunenavnNorsk = "Sokkelen, uspesifisert" });
        municipalities.Add(new Municipality { Kommunenummer = "2411", KommunenavnNorsk = "Luftrom under og lik 1000 moh." });
        municipalities.Add(new Municipality { Kommunenummer = "2412", KommunenavnNorsk = "Luftrom over 1000 moh." });
        municipalities.Add(new Municipality { Kommunenummer = "2499", KommunenavnNorsk = "Luftrom uspesifisert" });
        municipalities.Add(new Municipality { Kommunenummer = "2511", KommunenavnNorsk = "Norske ambassader i utlandet" });
        municipalities.Add(new Municipality { Kommunenummer = "2599", KommunenavnNorsk = "Utlandet, uspesifisert" });
        municipalities.Add(new Municipality { Kommunenummer = "2611", KommunenavnNorsk = "Havområder innenfor sokkelen" });
        municipalities.Add(new Municipality { Kommunenummer = "2612", KommunenavnNorsk = "Havområder utenfor sokkelen" });
        municipalities.Add(new Municipality { Kommunenummer = "2699", KommunenavnNorsk = "Havområder, uspesifisert" });
        municipalities.Add(new Municipality { Kommunenummer = "2799", KommunenavnNorsk = "Bouvetøya, Peter I Øy og Dronning Mauds Land" });
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
