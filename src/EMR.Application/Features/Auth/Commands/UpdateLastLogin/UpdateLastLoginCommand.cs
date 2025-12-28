using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;

namespace EMR.Application.Features.Auth.Commands.UpdateLastLogin;

/// <summary>
/// Command to update user's last login timestamp
/// </summary>
public record UpdateLastLoginCommand : ICommand<ResultDto>
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public Guid UserId { get; init; }

    public UpdateLastLoginCommand(Guid userId)
    {
        UserId = userId;
    }
}
