using Application;
using Application.Desks.Create;
using Application.Desks.GetPagedByLocation;
using Application.Interfaces;
using Application.Locations.Delete;
using Domain;
using Domain.Desks;
using Domain.Locations;
using Domain.Reservations;
using FluentValidation;
using Infrastructure.Time;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Moq;



namespace UnitTests.Behaviors;

public sealed class MediatorTests
{
    private readonly Mock<IDeskRepository> _desks = new(MockBehavior.Strict);
    private readonly Mock<ILocationRepository> _locations = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    [Fact]
    public async Task Send_InvalidCommand_ValidatesBeforeHandler()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new CreateDeskCommand("", null), TestContext.Current.CancellationToken).AsTask());

        Assert.Contains(exception.Errors, error => string.Equals(error.PropertyName, "Name", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => string.Equals(error.PropertyName, "LocationId", StringComparison.Ordinal));
        Assert.Empty(_desks.Invocations);
    }

    [Fact]
    public async Task Send_InvalidQuery_ValidatesBeforeHandler()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var query = new GetDesksByLocationQuery(Guid.Empty, null, null, null);

        await Assert.ThrowsAsync<ValidationException>(() => sender.Send(query, TestContext.Current.CancellationToken).AsTask());

        Assert.Empty(_desks.Invocations);
    }

    [Fact]
    public async Task Send_ValidCommand_ReturnsHandlerResultAndForwardsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var location = new Location { Id = Guid.NewGuid() };
        _locations.Setup(repository => repository.GetById(location.Id, cancellation.Token)).ReturnsAsync(location);
        _desks.Setup(repository => repository.Add(It.Is<Desk>(desk => desk.Name == "Window" && desk.LocationId == location.Id)));
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChanges(cancellation.Token)).Returns(Task.CompletedTask);
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateDeskCommand("Window", null) { LocationId = location.Id }, cancellation.Token);

        Assert.Equal("Window", result.Name);
        Assert.Equal(location.Id, result.LocationId);
        _locations.VerifyAll();
        _desks.VerifyAll();
        _unitOfWork.VerifyAll();
    }

    [Fact]
    public async Task Send_CommandWithoutValidator_ReturnsUnitAfterHandlerCompletes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = new Location { Id = Guid.NewGuid() };
        _locations.Setup(repository => repository.GetById(location.Id, cancellationToken)).ReturnsAsync(location);
        _locations.Setup(repository => repository.Delete(location));
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChanges(cancellationToken)).Returns(Task.CompletedTask);
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new DeleteLocationCommand(location.Id), cancellationToken);

        Assert.Equal(Unit.Value, result);
        _locations.VerifyAll();
        _unitOfWork.VerifyAll();
    }

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDateTimeProvider>(new DateTimeProvider
        {
            UtcNow = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc)
        });
        services.AddScoped<IUserContext>(_ => new Mock<IUserContext>(MockBehavior.Strict).Object);
        services.AddApplication();

        services.AddScoped(_ => _desks.Object);
        services.AddScoped(_ => _locations.Object);
        services.AddScoped(_ => _unitOfWork.Object);
        services.AddScoped(_ => new Mock<IReservationRepository>(MockBehavior.Strict).Object);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
