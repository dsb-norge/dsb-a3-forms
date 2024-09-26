namespace DsbA3Forms.Models.Address
{
    public class GeoNorgeAdresseRespons
    {
        // {"metadata":{"totaltAntallTreff":0,"asciiKompatibel":true,"side":0,"sokeStreng":"sok=Bue%20T%C3%B8nsberg","viserFra":0,"viserTil":10,"treffPerSide":10},"adresser":[]}
        
        public GeoNorgeMetadata Metadata { get; set; }
        
        public List<GeoNorgeAdresse> Adresser { get; set; }
    }

    public class GeoNorgeMetadata
    {
        public int TotaltAntallTreff { get; set; }

        public bool AsciiKompatibel { get; set; }

        public int Side { get; set; }

        public string SokeStreng { get; set; }

        public int ViserFra { get; set; }

        public int ViserTil { get; set; }

        public int TreffPerSide { get; set; }
    }

    public class GeoNorgeAdresse
    {
        public string Adressenavn { get; set; }
        
        public string Adressetekst { get; set; }
        
        public int? Adressekode { get; set; }
        
        public int? Nummer { get; set; }
        
        public string Bokstav { get; set; }
        
        public string Kommunenummer { get; set; }
        
        public string Kommunenavn { get; set; }
        
        public int? Gardsnummer { get; set; }

        public int? Bruksnummer { get; set; }
        
        public int? Festenummer { get; set; }
        
        public string Poststed { get; set; }
        
        public string Postnummer { get; set; }
        
        public Representasjonspunkt Representasjonspunkt { get; set; }
        
        public string Oppdateringsdato { get; set; }
    }

    public class Representasjonspunkt
    {
        public string Epsg { get; set; }

        public decimal Lat { get; set; }
        
        public decimal Lon { get; set; }
    }
}