using Application.Desks.ChangeAvailability;
using Application.Desks.Create;
using Application.Desks.Delete;
using Application.Desks.Get;
using Application.Desks.GetPagedByLocation;
using Application.Dtos;
using Domain.Users;
using Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace Web.Api.Controllers;

[Route("api/locations/{locationId:guid}/desks")]
[ApiController]
[Authorize]
public class DesksController : ControllerBase
{
    private readonly IMediator _mediator;

    public DesksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task<ActionResult> ChangeDeskAvailability(Guid id, Guid locationId, bool isAvailable)
    {
        await _mediator.Send(new ChangeDeskAvailabilityCommand(id, locationId, isAvailable));
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<PagedDto<DeskDto>>> GetDesksForLocation(Guid locationId, [FromQuery] GetDesksByLocationRequest query)
    {
        var response = await _mediator.Send(new GetDesksByLocationQuery(locationId, query.IsAvailable, query.Page, query.PageSize));
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeskDetailsDto>> GetDeskDetails(Guid id, Guid locationId)
    {
        var response = await _mediator.Send(new GetDeskDetailsQuery(id, locationId));
        return Ok(response);
    }
    
    [HttpPost]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task<ActionResult> CreateDeskInLocation(Guid locationId, [FromBody] CreateDeskCommand command)
    {
        command = command with { LocationId = locationId };
        var response = await _mediator.Send(command);
        return Created($"/locations/{response.LocationId}/desks/{response.Id}", response);
    }
    
    [HttpDelete("{deskId:guid}")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task<IActionResult> DeleteDeskFromLocation(Guid locationId, Guid deskId)
    {
        await _mediator.Send(new DeleteDeskCommand(locationId, deskId));
        return NoContent();
    }
}