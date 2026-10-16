using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class BrregAddress
{
    [JsonPropertyName("kommune")]
    public string? Kommune { get; set; }

    [JsonPropertyName("landkode")]
    public string? Landkode { get; set; }

    [JsonPropertyName("postnummer")]
    public string? Postnummer { get; set; }

    [JsonPropertyName("adresse")]
    public List<string>? AdresseLinjer { get; set; }

    [JsonPropertyName("land")]
    public string? Land { get; set; }

    [JsonPropertyName("kommunenummer")]
    public string? Kommunenummer { get; set; }

    [JsonPropertyName("poststed")]
    public string? Poststed { get; set; }
}
