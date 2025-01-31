using System.Globalization;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.Mottakstjeneste;
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Services;

public class CountryService(IMottakstjenesteClient mottakstjenesteClient)
{
    public async Task<List<Country>> GetCountries(string formName)
    {
        return await mottakstjenesteClient.GetCountries(formName);
    }

    /// Form name is passed as the value of key <c>formName</c> in <c>keyValuePairs</c>.
    /// This is needed because it is part of the downstream MT endpoint.
    public async Task<AppOptions> GetAppOptionsAsync(Dictionary<string, string> keyValuePairs)
    {
        var formName = keyValuePairs["formName"];
        var countries = await GetCountries(formName);

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
