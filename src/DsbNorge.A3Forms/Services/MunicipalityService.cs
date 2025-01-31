using System.Globalization;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.GeoNorge;
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Services;

public class MunicipalityService(IGeoNorgeClient geoNorgeClient)
{
    public async Task<List<Municipality>> GetMunicipalities()
    {
        return await geoNorgeClient.GetMunicipalities();
    }

    public async Task<AppOptions> GetAppOptionsAsync()
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
