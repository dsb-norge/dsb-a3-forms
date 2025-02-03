using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DsbNorge.A3Forms.Models;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DsbNorge.A3Forms.Clients.Saft;

public class SaftClient(
    HttpClient client,
    ILogger<ISaftClient> logger) : ISaftClient
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PlisMeta?> GetPlisPerson(string accessToken, string ssn)
    {
        try
        {
            logger.LogInformation("Retrieving PLIS person for {maskedSsn}", MaskSsn(ssn)); 
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var request = new GetPersonRequest(ssn);
            var requestJson = JsonSerializer.Serialize(request);

            var res = await client.PostAsync(
                "/api/v1/person", 
                new StringContent(requestJson, Encoding.UTF8, "application/json"));

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception("Get PLIS person for " + MaskSsn(ssn) + " failed with status code " + res.StatusCode);
            }

            var resString = await res.Content.ReadAsStringAsync();
            // should never have blank body when response code is 2xx, but we should handle it
            if (string.IsNullOrEmpty(resString))
            {
                throw new Exception("Get PLIS person for " + MaskSsn(ssn) + " returned 200 OK but blank body");
            }

            try
            {
                var response = JsonSerializer.Deserialize<GetPersonResponse>(resString, _serializerOptions);
                // only allow if person is found in PLIS and is not CANDIDATE
                if (response?.Results is { PersonExists: "YES" } && !response.Results.RecordType.Equals("CANDIDATE"))
                {
                    return new PlisMeta
                    {
                        HandlingDistrict = response.Results.DistrictId,
                        PlisId = response.Results.PlisId,
                        DsbArchRef = response.Results.CaseNo
                    };
                }
                // Unknown person or CANDIDATE --> should not allow submission
                return null;
            }
            catch (Exception e)
            {
                logger.LogError("Deserialization of PLIS person data failed with msg: {content}", e.Message);
                // Here we don't really know if person is PLIS person or not - then we should allow submission
                return new PlisMeta();
            }
        }
        catch (Exception e)
        {
            logger.LogError("{Message}", e.Message);
            // Here we don't really know if person is PLIS person or not - then we should allow submission
            return new PlisMeta();
        }
    }

    private static string MaskSsn(string ssn)
    {
        return ssn[..6] + "xxxxx";
    }

    private class GetPersonRequest(string ssn)
    {
        [JsonPropertyName("ssn")] public string Ssn { get; } = ssn;
    }

    public class GetPersonResponse(bool isSuccess, string? error, PlisPersonDetails? results)
    {
        [JsonPropertyName(nameof(IsSuccess))] public bool IsSuccess { get; } = isSuccess;
        [JsonPropertyName(nameof(Error))] public string? Error { get; } = error;
        [JsonPropertyName(nameof(Results))] public PlisPersonDetails? Results { get; } = results;
    }

    public class PlisPersonDetails(
        string fnr,
        string fullName,
        string recordType,
        string personExists,
        string plisId,
        string districtId,
        string caseNo)
    {
        [JsonPropertyName("fNr")] public string Fnr { get; } = fnr;
        [JsonPropertyName("lastName")] public string FullName { get; } = fullName;
        [JsonPropertyName("recordType")] public string RecordType { get; } = recordType;
        [JsonPropertyName("personExists")] public string PersonExists { get; } = personExists;
        [JsonPropertyName("plisId")] public string PlisId { get; } = plisId;
        [JsonPropertyName("districtId")] public string DistrictId { get; } = districtId;
        [JsonPropertyName("caseNo")] public string CaseNo { get; } = caseNo;
    }

}