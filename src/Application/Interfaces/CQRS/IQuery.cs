using MediatR;

namespace Application.Interfaces.CQRS;

public interface IQuery<out TResponse>
    : IRequest<TResponse> where TResponse 
    : notnull;