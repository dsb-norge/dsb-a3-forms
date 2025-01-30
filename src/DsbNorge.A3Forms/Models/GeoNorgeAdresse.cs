using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class GeoNorgeAdresseRespons
{
    // {"metadata":{"totaltAntallTreff":0,"asciiKompatibel":true,"side":0,"sokeStreng":"sok=Bue%20T%C3%B8nsberg","viserFra":0,"viserTil":10,"treffPerSide":10},"adresser":[]}
    
    [JsonPropertyName("metadata")]
    public GeoNorgeMetadata? Metadata { get; set; }
    
    [JsonPropertyName("adresser")]
    public List<GeoNorgeAdresse>? Adresser { get; set; }
}

public class GeoNorgeMetadata
{
    [JsonPropertyName("totaltAntallTreff")]
    public int? TotaltAntallTreff { get; set; }

    [JsonPropertyName("asciiKompatibel")]
    public bool? AsciiKompatibel { get; set; }

    [JsonPropertyName("side")]
    public int? Side { get; set; }

    [JsonPropertyName("sokeStreng")]
    public string? SokeStreng { get; set; }

    [JsonPropertyName("viserFra")]
    public int? ViserFra { get; set; }

    [JsonPropertyName("viserTil")]
    public int? ViserTil { get; set; }

    [JsonPropertyName("treffPerSide")]
    public int? TreffPerSide { get; set; }
}

public class GeoNorgeAdresse
{
    [JsonPropertyName("adressenavn")] 
    public string? Adressenavn { get; set; }
    
    [JsonPropertyName("adressetekst")] 
    public string? Adressetekst { get; set; }
    
    [JsonPropertyName("adressekode")] 
    public int? Adressekode { get; set; }
    
    [JsonPropertyName("nummer")] 
    public int? Nummer { get; set; }
    
    [JsonPropertyName("bokstav")] 
    public string? Bokstav { get; set; }
    
    [JsonPropertyName("kommunenummer")] 
    public string? Kommunenummer { get; set; }
    
    [JsonPropertyName("kommunenavn")] 
    public string? Kommunenavn { get; set; }
    
    [JsonPropertyName("gardsnummer")] 
    public int? Gardsnummer { get; set; }

    [JsonPropertyName("bruksnummer")] 
    public int? Bruksnummer { get; set; }
    
    [JsonPropertyName("festenummer")] 
    public int? Festenummer { get; set; }
    
    [JsonPropertyName("poststed")] 
    public string? Poststed { get; set; }
    
    [JsonPropertyName("postnummer")] 
    public string? Postnummer { get; set; }
    
    [JsonPropertyName("representasjonspunkt")] 
    public Representasjonspunkt? Representasjonspunkt { get; set; }
    
    [JsonPropertyName("oppdateringsdato")] 
    public string? Oppdateringsdato { get; set; }
}

public class Representasjonspunkt
{
    [JsonPropertyName("epsg")] 
    public string? Epsg { get; set; }

    [JsonPropertyName("lat")] 
    public decimal? Lat { get; set; }
    
    [JsonPropertyName("lon")] 
    public decimal? Lon { get; set; }
}