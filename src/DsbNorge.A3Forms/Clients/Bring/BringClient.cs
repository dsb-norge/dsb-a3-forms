using System.Text.Json;
using DsbNorge.A3Forms.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DsbNorge.A3Forms.Clients.Bring
{
    public class BringClient : IBringClient
    {
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly MemoryCacheEntryOptions _cacheOptions;
        private readonly ILogger<IBringClient> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly HttpClient _client;

        public BringClient(
            HttpClient client,
            ILogger<IBringClient> logger,
            IMemoryCache memoryCache
        )
        {
            _logger = logger;
            _client = client;
            _client.BaseAddress = new Uri("https://api.bring.com/");
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

        public async Task<string> GetCity(string postalCode)
        {
            var uniqueCacheKey = $"city-{postalCode}";
            if (_memoryCache.TryGetValue(uniqueCacheKey, out string city))
            {
                return city;
            }

            var query = $"shippingguide/api/postalCode.json?pnr={postalCode}";

            var res = await _client.GetAsync(query);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Retrieving city for postal code: {postalCode} failed with status code {statusCode}",
                    postalCode,
                    res.StatusCode);
                return null;
            }

            var resString = await res.Content.ReadAsStringAsync();

            var cityResponse = JsonSerializer.Deserialize<BringCityResponse>(resString, _serializerOptions);
            var cityName = cityResponse.Valid ? cityResponse.Result : "";

            _memoryCache.Set(uniqueCacheKey, cityName, _cacheOptions);

            return cityName;
        }
    }
}
