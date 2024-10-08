#nullable enable
using DsbNorge.A3Forms.Models;

namespace DsbNorge.A3Forms.DTOs;
public class ContactInfoDTO
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string FullName { get; set; }

    public string EmailAddress { get; set; }

    public string PhoneNumber { get; set; }

    public ContactInfoDTO(ContactInfo contactInfo)
    {
        FirstName = contactInfo.FirstName;
        LastName = contactInfo.LastName;
        FullName = contactInfo.FullName;
        EmailAddress = contactInfo.EmailAddress;
        PhoneNumber = contactInfo.PhoneNumber;
    }
}
