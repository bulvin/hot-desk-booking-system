using Application.Dtos;
using Application.Interfaces.CQRS;
using AutoMapper;
using Domain;
using Domain.Locations;

namespace Application.Locations.Create;

public record CreateLocationCommand(string Name, AddressDto Address) : ICommand<LocationDto>;

public class CreateLocationHandler : ICommandHandler<CreateLocationCommand, LocationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocationRepository _repository;
    private readonly IMapper _mapper;
    
    public CreateLocationHandler(IUnitOfWork unitOfWork, ILocationRepository repository, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
        _mapper = mapper;
    }
    public async Task<LocationDto> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var location = new Location
        {
            Name = command.Name,
            Address = new Address
            {
                Street = command.Address.Street,
                BuildingNumber = command.Address.BuildingNumber,
                City = command.Address.City,
                PostalCode = command.Address.PostalCode
            }
        };
        
        _repository.Add(location);
        await _unitOfWork.SaveChanges(cancellationToken);
        
        var locationDto = _mapper.Map<LocationDto>(location);
        return locationDto;
    }
}