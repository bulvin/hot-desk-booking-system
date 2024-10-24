using Application.Reservations.BookDesk;
using Application.Reservations.ChangeDesk;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Api.Controllers;

[Route("api/reservations")]
[ApiController]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Book a desk"
    )]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> BookDesk([FromBody] BookDeskCommand request)
    {
        var response = await _mediator.Send(request);
        return Created($"/reservations/{response.Id}", response);
    }

    [HttpPut("{id:guid}/change-desk")]
    [SwaggerOperation(
        Summary = "Change reserved desk"
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> ChangeDesk(Guid id, [FromBody] ChangeDeskCommand command)
    {
        command = command with { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }
}