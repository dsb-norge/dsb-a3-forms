using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Clients.Mottakstjeneste;

public class MottakstjenesteClient : IMottakstjenesteClient
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<IMottakstjenesteClient> _logger;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly IMemoryCache _memoryCache;
    private readonly HttpClient _client;
    
    public MottakstjenesteClient(
        HttpClient client,
        ILogger<IMottakstjenesteClient> logger,
        IConfiguration configuration,
        IMemoryCache memoryCache
    )
    {
        _logger = logger;
        _client = client;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _memoryCache = memoryCache;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
    }
    
    public async Task<List<Nationality>> GetNationalities(string formName)
    {
        _logger.LogInformation("Retrieving nationalities");
        const string uniqueCacheKey = "nationalities";
        if (_memoryCache.TryGetValue(uniqueCacheKey, out List<Nationality> nationalities))
        {
            return nationalities;
        }

        try
        {
            var res = await _client.GetAsync($"/mottakstjeneste/api/external/altinn/internet/{formName}/nationalities");

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError($"Retrieving nationalities failed with status code {res.StatusCode}");
            }

            var resString = await res.Content.ReadAsStringAsync();
            
            nationalities = JsonSerializer.Deserialize<List<Nationality>>(resString, _serializerOptions);
            _memoryCache.Set(uniqueCacheKey, nationalities, _cacheOptions);
            return nationalities;
        }
        catch (Exception e)
        {
            _logger.LogError($"Exception thrown when retrieving nationalities: {e.Message}");
            return
            [
                new Nationality
                {
                    Name = "invalid"
                }
            ];
        }
    }
}
