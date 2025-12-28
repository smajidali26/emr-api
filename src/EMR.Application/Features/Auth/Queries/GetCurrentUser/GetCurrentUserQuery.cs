using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Auth.DTOs;

namespace EMR.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Query to get the current authenticated user's details
/// </summary>
public record GetCurrentUserQuery : IQuery<ResultDto<UserDto>>
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public Guid UserId { get; init; }

    public GetCurrentUserQuery(Guid userId)
    {
        UserId = userId;
    }
}
