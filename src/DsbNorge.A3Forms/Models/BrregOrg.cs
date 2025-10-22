using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class BrregOrg
{
  [JsonPropertyName("organisasjonsnummer")]
  public string Organisasjonsnummer { get; set; } = string.Empty;

  [JsonPropertyName("navn")]
  public string Navn { get; set; } = string.Empty;

  [JsonPropertyName("forretningsadresse")]
  public ForretningsAdresse? ForretningsAdresse { get; set; }
}

public class ForretningsAdresse
{
  [JsonPropertyName("postnummer")]
  public string Postnummer { get; set; } = string.Empty;

  [JsonPropertyName("poststed")]
  public string Poststed { get; set; } = string.Empty;
}
