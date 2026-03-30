using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class BrregSubEntity
{
  [JsonPropertyName("organisasjonsnummer")]
  public string Organisasjonsnummer { get; set; } = string.Empty;

  [JsonPropertyName("navn")]
  public string Navn { get; set; } = string.Empty;

  [JsonPropertyName("organisasjonsform")]
  public BrregOrgForm Organisasjonsform { get; set; } = new();

  [JsonPropertyName("postadresse")]
  public Adresse? Postadresse { get; set; }

  [JsonPropertyName("beliggenhetsadresse")]
  public Adresse? Beliggenhetsadresse { get; set; }
}

public class Adresse
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
