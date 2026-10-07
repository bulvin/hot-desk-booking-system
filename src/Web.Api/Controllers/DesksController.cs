using Application.Desks.ChangeAvailability;
using Application.Desks.Create;
using Application.Desks.Delete;
using Application.Desks.GetDetails;
using Application.Desks.GetPagedByLocation;
using Application.Dtos;
using Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Api.Controllers;

[Route("api/locations/{locationId:guid}/desks")]
[ApiController]
[Authorize]
public class DesksController(IMediator mediator) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.Admin)]
    [SwaggerOperation(
      Summary = "update desk availability"
   )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangeDeskAvailability(Guid id, Guid locationId, bool isAvailable)
    {
        await mediator.Send(new ChangeDeskAvailabilityCommand(id, locationId, isAvailable));
        return NoContent();
    }

    [HttpGet]
    [SwaggerOperation(
       Summary = "Get paged desks for location"
    )]
    [ProducesResponseType(typeof(PagedDto<DeskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PagedDto<DeskDto>>> GetDesksForLocation(Guid locationId, [FromQuery] GetDesksByLocationRequest query)
    {
        var response = await mediator.Send(new GetDesksByLocationQuery(locationId,
            new DeskAvailabilityFilter(query.IsAvailable, query.IsBookable),
            new DateRange(query.StartDate, query.EndDate),
            new PaginationFilter(query.Page, query.PageSize)));

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
       Summary = "Get desk details"
    )]
    [ProducesResponseType(typeof(DeskDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeskDetailsDto>> GetDeskDetails(Guid id, Guid locationId)
    {
        var response = await mediator.Send(new GetDeskDetailsQuery(id, locationId));
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.Admin)]
    [SwaggerOperation(
       Summary = "Create desk in location"
    )]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateDeskInLocation(Guid locationId, [FromBody] CreateDeskCommand command)
    {
        command = command with { LocationId = locationId };
        var response = await mediator.Send(command);
        return Created(new Uri($"/locations/{response.LocationId}/desks/{response.Id}", UriKind.Relative), response);
    }

    [HttpDelete("{deskId:guid}")]
    [Authorize(Policy = PolicyNames.Admin)]
    [SwaggerOperation(
       Summary = "Delete desk"
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteDeskFromLocation(Guid locationId, Guid deskId)
    {
        await mediator.Send(new DeleteDeskCommand(locationId, deskId));
        return NoContent();
    }
}