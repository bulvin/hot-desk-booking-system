using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails();
        if (exception is FluentValidation.ValidationException fluentException)
        {
            problemDetails.Title = "One or more validation errors occurred.";
            httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            List<string> errors = [];
            errors.AddRange(fluentException.Errors.Select(error => error.ErrorMessage));
            problemDetails.Extensions.Add("errors", errors);
        }
        else if (exception is HotDeskBookingException bookingException)
        {
            httpContext.Response.StatusCode = Convert.ToInt32(bookingException.HttpStatusCode);
            problemDetails.Title = exception.Message;
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            problemDetails.Title = "Server failure";
        }

        httpContext.Response.ContentType = "application/problem+json";
        problemDetails.Status = httpContext.Response.StatusCode;
        
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);
        return true;
    }
}