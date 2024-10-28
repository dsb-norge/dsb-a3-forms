using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class Municipality
{
    [JsonPropertyName("kommunenavnNorsk")] 
    public string KommunenavnNorsk { get; set; }
    
    [JsonPropertyName("kommunenummer")] 
    public string Kommunenummer { get; set; }  
}