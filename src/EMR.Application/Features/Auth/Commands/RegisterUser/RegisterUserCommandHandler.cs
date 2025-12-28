using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Exceptions;
using EMR.Application.Common.Interfaces;
using EMR.Application.Common.Utilities;
using EMR.Application.Features.Auth.DTOs;
using EMR.Domain.Entities;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Auth.Commands.RegisterUser;

/// <summary>
/// Handler for RegisterUserCommand
/// </summary>
public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, ResultDto<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogger auditLogger,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<ResultDto<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();
        var createdBy = _currentUserService.GetUserEmail() ?? "system";

        try
        {
            _logger.LogInformation("Registering user with email: {Email}",
                LogSanitizer.SanitizeEmail(request.Email));

            // Create new user entity
            var user = new User(
                email: request.Email,
                firstName: request.FirstName,
                lastName: request.LastName,
                azureAdB2CId: request.AzureAdB2CId,
                roles: request.Roles,
                createdBy: createdBy
            );

            // Add user to repository
            await _userRepository.AddAsync(user, cancellationToken);

            // Save changes - this will handle race conditions via unique constraint violations
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {Email} registered successfully with ID: {UserId}",
                LogSanitizer.SanitizeEmail(request.Email), LogSanitizer.SanitizeUserId(user.Id));

            // Audit log the registration
            await _auditLogger.LogUserRegistrationAsync(
                userId: user.Id.ToString(),
                email: request.Email,
                roles: request.Roles,
                ipAddress: ipAddress,
                success: true,
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
        catch (DuplicateEntityException ex)
        {
            // Handle race condition: another request created the user between our insert attempt
            _logger.LogWarning(ex, "User registration failed due to duplicate constraint. Email: {Email}, Azure ID: {AzureId}",
                LogSanitizer.SanitizeEmail(request.Email),
                LogSanitizer.SanitizeAzureId(request.AzureAdB2CId));

            // Audit log the failed registration
            await _auditLogger.LogUserRegistrationAsync(
                userId: "N/A",
                email: request.Email,
                roles: request.Roles,
                ipAddress: ipAddress,
                success: false,
                errorMessage: ex.Message,
                cancellationToken: cancellationToken);

            return ResultDto<UserDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error during user registration: {Message}", ex.Message);

            await _auditLogger.LogUserRegistrationAsync(
                userId: "N/A",
                email: request.Email,
                roles: request.Roles,
                ipAddress: ipAddress,
                success: false,
                errorMessage: ex.Message,
                cancellationToken: cancellationToken);

            return ResultDto<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user with email: {Email}",
                LogSanitizer.SanitizeEmail(request.Email));

            await _auditLogger.LogUserRegistrationAsync(
                userId: "N/A",
                email: request.Email,
                roles: request.Roles,
                ipAddress: ipAddress,
                success: false,
                errorMessage: "An unexpected error occurred",
                cancellationToken: cancellationToken);

            return ResultDto<UserDto>.Failure("An error occurred while registering the user. Please try again.");
        }
    }
}
