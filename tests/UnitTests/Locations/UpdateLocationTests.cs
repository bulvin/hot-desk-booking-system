using Application.Dtos;
using Application.Locations.Update;
using Domain;
using Domain.Desks;
using Domain.Exceptions.Locations;
using Domain.Locations;
using Domain.Reservations;
using Moq;

namespace UnitTests.Locations;

public class UpdateLocationTests
{
    private static readonly AddressDto ValidAddress = new("Street", "10", "City", "00-001");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_PreservesIdentityAndRelationships(bool unchanged)
    {
        var location = new Location { Id = Guid.NewGuid(), Name = "Original", Address = new Address { Street = "Old", BuildingNumber = "1", City = "Old city", PostalCode = "Old code" } };
        var desk = new Desk { Id = Guid.NewGuid(), LocationId = location.Id, Location = location };
        var reservation = new Reservation { Id = Guid.NewGuid(), DeskId = desk.Id, Desk = desk };
        desk.Reservations.Add(reservation);
        location.Desks.Add(desk);
        var address = unchanged ? new AddressDto("Old", "1", "Old city", "Old code") : ValidAddress;
        var command = new UpdateLocationCommand(location.Id, unchanged ? location.Name : "Updated", address);
        var repository = new Mock<ILocationRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        repository.Setup(repo => repo.GetById(location.Id, It.IsAny<CancellationToken>())).ReturnsAsync(location);
        repository.Setup(repo => repo.IsDuplicate(It.Is<Location>(candidate => candidate.Id == location.Id && candidate.Name == command.Name && candidate.Address.Street == address.Street), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        unitOfWork.Setup(unit => unit.SaveChanges(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await new UpdateLocationHandler(repository.Object, unitOfWork.Object).Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(new LocationDto(location.Id, command.Name, address), result);
        Assert.Equal(command.Name, location.Name);
        Assert.Equal(address, new AddressDto(location.Address.Street, location.Address.BuildingNumber, location.Address.City, location.Address.PostalCode));
        Assert.Same(desk, Assert.Single(location.Desks));
        Assert.Same(reservation, Assert.Single(desk.Reservations));
        repository.VerifyAll();
        unitOfWork.VerifyAll();
    }

    [Fact]
    public async Task Duplicate_DoesNotMutateOrSave()
    {
        var location = new Location { Id = Guid.NewGuid(), Name = "Original", Address = new Address { Street = "Original street" } };
        var repository = new Mock<ILocationRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        repository.Setup(repo => repo.GetById(location.Id, It.IsAny<CancellationToken>())).ReturnsAsync(location);
        repository.Setup(repo => repo.IsDuplicate(It.IsAny<Location>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<LocationAlreadyExistsException>(async () => await new UpdateLocationHandler(repository.Object, unitOfWork.Object)
            .Handle(new UpdateLocationCommand(location.Id, "Updated", ValidAddress), TestContext.Current.CancellationToken));

        Assert.Equal("Original", location.Name);
        Assert.Equal("Original street", location.Address.Street);
        Assert.Empty(unitOfWork.Invocations);
    }

    [Fact]
    public async Task MissingLocation_DoesNotSave()
    {
        var repository = new Mock<ILocationRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        repository.Setup(repo => repo.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Location?)null);
        await Assert.ThrowsAsync<LocationNotFoundException>(async () => await new UpdateLocationHandler(repository.Object, unitOfWork.Object)
            .Handle(new UpdateLocationCommand(Guid.NewGuid(), "Name", ValidAddress), TestContext.Current.CancellationToken));
        Assert.Empty(unitOfWork.Invocations);
    }
}
