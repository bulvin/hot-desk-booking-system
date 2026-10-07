using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Desks.Create;

public record CreateDeskCommand(string Name, string? Description) : ICommand<DeskDto>
{
    public Guid LocationId { get; init; }
}