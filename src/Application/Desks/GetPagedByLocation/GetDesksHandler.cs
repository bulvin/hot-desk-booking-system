using Application.Dtos;
using Application.Interfaces.CQRS;
using AutoMapper;
using Domain.Desks;

namespace Application.Desks.GetPagedByLocation;

public record GetDesksByLocationQuery(Guid LocationId, bool? IsAvailable, int? Page, int? PageSize) : IQuery<PagedDto<DeskDto>>;

public record GetDesksByLocationRequest(bool? IsAvailable, int? Page, int? PageSize);
public class GetDesksHandler : IQueryHandler<GetDesksByLocationQuery, PagedDto<DeskDto>>
{
    private readonly IDeskRepository _repository;
    private readonly IMapper _mapper;

    public GetDesksHandler(IDeskRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
    public async Task<PagedDto<DeskDto>> Handle(GetDesksByLocationQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 30;
        
        var (desks, totalCount) = await _repository.GetDesksByLocation(
            query.LocationId,
            query.IsAvailable,
            page,
            pageSize,
            cancellationToken);

        var desksDtos = _mapper.Map<List<DeskDto>>(desks);
        return new PagedDto<DeskDto>(desksDtos, page, pageSize, totalCount);
    }
}