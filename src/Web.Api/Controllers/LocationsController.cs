using Application.Locations.Create;
using Application.Locations.Delete;
using Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Api.Controllers;

[Route("api/locations")]
[ApiController]
[Authorize(Policy = PolicyNames.Admin)]
public class LocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LocationsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create location"
    )]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> CreateLocation(CreateLocationCommand request)
    {
        var response = await _mediator.Send(request);
        return Created($"/locations/{response.Id}", response);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete location"
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteLocation(Guid id)
    {
        var command = new DeleteLocationCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}