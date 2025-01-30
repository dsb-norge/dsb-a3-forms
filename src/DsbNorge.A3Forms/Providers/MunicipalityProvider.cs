using System.Globalization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.GeoNorge;
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Providers;

public class MunicipalityProvider(IGeoNorgeClient geoNorgeClient, string? id = null) : IAppOptionsProvider
{
    public string Id { get; } = id ?? "municipalities";
    
    public async Task<List<Municipality>> GetMunicipalities()
    {
        return await geoNorgeClient.GetMunicipalities();
    }

    public async Task<AppOptions> GetAppOptionsAsync(string? language, Dictionary<string, string> keyValuePairs)
    {
        var municipalities = await GetMunicipalities();

        var options = new AppOptions
        {
            Options = municipalities?
                .Select(municipality => new AppOption
                {
                    Label = $"{municipality.KommunenavnNorsk} - {municipality.Kommunenummer}",
                    Value = municipality.Kommunenummer
                })
                .OrderBy(
                    municipality => municipality.Label,
                    StringComparer.Create(new CultureInfo("nb-NO"), true)
                )
                .ToList()
        };

        return options;
    }

}
