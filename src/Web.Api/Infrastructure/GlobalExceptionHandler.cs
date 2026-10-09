using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Web.Api.Infrastructure;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (statusCode, title) = MapException(exception);
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = httpContext.Request.Path,
            Detail = GetSafeErrorMessage(exception)
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray(), StringComparer.Ordinal);
        }

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
                httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
        HotDeskBookingException bookingException => ((int)bookingException.HttpStatusCode,
            ReasonPhrases.GetReasonPhrase((int)bookingException.HttpStatusCode)),
        BadHttpRequestException badRequest => (badRequest.StatusCode, ReasonPhrases.GetReasonPhrase(badRequest.StatusCode)),
        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
    };

    private string? GetSafeErrorMessage(Exception exception) => exception switch
    {
        HotDeskBookingException => exception.Message,
        _ when environment.IsDevelopment() => exception.Message,
        _ => null
    };
}
