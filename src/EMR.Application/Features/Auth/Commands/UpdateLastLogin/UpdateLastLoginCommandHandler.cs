using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Common.Utilities;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Auth.Commands.UpdateLastLogin;

/// <summary>
/// Handler for UpdateLastLoginCommand
/// </summary>
public class UpdateLastLoginCommandHandler : ICommandHandler<UpdateLastLoginCommand, ResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateLastLoginCommandHandler> _logger;

    public UpdateLastLoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuditLogger auditLogger,
        ICurrentUserService currentUserService,
        ILogger<UpdateLastLoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _auditLogger = auditLogger;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ResultDto> Handle(UpdateLastLoginCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();
        var userAgent = _currentUserService.GetUserAgent();

        try
        {
            _logger.LogInformation("Updating last login for user: {UserId}",
                LogSanitizer.SanitizeUserId(request.UserId));

            // Get user by ID
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}",
                    LogSanitizer.SanitizeUserId(request.UserId));
                return ResultDto.Failure("User not found.");
            }

            // Update last login timestamp
            user.UpdateLastLogin();

            // Update user in repository
            _userRepository.Update(user);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Last login updated successfully for user: {UserId}",
                LogSanitizer.SanitizeUserId(request.UserId));

            // Audit log the authentication
            await _auditLogger.LogAuthenticationAsync(
                userId: request.UserId.ToString(),
                action: "Login",
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: true,
                cancellationToken: cancellationToken);

            return ResultDto.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {UserId}",
                LogSanitizer.SanitizeUserId(request.UserId));

            await _auditLogger.LogAuthenticationAsync(
                userId: request.UserId.ToString(),
                action: "Login",
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: false,
                errorMessage: ex.Message,
                cancellationToken: cancellationToken);

            return ResultDto.Failure("An error occurred while updating the last login timestamp.");
        }
    }
}
