using System.Globalization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.Mottakstjeneste;
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Providers;

public class NationalitiesProvider(IMottakstjenesteClient mottakstjenesteClient, string? id = null) : IAppOptionsProvider
{
    public string Id { get; } = id ?? "nationalities";

    public async Task<List<Nationality>> GetNationalities(string formName)
    {
        return await mottakstjenesteClient.GetNationalities(formName);
    }

    public async Task<AppOptions> GetAppOptionsAsync(string? language, Dictionary<string, string> keyValuePairs)
    {
        var formName = keyValuePairs["formName"];
        var countries = await GetNationalities(formName);

        var options = new AppOptions
        {
            Options = countries
                .Select(country => new AppOption
                {
                    Label = $"{country.Name} - {country.CountryCode}",
                    Value = $"{country.A2CountryCode}"
                })
                .OrderBy(
                    country => country.Label,
                    StringComparer.Create(new CultureInfo("nb-NO"), true)
                )
                .ToList()
        };

        return options;
    }
}
