using System.Globalization;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.GeoNorge;

namespace DsbNorge.A3Forms.OptionsProviders;

public class MunicipalitiesProvider
{
    private readonly IGeoNorgeClient _geoNorgeClient;
    
    public MunicipalitiesProvider(IGeoNorgeClient geoNorgeClient)
    {
        _geoNorgeClient = geoNorgeClient;
    }

    public async Task<AppOptions> GetMunicipalitiesOptions()
    {
        var municipalities = await _geoNorgeClient.GetMunicipalities();

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
