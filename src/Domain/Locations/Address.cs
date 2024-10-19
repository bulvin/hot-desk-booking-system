namespace Domain.Locations;

public class Address
{
    public string Street { get; set; } = default!;
    public string BuildingNumber { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
}