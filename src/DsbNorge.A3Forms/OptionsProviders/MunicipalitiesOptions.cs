using System.Globalization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients;

namespace DsbNorge.A3Forms.OptionsProviders;

public class MunicipalitiesOptions(IGeoNorgeClient geoNorgeClient) : IAppOptionsProvider
{
    public string Id { get; set; } = "municipalities";
    
    public async Task<AppOptions> GetAppOptionsAsync(string language, Dictionary<string, string> keyValuePairs)
    {
        var municipalities = await geoNorgeClient.GetMunicipalities();

        var options = new AppOptions
        {
            Options =
                municipalities?.Select(municipality => new AppOption
                        {
                            Label = $"{municipality.KommunenavnNorsk} - {municipality.Kommunenummer}",
                            Value = municipality.Kommunenummer
                        }
                    )
                    .OrderBy(
                        municipality => municipality.Label,
                        StringComparer.Create(new CultureInfo("nb-NO"), true)
                    )
                    .ToList()
        };

        return await Task.FromResult(options);
    }
}