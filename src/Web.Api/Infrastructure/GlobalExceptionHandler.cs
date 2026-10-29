using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        DbUpdateException when IsDuplicateAccount(exception) => (StatusCodes.Status409Conflict, "Conflict"),
        ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
        HotDeskBookingException bookingException => ((int)bookingException.HttpStatusCode,
            ReasonPhrases.GetReasonPhrase((int)bookingException.HttpStatusCode)),
        BadHttpRequestException badRequest => (badRequest.StatusCode, ReasonPhrases.GetReasonPhrase(badRequest.StatusCode)),
        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
    };

    private static bool IsDuplicateAccount(Exception exception) => exception is DbUpdateException
    {
        InnerException: PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            TableName: "AspNetUsers",
            ConstraintName: "EmailIndex" or "UserNameIndex"
        }
    };

    private string? GetSafeErrorMessage(Exception exception) => exception switch
    {
        DbUpdateException when IsDuplicateAccount(exception) => "An account with this email already exists.",
        HotDeskBookingException => exception.Message,
        _ when environment.IsDevelopment() => exception.Message,
        _ => null
    };
}
