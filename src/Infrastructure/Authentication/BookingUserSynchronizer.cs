using Domain;
using Domain.Users;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Authentication;

internal sealed class BookingUserSynchronizer(AppDbContext dbContext, IUnitOfWork unitOfWork)
{
    public async Task Synchronize(Guid id, string firstName, string lastName, string email, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            user = new User { Id = id, FirstName = firstName, LastName = lastName, Email = email };
            dbContext.Users.Add(user);
            try
            {
                await unitOfWork.SaveChanges(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                dbContext.Entry(user).State = EntityState.Detached;
                user = await dbContext.Users.SingleAsync(existing => existing.Id == id, cancellationToken);
            }
        }

        if (string.Equals(user.FirstName, firstName, StringComparison.Ordinal)
            && string.Equals(user.LastName, lastName, StringComparison.Ordinal)
            && string.Equals(user.Email, email, StringComparison.Ordinal))
            return;

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        await unitOfWork.SaveChanges(cancellationToken);
    }
}
