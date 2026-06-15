using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DsbNorge.A3Forms.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DsbNorge.A3Forms.Clients.Brreg;

public class BrregClient(
    HttpClient client,
    ILogger<IBrregClient> logger,
    IMemoryCache memoryCache) : IBrregClient
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    [Obsolete("GetOrg is deprecated. Use GetEntity instead.")]
    public async Task<BrregOrg?> GetOrg(string organizationNumber)
    {
        return await GetEntity(organizationNumber);
    }

    public async Task<BrregOrg?> GetEntity(string organizationNumber)
    {
        try
        {
            if (memoryCache.TryGetValue(organizationNumber, out BrregOrg? cachedOrg))
            {
                logger.LogInformation("Retrieved organization {OrganizationNumber} from cache", organizationNumber);
                return cachedOrg;
            }

            var path = $"/enhetsregisteret/api/enheter/{organizationNumber}";
            logger.LogInformation("Retrieving organization {OrganizationNumber} from BRREG, url: {Url}", organizationNumber, client.BaseAddress + path);

            var response = await client.GetAsync(path);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Failed to retrieve organization {OrganizationNumber}, status code: {StatusCode}", organizationNumber, response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadAsStringAsync();
            var brregOrg = JsonSerializer.Deserialize<BrregOrg>(data, _serializerOptions);

            if (brregOrg == null)
            {
                logger.LogWarning("Failed to deserialize organization {OrganizationNumber} - received null from BRREG API", organizationNumber);
                return brregOrg;
            }

            memoryCache.Set(organizationNumber, brregOrg, _cacheOptions);
            logger.LogInformation("Successfully retrieved and cached organization {OrganizationNumber}", organizationNumber);

            return brregOrg;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving organization {OrganizationNumber}: {ErrorMessage}", organizationNumber, e.Message);
            return null;
        }
    }

    public async Task<BrregSubEntity?> GetSubEntity(string organizationNumber)
    {
        try
        {
            if (memoryCache.TryGetValue($"sub-{organizationNumber}", out BrregSubEntity? cachedSubEntity))
            {
                logger.LogInformation("Retrieved sub entity {OrganizationNumber} from cache", organizationNumber);
                return cachedSubEntity;
            }

            var path = $"/enhetsregisteret/api/underenheter/{organizationNumber}";
            logger.LogInformation("Retrieving sub entity {OrganizationNumber} from BRREG, url: {Url}", organizationNumber, client.BaseAddress + path);

            var response = await client.GetAsync(path);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Failed to retrieve sub entity {OrganizationNumber}, status code: {StatusCode}", organizationNumber, response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadAsStringAsync();
            var brregSubEntity = JsonSerializer.Deserialize<BrregSubEntity>(data, _serializerOptions);

            if (brregSubEntity == null)
            {
                logger.LogWarning("Failed to deserialize sub entity {OrganizationNumber} - received null from BRREG API", organizationNumber);
                return brregSubEntity;
            }

            memoryCache.Set($"sub-{organizationNumber}", brregSubEntity, _cacheOptions);
            logger.LogInformation("Successfully retrieved and cached sub entity {OrganizationNumber}", organizationNumber);

            return brregSubEntity;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving sub entity {OrganizationNumber}: {ErrorMessage}", organizationNumber, e.Message);
            return null;
        }
    }

    public async Task<BrregOrgForm?> GetOrgForm(string code)
    {
        try
        {
            var orgFormPath = $"/enhetsregisteret/api/organisasjonsformer/{code}";
            logger.LogInformation("Retrieving organization form {Code} from BRREG, url: {Url}", code, client.BaseAddress + orgFormPath);

            var res = await client.GetAsync(orgFormPath);

            if (res.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Failed to retrieve organization form {Code}, status code: {StatusCode}", code, res.StatusCode);
                return null;
            }

            var data = await res.Content.ReadAsStringAsync();
            var brregOrgForm = JsonSerializer.Deserialize<BrregOrgForm>(data, _serializerOptions);

            return brregOrgForm;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving organization form {Code}: {ErrorMessage}", code, e.Message);
            return null;
        }
    }

    public async Task<BrregOrganizationStatus> GetOrganizationStatus(string organizationNumber)
    {
        try
        {
            // 1. Lookup sub entity. If active or deleted, we are done.
            var subEntityPath = $"/enhetsregisteret/api/underenheter/{organizationNumber}";
            logger.LogInformation("Retrieving sub entity {OrganizationNumber} from BRREG, url: {Url}", organizationNumber, client.BaseAddress + subEntityPath);
            var res = await client.GetAsync(subEntityPath);

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
            logger.LogInformation("Sub entity not found, or lookup failed. Trying entity, url: {Url}", client.BaseAddress + entityPath);
            var result = await client.GetAsync(entityPath);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("Organization {OrganizationNumber} not found in BRREG. Wonder how user got logged in with this org!", organizationNumber);
                return BrregOrganizationStatus.NotFound;
            }
            if (result.IsSuccessStatusCode)
            {
                var resString = await result.Content.ReadAsStringAsync();
                var brregResponse = JsonSerializer.Deserialize<BrregOrganization>(resString, _serializerOptions);
                var deletionDate = brregResponse != null ? GetDeletionDate(brregResponse) : null;
                if (deletionDate != null && deletionDate < DateTime.Now)
                {
                    logger.LogWarning("Organization {OrganizationNumber} has deletion date {DeletionDate} in BRREG. Wonder how user got logged in with this org!", organizationNumber, deletionDate?.ToString("yyyy-MM-dd"));
                    return BrregOrganizationStatus.Deleted;
                }
            }
            else
            {
                logger.LogError("Retrieving organization as entity failed with status code {StatusCode}", res.StatusCode);
                return BrregOrganizationStatus.LookupFailed;
            }

            // 3. If entity is active, check if it has sub entities.
            var path = $"/enhetsregisteret/api/underenheter/?overordnetEnhet={organizationNumber}&size=500";
            logger.LogInformation("Checking if organization {OrganizationNumber} has sub entities, url: {Url}", organizationNumber, client.BaseAddress + path);
            var hasSubRes = await client.GetAsync(path);

            if (!hasSubRes.IsSuccessStatusCode)
            {
                logger.LogError("Failed looking up sub entities for existing entity org {OrganizationNumber}, status code: {StatusCode}", organizationNumber, hasSubRes.StatusCode);
                return BrregOrganizationStatus.LookupFailed;
            }
            var sub = await hasSubRes.Content.ReadAsStringAsync();

            var subResponse = JsonSerializer.Deserialize<BrregSubResponse>(sub, _serializerOptions);
            return subResponse?.Embedded?.SubEntities?.Count > 0 ? BrregOrganizationStatus.EntityWithSubEntities : BrregOrganizationStatus.EntityWithoutSubEntities;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Retrieving organization {OrganizationNumber} failed: {ErrorMessage}", organizationNumber, e.Message);
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
    
    public async Task<BrregOrgForm?> GetLegalOrgForm(string organizationNumber)
    {
        // Sub entitites always have organisasjonsform BEDR/AAFY;
        // the legal form (ENK/AS/NUF/...) lives on the main entity.
        // Resolve from the parent when needed.
        var subEntity = await GetSubEntity(organizationNumber);
        var mainEntityNumber = string.IsNullOrWhiteSpace(subEntity?.OverordnetEnhet)
            ? organizationNumber
            : subEntity.OverordnetEnhet;

        var entity = await GetEntity(mainEntityNumber);
        return entity?.Organisasjonsform;
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