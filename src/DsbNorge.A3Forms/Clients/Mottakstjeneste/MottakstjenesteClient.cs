using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Clients.Mottakstjeneste;

public class MottakstjenesteClient(
    HttpClient client,
    ILogger<IMottakstjenesteClient> logger,
    IMemoryCache memoryCache) : IMottakstjenesteClient
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    public async Task<List<Country>> GetCountries(string formName)
    {
        logger.LogInformation("Retrieving countries");
        const string uniqueCacheKey = "countries";
        if (memoryCache.TryGetValue(uniqueCacheKey, out List<Country>? countries))
        {
            return countries ?? [];
        }

        try
        {
            var res = await client.GetAsync($"api/external/altinn/internet/{formName}/nationalities");

            if (!res.IsSuccessStatusCode)
            {
                logger.LogError($"Retrieving countries failed with status code {res.StatusCode}");
            }

            var resString = await res.Content.ReadAsStringAsync();
            
            countries = JsonSerializer.Deserialize<List<Country>>(resString, _serializerOptions) ?? [];
            memoryCache.Set(uniqueCacheKey, countries, _cacheOptions);
            return countries;
        }
        catch (Exception e)
        {
            logger.LogError($"Exception thrown when retrieving countries: {e.Message}");
            return
            [
                new Country
                {
                    Name = "invalid"
                }
            ];
        }
    }
}
