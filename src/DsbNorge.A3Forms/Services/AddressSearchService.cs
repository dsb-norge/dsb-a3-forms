using System.Text.Json;
using Altinn.App.Core.Models;
using DsbNorge.A3Forms.Clients.GeoNorge;
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.Services;

public class AddressSearchService(IGeoNorgeClient geoNorgeClient)
{
    public async Task<DataList> GetAddresses(string addressSearch, int hitsPerPage)
    {
        var items = new List<AddressSearchHit>();
        
        var searchHits = await geoNorgeClient.GetAddresses(addressSearch, hitsPerPage);
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
