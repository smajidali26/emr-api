using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Common.Utilities;
using EMR.Application.Features.Auth.DTOs;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Auth.Queries.GetUserByAzureId;

/// <summary>
/// Handler for GetUserByAzureIdQuery
/// </summary>
public class GetUserByAzureIdQueryHandler : IQueryHandler<GetUserByAzureIdQuery, ResultDto<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserByAzureIdQueryHandler> _logger;

    public GetUserByAzureIdQueryHandler(
        IUserRepository userRepository,
        IAuditLogger auditLogger,
        ICurrentUserService currentUserService,
        ILogger<GetUserByAzureIdQueryHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogger = auditLogger;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ResultDto<UserDto>> Handle(GetUserByAzureIdQuery request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();
        var performedBy = _currentUserService.GetUserId()?.ToString() ?? "Anonymous";

        try
        {
            _logger.LogInformation("Getting user by Azure AD B2C ID: {AzureAdB2CId}",
                LogSanitizer.SanitizeAzureId(request.AzureAdB2CId));

            // Get user by Azure AD B2C ID
            var user = await _userRepository.GetByAzureAdB2CIdAsync(request.AzureAdB2CId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found with Azure AD B2C ID: {AzureAdB2CId}",
                    LogSanitizer.SanitizeAzureId(request.AzureAdB2CId));

                await _auditLogger.LogUserAccessAsync(
                    performedBy: performedBy,
                    action: "GetUserByAzureId",
                    targetUserId: null,
                    ipAddress: ipAddress,
                    details: "User not found",
                    cancellationToken: cancellationToken);

                return ResultDto<UserDto>.Failure("User not found.");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted access. Azure AD B2C ID: {AzureAdB2CId}",
                    LogSanitizer.SanitizeAzureId(request.AzureAdB2CId));

                await _auditLogger.LogUserAccessAsync(
                    performedBy: performedBy,
                    action: "GetUserByAzureId",
                    targetUserId: user.Id.ToString(),
                    ipAddress: ipAddress,
                    details: "User account is inactive",
                    cancellationToken: cancellationToken);

                return ResultDto<UserDto>.Failure("User account is inactive. Please contact support.");
            }

            _logger.LogInformation("Successfully retrieved user by Azure AD B2C ID: {AzureAdB2CId}",
                LogSanitizer.SanitizeAzureId(request.AzureAdB2CId));

            await _auditLogger.LogUserAccessAsync(
                performedBy: performedBy,
                action: "GetUserByAzureId",
                targetUserId: user.Id.ToString(),
                ipAddress: ipAddress,
                details: "User retrieved successfully",
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
            _logger.LogError(ex, "Error getting user by Azure AD B2C ID: {AzureAdB2CId}",
                LogSanitizer.SanitizeAzureId(request.AzureAdB2CId));

            await _auditLogger.LogUserAccessAsync(
                performedBy: performedBy,
                action: "GetUserByAzureId",
                targetUserId: null,
                ipAddress: ipAddress,
                details: $"Error: {ex.Message}",
                cancellationToken: cancellationToken);

            return ResultDto<UserDto>.Failure("An error occurred while retrieving user details.");
        }
    }
}
