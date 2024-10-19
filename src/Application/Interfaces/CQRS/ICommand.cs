using MediatR;

namespace Application.Interfaces.CQRS;

public interface ICommand : IRequest<Unit>;

public interface ICommand<out TResponse>
    : IRequest<TResponse>;