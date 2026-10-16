namespace DsbNorge.A3Forms.Models;

public class BrregOrganization
{
  public required string Organisasjonsnummer { get; set; }

  // Ok when the organization and all its related data were retrieved.
  // Other fields are filled with whatever could be retrieved, and may be null when the status is not Ok.
  public required LookupStatus LookupStatus { get; set; }

  public string? Navn { get; set; }

  // main or sub entity, null when the organization could not be retrieved
  public EntityType? Type { get; set; }

  // null for main entity
  public string? OverordnetEnhet { get; set; }

  public BrregOrgForm? Organisasjonsform { get; set; }

  // org form of the main entity, for sub entity. Null for main entity
  public BrregOrgForm? OverordnetOrganisasjonsform { get; set; }

  // for main entity
  public BrregAddress? ForretningsAdresse { get; set; }

  // for sub entity
  public BrregAddress? Postadresse { get; set; }

  // for sub entity
  public BrregAddress? Beliggenhetsadresse { get; set; }

  // for main entity
  public List<BrregOrganization>? Underenheter { get; set; }
}
