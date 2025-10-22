using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DsbNorge.A3Forms.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DsbNorge.A3Forms.Clients.Brreg;

public class BrregClient : IBrregClient
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly ILogger<IBrregClient> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly HttpClient _client;

    public BrregClient(
        HttpClient client,
        ILogger<IBrregClient> logger,
        IMemoryCache memoryCache
        )
    {
        _logger = logger;
        _client = client;
        _client.BaseAddress = new Uri("https://data.brreg.no/");
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
    
    public async Task<BrregOrg?> GetOrg(string orgNumber)
    {
        try
        {
            if (_memoryCache.TryGetValue(orgNumber, out BrregOrg? cachedOrg))
            {
                _logger.LogInformation($"Retrieved organization {orgNumber} from cache");
                return cachedOrg;
            }

            var path = $"/enhetsregisteret/api/enheter/{orgNumber}";
            _logger.LogInformation($"Retrieving organization {orgNumber} from BRREG, url: {_client.BaseAddress + path}");

            var response = await _client.GetAsync(path);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning($"Failed to retrieve organization {orgNumber}, status code: {response.StatusCode}");
                return null;
            }

            var data = await response.Content.ReadAsStringAsync();
            var brregOrg = JsonSerializer.Deserialize<BrregOrg>(data, _serializerOptions);

            if (brregOrg == null) return brregOrg;
            
            _memoryCache.Set(orgNumber, brregOrg, _cacheOptions);
            _logger.LogInformation($"Successfully retrieved and cached organization {orgNumber}");

            return brregOrg;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error retrieving organization {orgNumber}: {e.Message}");
            return null;
        }
    }

    public async Task<BrregOrgForm?> GetOrgForm(string code)
    {
        try
        {
            var orgFormPath = $"/enhetsregisteret/api/organisasjonsformer/{code}";
            _logger.LogInformation($"Retrieving organization form {code} from BRREG, url: {_client.BaseAddress + orgFormPath}");

            var res = await _client.GetAsync(orgFormPath);

            if (res.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning($"Failed to retrieve organization form {code}, status code: {res.StatusCode}");
                return null;
            }

            var data = await res.Content.ReadAsStringAsync();
            var brregOrgForm = JsonSerializer.Deserialize<BrregOrgForm>(data, _serializerOptions);

            return brregOrgForm;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error retrieving organization form {code}: {e.Message}");
            return null;
        }
    }

    public async Task<BrregOrganizationStatus> GetOrganizationStatus(string organizationNumber)
    {
        try
        {
            // 1. Lookup sub entity. If active or deleted, we are done.
            var subEntityPath = $"/enhetsregisteret/api/underenheter/{organizationNumber}";
            _logger.LogInformation($"Retrieving sub entity {organizationNumber} from BRREG, url: {_client.BaseAddress + subEntityPath}");
            var res = await _client.GetAsync(subEntityPath);

            if (res.IsSuccessStatusCode)
            {
                var resString = await res.Content.ReadAsStringAsync();
                var brregResponse = JsonSerializer.Deserialize<BrregOrganization>(resString, _serializerOptions);
                var deletionDate = brregResponse != null ? GetDeletionDate(brregResponse) : null;
                if (deletionDate != null && deletionDate < DateTime.Now)
                {
                    return BrregOrganizationStatus.Deleted;
                }
                return BrregOrganizationStatus.SubEntity;
            }

            // 2. Lookup entity. If deleted or not found, we are done.
            var entityPath = $"/enhetsregisteret/api/enheter/{organizationNumber}";
            _logger.LogInformation($"Sub entity not found, or lookup failed. Trying entity, url: {_client.BaseAddress + entityPath}");
            var result = await _client.GetAsync(entityPath);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Organization {organizationNumber} not found in BRREG. Wonder how user got logged in with this org!");
                return BrregOrganizationStatus.NotFound;
            }
            if (result.IsSuccessStatusCode)
            {
                var resString = await result.Content.ReadAsStringAsync();
                var brregResponse = JsonSerializer.Deserialize<BrregOrganization>(resString, _serializerOptions);
                var deletionDate = brregResponse != null ? GetDeletionDate(brregResponse) : null;
                if (deletionDate != null && deletionDate < DateTime.Now)
                {
                    _logger.LogWarning($"Organization {organizationNumber} has deletion date {deletionDate:yyyy-MM-dd)} in BRREG . Wonder how user got logged in with this org!");
                    return BrregOrganizationStatus.Deleted;
                }
            }
            else
            {
                _logger.LogError($"Retrieving organization as entity failed with status code {res.StatusCode}");
                return BrregOrganizationStatus.LookupFailed;
            }

            // 3. If entity is active, check if it has sub entities.
            var path = $"/enhetsregisteret/api/underenheter/?overordnetEnhet={organizationNumber}&size=500";
            _logger.LogInformation($"Checking if organization {organizationNumber} has sub entities, url: {_client.BaseAddress + path}");
            var hasSubRes = await _client.GetAsync(path);

            if (!hasSubRes.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed looking up sub entities for existing entity org {organizationNumber}, status code: {hasSubRes.StatusCode}");
                return BrregOrganizationStatus.LookupFailed;
            }
            var sub = await hasSubRes.Content.ReadAsStringAsync();

            var subResponse = JsonSerializer.Deserialize<BrregSubResponse>(sub, _serializerOptions);
            return subResponse?.Embedded?.SubEntities?.Count > 0 ? BrregOrganizationStatus.EntityWithSubEntities : BrregOrganizationStatus.EntityWithoutSubEntities;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Retrieving organization {organizationNumber} failed: : {e.Message}");
            return BrregOrganizationStatus.LookupFailed;
        }
    }

    private static DateTime? GetDeletionDate(BrregOrganization brregOrganization)
    {
        return DateTime.TryParseExact(brregOrganization.DeletionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? parsedDate
            : null;
    }

    private class BrregOrganization(string organizationNumber, string name)
    {
        [JsonPropertyName("organisasjonsnummer")] public string OrganizationNumber { get; init; } = organizationNumber;
        [JsonPropertyName("navn")] public string Name { get; init; } = name;
        [JsonPropertyName("slettedato")] public string? DeletionDate { get; init; }
    }

    private class BrregSubResponse
    {
        [JsonPropertyName("_embedded")] public EmbeddedSubEntities? Embedded { get; init; }
    }

    private class EmbeddedSubEntities(List<BrregOrganization>? subEntities)
    {
        [JsonPropertyName("underenheter")] public List<BrregOrganization>? SubEntities { get; } = subEntities;
    }

}

public enum BrregOrganizationStatus
{
    SubEntity,
    EntityWithoutSubEntities,
    EntityWithSubEntities,
    NotFound,
    Deleted,
    LookupFailed,
}