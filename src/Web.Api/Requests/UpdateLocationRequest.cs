using Application.Dtos;

namespace Web.Api.Requests;

internal sealed record UpdateLocationRequest(string Name, AddressDto Address);
