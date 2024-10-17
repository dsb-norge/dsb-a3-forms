using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients;

namespace DsbNorge.A3Forms.OptionProviders;

public class AddressSearchHitsProvider(IGeoNorgeClient iGeoNorgeClient) : IInstanceDataListProvider
{
    public string Id { get; set; } = "addressSearchHits";

    public async Task<DataList> GetInstanceDataListAsync(InstanceIdentifier instanceIdentifier, string language, Dictionary<string, string> keyValuePairs)
    {
        var items = new List<ListItem>();

        if (keyValuePairs.TryGetValue("search", out var addressSearch) && addressSearch.Length >= 3)
        {
            var searchHits = await iGeoNorgeClient.GetAddresses(addressSearch, 5);
            items.AddRange(searchHits.Select(address => new ListItem
            {
                Address = address.Adressetekst,
                PostalCode = address.Postnummer,
                PostalCity = address.Poststed,
                SearchHit = JsonSerializer.Serialize(address)
            }));
        }

        var appListsMetaData = new DataListMetadata { TotaltItemsCount = items.Count };

        var objectList = new List<object>();
        items.ForEach(o => objectList.Add(o));

        return new DataList { ListItems = objectList, _metaData = appListsMetaData };
    }

    private class ListItem
    {
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string PostalCity { get; set; }
        public string SearchHit { get; set; }
    }

}