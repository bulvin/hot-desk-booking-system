using Application.Reservations.BookDesk;
using Application.Reservations.ChangeDesk;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

[Route("api/reservations")]
[ApiController]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult> BookDesk([FromBody] BookDeskCommand request)
    {
       var response = await _mediator.Send(request);
       return Created($"/reservations/{response.Id}", response);
    }

    [HttpPut("{id:guid}/change-desk")]
    public async Task<ActionResult> ChangeDesk(Guid id, [FromBody] ChangeDeskCommand command)
    {
        command = command with { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }
}