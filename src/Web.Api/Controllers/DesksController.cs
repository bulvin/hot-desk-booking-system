using Application.Desks.ChangeAvailability;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DesksController : ControllerBase
{
    private readonly IMediator _mediator;

    public DesksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{id:guid}/availability")]
    public async Task<ActionResult> ChangeDeskAvailability(Guid id, ChangeDeskAvailabilityCommand command)
    {
        command = command with { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }
}