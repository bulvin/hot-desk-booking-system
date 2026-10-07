using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Exceptions.Users;
using Domain.Users;

namespace Application.Users.Register;

public class RegisterUserHandler(IUnitOfWork unitOfWork, IUserRepository repository, IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await repository.Exists(request.Email, cancellationToken))
            throw new EmailAlreadyExistsException(request.Email);

        var password = passwordHasher.Hash(request.Password);
        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Password = password,
        };
        var role = await repository.GetRoleByName(UserRole.Employee.ToString(), cancellationToken)
                   ?? throw new RoleNotFoundException(UserRole.Employee.ToString());

        user.Roles.Add(role);
        repository.Add(user);
        await unitOfWork.SaveChanges(cancellationToken);

        return user.Id;
    }
}