using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class PlisMeta
{
    [JsonPropertyName("HandlingDistrict")]
    public string? HandlingDistrict { get; set; }

    [JsonPropertyName("PlisId")]
    public string? PlisId { get; set; }

    [JsonPropertyName("DsbArchRef")]
    public string? DsbArchRef { get; set; }

    [JsonPropertyName("MessageXref")]
    public string? MessageXref { get; set; }

}