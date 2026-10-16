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

    public async Task<BrregOrganization> GetOrganization(string organizationNumber)
    {
        // 1. Lookup sub entity.
        var (subStatus, subEntity) = await Fetch<SubEntityResponse>(
            $"/enhetsregisteret/api/underenheter/{organizationNumber}", $"sub-{organizationNumber}", "sub entity", organizationNumber);

        if (subStatus == LookupStatus.Ok)
        {
            var organization = ToOrganization(subEntity!, null);
            if (organization.LookupStatus == LookupStatus.OrganizationDeleted || string.IsNullOrWhiteSpace(subEntity!.OverordnetEnhet))
            {
                return organization;
            }

            // The legal form (ENK/AS/NUF/...) lives on the main entity, sub entities always have BEDR/AAFY.
            var (parentStatus, parent) = await Fetch<EntityResponse>(
                $"/enhetsregisteret/api/enheter/{subEntity.OverordnetEnhet}", subEntity.OverordnetEnhet, "organization", subEntity.OverordnetEnhet);
            if (parentStatus != LookupStatus.Ok)
            {
                logger.LogError("Failed to retrieve main entity {ParentNumber} of sub entity {OrganizationNumber}", subEntity.OverordnetEnhet, organizationNumber);
                organization.LookupStatus = LookupStatus.LookupFailed;
                return organization;
            }

            organization.OverordnetOrganisasjonsform = parent!.Organisasjonsform;
            return organization;
        }

        if (subStatus == LookupStatus.LookupFailed)
        {
            return new BrregOrganization { Organisasjonsnummer = organizationNumber, LookupStatus = LookupStatus.LookupFailed };
        }

        // 2. Not a sub entity. Lookup entity.
        logger.LogInformation("Sub entity {OrganizationNumber} not found. Trying entity", organizationNumber);
        var (entityStatus, entity) = await Fetch<EntityResponse>(
            $"/enhetsregisteret/api/enheter/{organizationNumber}", organizationNumber, "organization", organizationNumber);

        if (entityStatus != LookupStatus.Ok)
        {
            if (entityStatus == LookupStatus.OrganizationNotFound)
            {
                logger.LogWarning("Organization {OrganizationNumber} not found in BRREG", organizationNumber);
            }
            return new BrregOrganization { Organisasjonsnummer = organizationNumber, LookupStatus = entityStatus };
        }

        var mainEntity = new BrregOrganization
        {
            Organisasjonsnummer = entity!.Organisasjonsnummer,
            LookupStatus = IsDeleted(entity.DeletionDate) ? LookupStatus.OrganizationDeleted : LookupStatus.Ok,
            Navn = entity.Navn,
            Type = EntityType.Main,
            Organisasjonsform = entity.Organisasjonsform,
            ForretningsAdresse = entity.ForretningsAdresse
        };
        if (mainEntity.LookupStatus == LookupStatus.OrganizationDeleted)
        {
            logger.LogWarning("Organization {OrganizationNumber} has deletion date {DeletionDate} in BRREG", organizationNumber, entity.DeletionDate);
            return mainEntity;
        }

        // 3. Entity is active, lookup its sub entities.
        var (subEntitiesStatus, subEntities) = await Fetch<BrregSubResponse>(
            $"/enhetsregisteret/api/underenheter?overordnetEnhet={organizationNumber}&size=500&sort=navn,DESC",
            $"subentities-{organizationNumber}", "sub entities of", organizationNumber);
        // Having no sub entities is valid, only an actual error is a failed lookup.
        if (subEntitiesStatus == LookupStatus.LookupFailed)
        {
            logger.LogError("Failed looking up sub entities for existing entity org {OrganizationNumber}", organizationNumber);
            mainEntity.LookupStatus = LookupStatus.LookupFailed;
            return mainEntity;
        }

        // BRREG omits _embedded entirely when the entity has no sub entities.
        mainEntity.Underenheter = (subEntities?.Embedded?.SubEntities ?? [])
            .Select(sub => ToOrganization(sub, entity.Organisasjonsform))
            .ToList();
        return mainEntity;
    }

    /// <summary>
    /// Retrieves and caches a BRREG resource. Returns OrganizationNotFound on 404, LookupFailed on any other error.
    /// </summary>
    private async Task<(LookupStatus Status, T? Data)> Fetch<T>(string path, string cacheKey, string description, string organizationNumber) where T : class
    {
        try
        {
            if (memoryCache.TryGetValue(cacheKey, out T? cached) && cached != null)
            {
                logger.LogInformation("Retrieved {Description} {OrganizationNumber} from cache", description, organizationNumber);
                return (LookupStatus.Ok, cached);
            }

            logger.LogInformation("Retrieving {Description} {OrganizationNumber} from BRREG, url: {Url}", description, organizationNumber, client.BaseAddress + path);
            var response = await client.GetAsync(path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("{Description} {OrganizationNumber} not found in BRREG", description, organizationNumber);
                return (LookupStatus.OrganizationNotFound, null);
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Failed to retrieve {Description} {OrganizationNumber}, status code: {StatusCode}", description, organizationNumber, response.StatusCode);
                return (LookupStatus.LookupFailed, null);
            }

            var data = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(data, _serializerOptions);

            if (result == null)
            {
                logger.LogWarning("Failed to deserialize {Description} {OrganizationNumber} - received null from BRREG API", description, organizationNumber);
                return (LookupStatus.LookupFailed, null);
            }

            memoryCache.Set(cacheKey, result, _cacheOptions);
            logger.LogInformation("Successfully retrieved and cached {Description} {OrganizationNumber}", description, organizationNumber);

            return (LookupStatus.Ok, result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving {Description} {OrganizationNumber}: {ErrorMessage}", description, organizationNumber, e.Message);
            return (LookupStatus.LookupFailed, null);
        }
    }

    private static BrregOrganization ToOrganization(SubEntityResponse subEntity, BrregOrgForm? parentOrgForm)
    {
        return new BrregOrganization
        {
            Organisasjonsnummer = subEntity.Organisasjonsnummer,
            LookupStatus = IsDeleted(subEntity.DeletionDate) ? LookupStatus.OrganizationDeleted : LookupStatus.Ok,
            Navn = subEntity.Navn,
            Type = EntityType.Sub,
            OverordnetEnhet = subEntity.OverordnetEnhet,
            Organisasjonsform = subEntity.Organisasjonsform,
            OverordnetOrganisasjonsform = parentOrgForm,
            Postadresse = subEntity.Postadresse,
            Beliggenhetsadresse = subEntity.Beliggenhetsadresse
        };
    }

    private static bool IsDeleted(string? deletionDate)
    {
        return DateTime.TryParseExact(deletionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
               && parsedDate < DateTime.Now;
    }

    // Response from /enheter/{organisasjonsnummer}
    private class EntityResponse
    {
        [JsonPropertyName("organisasjonsnummer")] public string Organisasjonsnummer { get; init; } = string.Empty;
        [JsonPropertyName("navn")] public string? Navn { get; init; }
        [JsonPropertyName("organisasjonsform")] public BrregOrgForm? Organisasjonsform { get; init; }
        [JsonPropertyName("forretningsadresse")] public BrregAddress? ForretningsAdresse { get; init; }
        [JsonPropertyName("slettedato")] public string? DeletionDate { get; init; }
    }

    // Response from /underenheter/{organisasjonsnummer}, and the items of /underenheter?overordnetEnhet=...
    private class SubEntityResponse
    {
        [JsonPropertyName("organisasjonsnummer")] public string Organisasjonsnummer { get; init; } = string.Empty;
        [JsonPropertyName("navn")] public string? Navn { get; init; }
        [JsonPropertyName("overordnetEnhet")] public string? OverordnetEnhet { get; init; }
        [JsonPropertyName("organisasjonsform")] public BrregOrgForm? Organisasjonsform { get; init; }
        [JsonPropertyName("postadresse")] public BrregAddress? Postadresse { get; init; }
        [JsonPropertyName("beliggenhetsadresse")] public BrregAddress? Beliggenhetsadresse { get; init; }
        [JsonPropertyName("slettedato")] public string? DeletionDate { get; init; }
    }

    private class BrregSubResponse
    {
        [JsonPropertyName("_embedded")] public EmbeddedSubEntities? Embedded { get; init; }
    }

    private class EmbeddedSubEntities(List<SubEntityResponse>? subEntities)
    {
        [JsonPropertyName("underenheter")] public List<SubEntityResponse>? SubEntities { get; } = subEntities;
    }
}
