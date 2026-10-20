using System.Diagnostics;
using Domain.Exceptions;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IMessage
{
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(message, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation("Handled {RequestName} successfully in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            logger.LogInformation("Handling {RequestName} was canceled after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception) when (exception is ValidationException or HotDeskBookingException)
        {
            stopwatch.Stop();
            logger.LogWarning("Handled {RequestName} with {ErrorType} in {ElapsedMs}ms", requestName, exception.GetType().Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError("Handled {RequestName} with unexpected {ErrorType} in {ElapsedMs}ms", requestName, exception.GetType().Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            if (stopwatch.ElapsedMilliseconds > 500)
            {
                logger.LogWarning("Long-running request: {RequestName} took {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
