using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class Nationality
{
    [JsonPropertyName("navn_id")] 
    public string Name { get; set; }
    [JsonPropertyName("landkode")] 
    public string CountryCode { get; set; }
    [JsonPropertyName("a2_landkode")] 
    public string A2CountryCode { get; set; }
}