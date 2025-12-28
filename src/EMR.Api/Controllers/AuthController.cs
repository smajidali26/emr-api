using EMR.Application.Common.Utilities;
using EMR.Application.Features.Auth.Commands.RegisterUser;
using EMR.Application.Features.Auth.Commands.UpdateLastLogin;
using EMR.Application.Features.Auth.DTOs;
using EMR.Application.Features.Auth.Queries.GetCurrentUser;
using EMR.Application.Features.Auth.Queries.GetUserByAzureId;
using EMR.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace EMR.Api.Controllers;

/// <summary>
/// Authentication and user management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator,
        IAntiforgery antiforgery,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user after Azure AD B2C signup
    /// Only allows self-registration with Patient role, or Admin can register users with any role
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registered user details</returns>
    [HttpPost("register")]
    [AllowAnonymous] // Allow anonymous access for self-registration
    [EnableRateLimiting("auth")] // SECURITY: Rate limit registration to prevent abuse
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Register endpoint called for email: {Email}",
            LogSanitizer.SanitizeEmail(request.Email));

        // Authorization check: Only Admin can register non-Patient users
        // Self-registration is only allowed for Patient role
        var isAuthenticated = User.Identity?.IsAuthenticated == true;
        var isAdmin = User.IsInRole("Admin");

        // Check if registering with non-Patient roles
        var hasNonPatientRole = request.Roles.Any(r => r != UserRole.Patient);

        if (hasNonPatientRole && (!isAuthenticated || !isAdmin))
        {
            _logger.LogWarning("Unauthorized registration attempt for non-Patient roles. Email: {Email}, Roles: {Roles}",
                LogSanitizer.SanitizeEmail(request.Email), string.Join(", ", request.Roles));

            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only administrators can register users with Doctor, Nurse, Staff, or Admin roles. Self-registration is limited to Patient role only." });
        }

        var command = new RegisterUserCommand
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            AzureAdB2CId = request.AzureAdB2CId,
            Roles = request.Roles
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("User registration failed for email: {Email}. Error: {Error}",
                LogSanitizer.SanitizeEmail(request.Email), result.ErrorMessage);

            if (result.Errors != null && result.Errors.Any())
            {
                return BadRequest(new
                {
                    message = result.ErrorMessage,
                    errors = result.Errors
                });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("User registered successfully: {Email}",
            LogSanitizer.SanitizeEmail(request.Email));

        return CreatedAtAction(
            nameof(GetCurrentUser),
            new { id = result.Data!.Id },
            result.Data);
    }

    /// <summary>
    /// Get current authenticated user's profile
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Unable to extract user ID from claims");
            return Unauthorized(new { message = "Invalid user credentials" });
        }

        _logger.LogInformation("GetCurrentUser endpoint called for user: {UserId}", userId);

        var query = new GetCurrentUserQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get current user: {UserId}. Error: {Error}",
                userId, result.ErrorMessage);
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Handle post-login processing (update last login timestamp)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("login-callback")]
    [EnableRateLimiting("auth")] // SECURITY: Rate limit login callbacks to prevent brute force
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginCallback(CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId == Guid.Empty)
        {
            // Try to find user by Azure AD B2C ID if user ID is not available
            var azureAdB2CId = GetAzureAdB2CIdFromClaims();
            if (string.IsNullOrEmpty(azureAdB2CId))
            {
                _logger.LogWarning("Unable to extract user ID or Azure AD B2C ID from claims");
                return Unauthorized(new { message = "Invalid user credentials" });
            }

            // Get user by Azure AD B2C ID
            var userQuery = new GetUserByAzureIdQuery(azureAdB2CId);
            var userResult = await _mediator.Send(userQuery, cancellationToken);

            if (!userResult.IsSuccess)
            {
                _logger.LogWarning("User not found with Azure AD B2C ID: {AzureAdB2CId}",
                    LogSanitizer.SanitizeAzureId(azureAdB2CId));
                return NotFound(new { message = "User not found. Please complete registration." });
            }

            userId = userResult.Data!.Id;
        }

        _logger.LogInformation("LoginCallback endpoint called for user: {UserId}", userId);

        var command = new UpdateLastLoginCommand(userId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update last login for user: {UserId}. Error: {Error}",
                userId, result.ErrorMessage);
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Login processed successfully" });
    }

    /// <summary>
    /// Get a CSRF token for making state-changing requests
    /// SECURITY: Required for POST, PUT, PATCH, DELETE operations
    /// </summary>
    /// <returns>CSRF token for use in X-CSRF-Token header</returns>
    [HttpGet("csrf-token")]
    [AllowAnonymous] // Allow fetching token before full authentication
    [EnableRateLimiting("auth")] // SECURITY: Rate limit CSRF token requests to prevent token farming
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        // Set the token in a cookie that JavaScript can read
        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false, // Allow JavaScript to read
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Path = "/",
            MaxAge = TimeSpan.FromHours(1) // Token expires after 1 hour
        });

        return Ok(new
        {
            token = tokens.RequestToken,
            headerName = "X-CSRF-Token"
        });
    }

    /// <summary>
    /// Get user ID from JWT claims
    /// </summary>
    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Guid.Empty;
        }

        return userId;
    }

    /// <summary>
    /// Get Azure AD B2C ID from JWT claims
    /// </summary>
    private string GetAzureAdB2CIdFromClaims()
    {
        return User.FindFirst("oid")?.Value
               ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
               ?? string.Empty;
    }
}
