using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class BrregOrgForm
{
    [JsonPropertyName("kode")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("beskrivelse")]
    public string Description { get; init; } = string.Empty;
}
