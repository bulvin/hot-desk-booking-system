using Application.Desks.Create;
using Application.Desks.Delete;
using Application.Locations.Create;
using Application.Locations.Delete;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LocationsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<ActionResult> CreateLocation(CreateLocationCommand request)
    {
        var response = await _mediator.Send(request);
        return Created($"/locations/{response.Id}", response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteLocation(Guid id)
    {
        var command = new DeleteLocationCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
    
    [HttpPost("{id:guid}/desks")]
    public async Task<ActionResult> CreateDeskInLocation(Guid id, [FromBody] CreateDeskCommand command)
    {
        command = command with { LocationId = id };
        var response = await _mediator.Send(command);
        return Created($"/locations/{response.LocationId}/desks/{response.Id}", response);
    }
    
    [HttpDelete("{locationId:guid}/desks/{deskId:guid}")]
    public async Task<IActionResult> DeleteDeskFromLocation(Guid locationId, Guid deskId)
    {
        await _mediator.Send(new DeleteDeskCommand(locationId, deskId));
        return NoContent();
    }
}