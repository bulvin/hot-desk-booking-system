using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Users;

namespace Application.Users.Register;

public record RegisterUserCommand(string Email, string FirstName, string LastName, string Password)
    : ICommand<Guid>;
    
public class RegisterUserHandler: ICommandHandler<RegisterUserCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(IUnitOfWork unitOfWork, IUserRepository repository, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.Exists(request.Email, cancellationToken))
            throw new ApplicationException("Email is already taken");

        var password = _passwordHasher.Hash(request.Password);
        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Password = password,
        };
        var role = await _repository.GetRoleByName(UserRole.Employee.ToString(), cancellationToken)
                   ?? throw new ApplicationException("Role not exists");
        
        user.Roles.Add(role);
        _repository.Add(user);
        await _unitOfWork.SaveChanges(cancellationToken);

        return user.Id;
    }
}