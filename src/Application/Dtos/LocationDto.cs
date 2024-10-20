namespace Application.Dtos;

public record LocationDto(Guid Id, string Name, AddressDto Address);

public record AddressDto(string Street, string BuildingNumber, string City, string PostalCode);