using System.Text.Json;
using Altinn.App.Core.Extensions;
using DsbA3Forms.Models.Address;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DsbA3Forms.Clients
{
    public class GeoNorgeClient : IGeoNorgeClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<IGeoNorgeClient> _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        public GeoNorgeClient(HttpClient client, ILogger<IGeoNorgeClient> logger)
        {
            _logger = logger;
            _client = client;
            _client.BaseAddress = new Uri("https://ws.geonorge.no/");
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
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
                    searchString,
                    res.StatusCode);
                return [];
            }

            try
            {
                var resString = await res.Content.ReadAsStringAsync();
                var response = JsonSerializer.Deserialize<GeoNorgeAdresseRespons>(resString, _serializerOptions);
                return response.Adresser;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception thrown when retrieving addresses for search string '{searchString}': {message}", searchString, e.Message);
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
}