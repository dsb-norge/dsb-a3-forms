﻿namespace DsbNorge.A3Forms.Models;

public class AddressSearchHit
{
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? PostalCity { get; set; }
    public required string SearchHit { get; set; }
}