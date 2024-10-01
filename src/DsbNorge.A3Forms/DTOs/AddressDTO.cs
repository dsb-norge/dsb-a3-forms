using DsbNorge.A3Forms.Models.Address;

namespace DsbNorge.A3Forms.DTOs;
public class AddressDTO
{
    public string StreetAddress { get; set; }

    public string PostalCode { get; set; }

    public string PostalCity { get; set; }

    public string Municipality { get; set; }

    public AddressDTO(Address address)
    {
        StreetAddress = address.StreetAddress;
        PostalCode = address.PostalCode;
        PostalCity = address.PostalCity;
        Municipality = address.Municipality;
    }
}
