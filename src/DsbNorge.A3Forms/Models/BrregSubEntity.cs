using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class BrregSubEntity
{
  [JsonPropertyName("organisasjonsnummer")]
  public string Organisasjonsnummer { get; set; } = string.Empty;

  [JsonPropertyName("navn")]
  public string Navn { get; set; } = string.Empty;

  [JsonPropertyName("organisasjonsform")]
  public Organisasjonsform Organisasjonsform { get; set; } = new();

  [JsonPropertyName("postadresse")]
  public Adresse? Postadresse { get; set; }

  [JsonPropertyName("beliggenhetsadresse")]
  public Adresse? Beliggenhetsadresse { get; set; }

  [JsonPropertyName("registrertIMvaregisteret")]
  public bool RegistrertIMvaregisteret { get; set; }

  [JsonPropertyName("naeringskode1")]
  public BrregOrgForm? Naeringskode1 { get; set; }

  [JsonPropertyName("naeringskode2")]
  public BrregOrgForm? Naeringskode2 { get; set; }

  [JsonPropertyName("naeringskode3")]
  public BrregOrgForm? Naeringskode3 { get; set; }

  [JsonPropertyName("hjelpeenhetskode")]
  public BrregOrgForm? Hjelpeenhetskode { get; set; }

  [JsonPropertyName("registreringsdatoEnhetsregisteret")]
  public string RegistreringsdatoEnhetsregisteret { get; set; } = string.Empty;

  [JsonPropertyName("hjemmeside")]
  public string? Hjemmeside { get; set; }

  [JsonPropertyName("frivilligMvaRegistrertBeskrivelser")]
  public List<string>? FrivilligMvaRegistrertBeskrivelser { get; set; }

  [JsonPropertyName("antallAnsatte")]
  public int AntallAnsatte { get; set; }

  [JsonPropertyName("harRegistrertAntallAnsatte")]
  public bool HarRegistrertAntallAnsatte { get; set; }

  [JsonPropertyName("overordnetEnhet")]
  public string? OverordnetEnhet { get; set; }

  [JsonPropertyName("oppstartsdato")]
  public string? Oppstartsdato { get; set; }

  [JsonPropertyName("datoEierskifte")]
  public string? DatoEierskifte { get; set; }

  [JsonPropertyName("nedleggelsesdato")]
  public string? Nedleggelsesdato { get; set; }

  [JsonPropertyName("registreringsdatoAntallAnsatteNAVAaregisteret")]
  public string? RegistreringsdatoAntallAnsatteNAVAaregisteret { get; set; }

  [JsonPropertyName("registreringsdatoAntallAnsatteEnhetsregisteret")]
  public string? RegistreringsdatoAntallAnsatteEnhetsregisteret { get; set; }

  [JsonPropertyName("registreringsdatoMerverdiavgiftsregisteret")]
  public string? RegistreringsdatoMerverdiavgiftsregisteret { get; set; }

  [JsonPropertyName("registreringsdatoMerverdiavgiftsregisteretEnhetsregisteret")]
  public string? RegistreringsdatoMerverdiavgiftsregisteretEnhetsregisteret { get; set; }

  [JsonPropertyName("registreringsdatoFrivilligMerverdiavgiftsregisteret")]
  public string? RegistreringsdatoFrivilligMerverdiavgiftsregisteret { get; set; }

  [JsonPropertyName("epostadresse")]
  public string? Epostadresse { get; set; }

  [JsonPropertyName("telefon")]
  public string? Telefon { get; set; }

  [JsonPropertyName("mobil")]
  public string? Mobil { get; set; }
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

public class Organisasjonsform
{
  [JsonPropertyName("kode")]
  public string Kode { get; set; } = string.Empty;

  [JsonPropertyName("utgaatt")]
  public string? Utgaatt { get; set; }

  [JsonPropertyName("beskrivelse")]
  public string Beskrivelse { get; set; } = string.Empty;
}
