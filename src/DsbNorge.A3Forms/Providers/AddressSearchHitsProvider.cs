using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.GeoNorge;
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Providers;

public class AddressSearchHitsProvider(IGeoNorgeClient geoNorgeClient, string? id = null) : IInstanceDataListProvider
{
    public string Id { get; } = id ?? "addressSearchHits";

    public async Task<List<GeoNorgeAdresse>> GetAddresses(string addressSearch, int hitsPerPage)
    {
        return await geoNorgeClient.GetAddresses(addressSearch, hitsPerPage);
    }

    /// Search string is passed as the value of key <c>search</c> in <c>keyValuePairs</c>.
    public async Task<DataList> GetInstanceDataListAsync(InstanceIdentifier instanceIdentifier, string? language, Dictionary<string, string> keyValuePairs)
    {
        if (!keyValuePairs.TryGetValue("search", out var addressSearch) || addressSearch.Length < 3)
        {
            return new DataList
            {
                ListItems = [],
                _metaData = new DataListMetadata { TotaltItemsCount = 0 }
            };
        }

        var items = new List<AddressSearchHit>();
        
        var searchHits = await GetAddresses(addressSearch, 5);
        items.AddRange(searchHits.Select(address => new AddressSearchHit
        {
            Address = address.Adressetekst,
            PostalCode = address.Postnummer,
            PostalCity = address.Poststed,
            SearchHit = JsonSerializer.Serialize(address)
        }));
        
        var appListsMetaData = new DataListMetadata { TotaltItemsCount = items.Count };

        var objectList = new List<object>();
        items.ForEach(o => objectList.Add(o));

        return new DataList { ListItems = objectList, _metaData = appListsMetaData };
    }
}
