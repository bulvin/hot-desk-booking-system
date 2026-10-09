using Application.Dtos;

namespace Web.Api.Requests;

internal sealed record CreateLocationRequest(string Name, AddressDto Address);
