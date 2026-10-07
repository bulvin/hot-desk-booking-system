using Application.Users.Login;
using Application.Users.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Api.Controllers;

[Route("/api")]
[ApiController]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Creates a new employee account")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register(RegisterUserCommand command)
    {
        var response = await mediator.Send(command);
        return Ok(response);
    }

    [HttpPost("login")]
    [SwaggerOperation(Summary = "Logs in a user")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Login(LoginUserCommand command)
    {
        var response = await mediator.Send(command);
        return Ok(response);
    }
}