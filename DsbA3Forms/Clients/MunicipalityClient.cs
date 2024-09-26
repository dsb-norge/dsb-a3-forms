using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DsbA3Forms.Clients;
using DsbA3Forms.Models.Address;
using DsbA3Forms.Models.Address;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DsbA3Forms.Clients;

public class MunicipalityClient : IMunicipalityClient
{
    HttpClient _client;
    ILogger<IMunicipalityClient> _logger;
    JsonSerializerOptions _serializerOptions;
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _cacheOptions;


    public MunicipalityClient(HttpClient client, ILogger<IMunicipalityClient> logger, IMemoryCache memoryCache)
    {
        _logger = logger;
        _client = client;
        _client.BaseAddress = new Uri("https://ws.geonorge.no/kommuneinfo/v1/");

        _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };


        _memoryCache = memoryCache;
        _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
    }

    public async Task<List<Municipality>> GetMunicipalities()
    {
        string uniqueCacheKey = "municipalities";
        if (_memoryCache.TryGetValue(uniqueCacheKey, out List<Municipality> municipalities))
        {
            return municipalities;
        }

        string query = "kommuner";
        
        HttpResponseMessage res = await _client.GetAsync(query);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Retrieving municipalities failed with status code {statusCode}",
                res.StatusCode);
            return null;
        }

        string resString = await res.Content.ReadAsStringAsync();

        List<Municipality> municipalityResponse =
            JsonSerializer.Deserialize<List<Municipality>>(resString, _serializerOptions);

        _memoryCache.Set(uniqueCacheKey, municipalityResponse, _cacheOptions);

        return municipalityResponse;
    }
}
