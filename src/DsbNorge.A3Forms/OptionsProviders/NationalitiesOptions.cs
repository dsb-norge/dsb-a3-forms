using System.Globalization;
using DsbNorge.A3Forms.Clients.Mottakstjeneste;
using Altinn.App.Core.Models; 

namespace DsbNorge.A3Forms.OptionsProviders
{
    public class NationalitiesOptions
    {
        private readonly IMottakstjenesteClient _mottakstjenesteClient;

        public NationalitiesOptions(IMottakstjenesteClient mottakstjenesteClient)
        {
            _mottakstjenesteClient = mottakstjenesteClient;
        }

        public async Task<AppOptions> GetNationalitiesOptions(string formName)
        {
            var countries = await _mottakstjenesteClient.GetNationalities(formName);

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
}