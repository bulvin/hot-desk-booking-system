using Application;
using Application.Desks.GetPagedByLocation;
using Application.Interfaces;
using Application.Locations.Delete;
using Application.Users.Login;
using Domain;
using Domain.Desks;
using Domain.Locations;
using Domain.Reservations;
using Domain.Users;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace UnitTests.Behaviors;

public sealed class MediatorTests
{
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IDeskRepository> _desks = new(MockBehavior.Strict);
    private readonly Mock<ILocationRepository> _locations = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IPasswordHasher> _passwordHasher = new(MockBehavior.Strict);
    private readonly Mock<ITokenProvider> _tokenProvider = new(MockBehavior.Strict);

    [Fact]
    public async Task Send_InvalidCommand_ValidatesBeforeHandler()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new LoginUserCommand("invalid-email", ""), TestContext.Current.CancellationToken).AsTask());

        Assert.Contains(exception.Errors, error => string.Equals(error.PropertyName, "Email", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => string.Equals(error.PropertyName, "Password", StringComparison.Ordinal));
        Assert.Empty(_users.Invocations);
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
        var user = new User { Email = "user@example.com", Password = "stored-hash" };
        _users.Setup(repository => repository.GetByEmail(user.Email, cancellation.Token)).ReturnsAsync(user);
        _passwordHasher.Setup(hasher => hasher.Verify("password", user.Password)).Returns(true);
        _tokenProvider.Setup(provider => provider.GenerateToken(user)).Returns("access-token");
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new LoginUserCommand(user.Email, "password"), cancellation.Token);

        Assert.Equal("access-token", result);
        _users.VerifyAll();
        _passwordHasher.VerifyAll();
        _tokenProvider.VerifyAll();
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

    [Fact]
    public void Registration_UsesScopedMediatorAndHandlers()
    {
        using var provider = CreateProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ISender>());
        Assert.Same(firstScope.ServiceProvider.GetRequiredService<ISender>(), firstScope.ServiceProvider.GetRequiredService<ISender>());
        Assert.NotSame(firstScope.ServiceProvider.GetRequiredService<ISender>(), secondScope.ServiceProvider.GetRequiredService<ISender>());
        Assert.Same(firstScope.ServiceProvider.GetRequiredService<LoginUserHandler>(), firstScope.ServiceProvider.GetRequiredService<LoginUserHandler>());
        Assert.NotSame(firstScope.ServiceProvider.GetRequiredService<LoginUserHandler>(), secondScope.ServiceProvider.GetRequiredService<LoginUserHandler>());
    }

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddApplication();
        services.AddScoped(_ => _users.Object);
        services.AddScoped(_ => _desks.Object);
        services.AddScoped(_ => _locations.Object);
        services.AddScoped(_ => _unitOfWork.Object);
        services.AddScoped(_ => new Mock<IReservationRepository>(MockBehavior.Strict).Object);
        services.AddSingleton(_passwordHasher.Object);
        services.AddSingleton(_tokenProvider.Object);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
