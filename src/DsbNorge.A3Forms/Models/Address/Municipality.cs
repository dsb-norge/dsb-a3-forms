using Newtonsoft.Json;

namespace DsbNorge.A3Forms.Models.Address;

public class Municipality
{
    [JsonProperty("kommunenavnNorsk")] 
    public string KommunenavnNorsk { get; set; }
    [JsonProperty("kommunenummer")] 
    public string Kommunenummer { get; set; }  
}