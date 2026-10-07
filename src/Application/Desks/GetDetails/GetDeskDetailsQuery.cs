using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Desks.GetDetails;

public record GetDeskDetailsQuery(Guid Id, Guid LocationId) : IQuery<DeskDetailsDto>;