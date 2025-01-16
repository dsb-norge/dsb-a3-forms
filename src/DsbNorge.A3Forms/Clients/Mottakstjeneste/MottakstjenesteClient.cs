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

    public async Task<List<Nationality>> GetNationalities(string formName)
    {
        logger.LogInformation("Retrieving nationalities");
        const string uniqueCacheKey = "nationalities";
        if (memoryCache.TryGetValue(uniqueCacheKey, out List<Nationality>? nationalities))
        {
            return nationalities ?? [];
        }

        try
        {
            var res = await client.GetAsync($"api/external/altinn/internet/{formName}/nationalities");

            if (!res.IsSuccessStatusCode)
            {
                logger.LogError($"Retrieving nationalities failed with status code {res.StatusCode}");
            }

            var resString = await res.Content.ReadAsStringAsync();
            
            nationalities = JsonSerializer.Deserialize<List<Nationality>>(resString, _serializerOptions) ?? [];
            memoryCache.Set(uniqueCacheKey, nationalities, _cacheOptions);
            return nationalities;
        }
        catch (Exception e)
        {
            logger.LogError($"Exception thrown when retrieving nationalities: {e.Message}");
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
