using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.GeoNorge;
using DsbNorge.A3Forms.Services;

namespace DsbNorge.A3Forms.OptionsProviders;

public class AddressSearchHitsProvider(IGeoNorgeClient geoNorgeClient, string? id = null) : IInstanceDataListProvider
{
    public string Id { get; } = id ?? "addressSearchHits";

    private readonly AddressSearchService _addressSearchService = new(geoNorgeClient);

    public async Task<DataList> GetInstanceDataListAsync(InstanceIdentifier instanceIdentifier, string? language, Dictionary<string, string> keyValuePairs)
    {
        if (keyValuePairs.TryGetValue("search", out var addressSearch) && addressSearch.Length >= 3)
        {
            return await _addressSearchService.GetAddresses(addressSearch, 5);
        }
        
        return new DataList
        {
            ListItems = [], 
            _metaData = new DataListMetadata { TotaltItemsCount = 0 }
        };
    }

}
