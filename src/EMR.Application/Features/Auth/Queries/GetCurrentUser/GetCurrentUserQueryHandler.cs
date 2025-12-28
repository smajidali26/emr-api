using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Common.Utilities;
using EMR.Application.Features.Auth.DTOs;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Handler for GetCurrentUserQuery
/// </summary>
public class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, ResultDto<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetCurrentUserQueryHandler> _logger;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        IAuditLogger auditLogger,
        ICurrentUserService currentUserService,
        ILogger<GetCurrentUserQueryHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogger = auditLogger;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ResultDto<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();

        try
        {
            _logger.LogInformation("Getting current user details for user: {UserId}",
                LogSanitizer.SanitizeUserId(request.UserId));

            // Get user by ID
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}",
                    LogSanitizer.SanitizeUserId(request.UserId));

                await _auditLogger.LogUserAccessAsync(
                    performedBy: request.UserId.ToString(),
                    action: "GetCurrentUser",
                    targetUserId: request.UserId.ToString(),
                    ipAddress: ipAddress,
                    details: "User not found",
                    cancellationToken: cancellationToken);

                return ResultDto<UserDto>.Failure("User not found.");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("User account is inactive: {UserId}",
                    LogSanitizer.SanitizeUserId(request.UserId));

                await _auditLogger.LogUserAccessAsync(
                    performedBy: request.UserId.ToString(),
                    action: "GetCurrentUser",
                    targetUserId: request.UserId.ToString(),
                    ipAddress: ipAddress,
                    details: "User account is inactive",
                    cancellationToken: cancellationToken);

                return ResultDto<UserDto>.Failure("User account is inactive.");
            }

            _logger.LogInformation("Successfully retrieved current user details for: {UserId}",
                LogSanitizer.SanitizeUserId(request.UserId));

            await _auditLogger.LogUserAccessAsync(
                performedBy: request.UserId.ToString(),
                action: "GetCurrentUser",
                targetUserId: request.UserId.ToString(),
                ipAddress: ipAddress,
                details: "Profile accessed successfully",
                cancellationToken: cancellationToken);

            // Map to DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                AzureAdB2CId = user.AzureAdB2CId,
                Roles = user.Roles.ToList(),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return ResultDto<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user details for user: {UserId}",
                LogSanitizer.SanitizeUserId(request.UserId));

            await _auditLogger.LogUserAccessAsync(
                performedBy: request.UserId.ToString(),
                action: "GetCurrentUser",
                targetUserId: request.UserId.ToString(),
                ipAddress: ipAddress,
                details: $"Error: {ex.Message}",
                cancellationToken: cancellationToken);

            return ResultDto<UserDto>.Failure("An error occurred while retrieving user details.");
        }
    }
}
