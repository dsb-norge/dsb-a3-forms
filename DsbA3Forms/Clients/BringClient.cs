using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DsbA3Forms.Clients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace dsb_a3_forms.DsbA3Forms.Clients
{
    public class BringClient : IBringClient
    {
        HttpClient _client;
        ILogger<IBringClient> _logger;
        JsonSerializerOptions _serializerOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheOptions;


        public BringClient(HttpClient client, ILogger<IBringClient> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _client = client;
            _client.BaseAddress = new Uri("https://api.bring.com/");

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

        public async Task<string> GetCity(string postalCode)
        {
            string uniqueCacheKey = $"city-{postalCode}";
            if (_memoryCache.TryGetValue(uniqueCacheKey, out string city))
            {
                return city;
            }

            string query = $"shippingguide/api/postalCode.json?pnr={postalCode}";

            HttpResponseMessage res = await _client.GetAsync(query);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Retrieving city for postal code: {postalCode} failed with status code {statusCode}",
                    postalCode,
                    res.StatusCode);
                return null;
            }

            string resString = await res.Content.ReadAsStringAsync();

            CityResponse cityResponse = JsonSerializer.Deserialize<CityResponse>(resString, _serializerOptions);
            string cityName = cityResponse.Valid ? cityResponse.Result : "";

            _memoryCache.Set(uniqueCacheKey, cityName, _cacheOptions);

            return cityName;
        }
    }

    class CityResponse
    {
        public string Result { get; set; }
        public bool Valid { get; set; }
        public string PostalCodeType { get; set; }
    }
}
