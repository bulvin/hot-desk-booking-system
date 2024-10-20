using Domain.Desks;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class DeskRepository : IDeskRepository
{
    private readonly AppDbContext _dbContext;

    public DeskRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Desk desk)
    {
        _dbContext.Desks.Add(desk);
    }
}