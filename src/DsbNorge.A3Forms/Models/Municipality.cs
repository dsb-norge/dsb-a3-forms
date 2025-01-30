using System.Text.Json.Serialization;

namespace DsbNorge.A3Forms.Models;

public class Municipality
{
    [JsonPropertyName("kommunenavnNorsk")] 
    public required string KommunenavnNorsk { get; set; }
    
    [JsonPropertyName("kommunenummer")] 
    public required string Kommunenummer { get; set; }  
}