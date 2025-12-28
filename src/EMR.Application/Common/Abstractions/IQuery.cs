using MediatR;

namespace EMR.Application.Common.Abstractions;

/// <summary>
/// Marker interface for queries
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
