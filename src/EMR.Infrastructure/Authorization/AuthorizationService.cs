using EMR.Application.Common.Authorization;
using EMR.Application.Common.Interfaces;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Authorization;

/// <summary>
/// Implementation of IAuthorizationService for RBAC and ABAC
/// Combines role-based and resource-based authorization
/// SECURITY: Implements principle of least privilege
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IResourceAuthorizationRepository _resourceAuthorizationRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthorizationService> _logger;

    // SECURITY: Short cache TTL to balance performance and security
    // Permission changes take effect within this time window
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string UserCacheKeyPrefix = "AuthUser_";

    public AuthorizationService(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IResourceAuthorizationRepository resourceAuthorizationRepository,
        IPatientRepository patientRepository,
        IMemoryCache cache,
        ILogger<AuthorizationService> logger)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _resourceAuthorizationRepository = resourceAuthorizationRepository;
        _patientRepository = patientRepository;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get cached user or fetch from repository
    /// Uses IMemoryCache with TTL for security
    /// </summary>
    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
            return null;

        return await GetUserByIdCachedAsync(userId.Value, cancellationToken);
    }

    /// <summary>
    /// Get user by ID with caching
    /// SECURITY: Uses short TTL to ensure permission changes take effect quickly
    /// </summary>
    private async Task<User?> GetUserByIdCachedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{UserCacheKeyPrefix}{userId}";

        if (_cache.TryGetValue(cacheKey, out User? cachedUser))
        {
            return cachedUser;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheTtl)
                .SetSize(1); // For memory pressure handling

            _cache.Set(cacheKey, user, cacheOptions);
        }

        return user;
    }

    /// <summary>
    /// Invalidate user cache entry
    /// Call this when user roles or permissions change
    /// </summary>
    public void InvalidateUserCache(Guid userId)
    {
        var cacheKey = $"{UserCacheKeyPrefix}{userId}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("Invalidated authorization cache for user {UserId}", userId);
    }

    public async Task<bool> HasPermissionAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Permission check failed: No authenticated user");
            return false;
        }

        return await UserHasPermissionAsync(userId.Value, permission, cancellationToken);
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, Permission permission, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user with roles
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Permission check failed: User {UserId} not found or inactive", userId);
                return false;
            }

            // Admin has all permissions
            if (user.HasRole(UserRole.Admin))
            {
                return true;
            }

            // Check role-based permissions using the permission matrix
            foreach (var role in user.Roles)
            {
                if (RolePermissionMatrix.RoleHasPermission(role, permission))
                {
                    return true;
                }
            }

            _logger.LogDebug("User {UserId} does not have permission {Permission}", userId, permission);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<bool> HasResourceAccessAsync(
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Resource access check failed: No authenticated user");
            return false;
        }

        return await UserHasResourceAccessAsync(userId.Value, resourceType, resourceId, permission, cancellationToken);
    }

    public async Task<bool> UserHasResourceAccessAsync(
        Guid userId,
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User {UserId} not found or inactive during resource access check", userId);
                return false;
            }

            // Admin has access to all resources
            if (user.HasRole(UserRole.Admin))
            {
                return true;
            }

            // SECURITY FIX: Handle Patient role with PatientsViewOwn permission
            // When a handler checks for PatientsView on a Patient resource,
            // Patient role users should be allowed if it's their own record
            if (resourceType == ResourceType.Patient &&
                permission == Permission.PatientsView &&
                user.HasRole(UserRole.Patient))
            {
                // Patient role doesn't have PatientsView, but has PatientsViewOwn
                // Check if this is their own record
                var hasViewOwnPermission = await UserHasPermissionAsync(userId, Permission.PatientsViewOwn, cancellationToken);
                if (hasViewOwnPermission)
                {
                    var isOwnRecord = await IsOwnPatientRecordAsync(userId, resourceType, resourceId, cancellationToken);
                    if (isOwnRecord)
                    {
                        return true;
                    }
                    _logger.LogWarning(
                        "Patient {UserId} attempted to access patient record {ResourceId} that is not their own",
                        userId, resourceId);
                    return false;
                }
            }

            // Check if user has the permission at role level
            var hasPermission = await UserHasPermissionAsync(userId, permission, cancellationToken);
            if (!hasPermission)
            {
                _logger.LogDebug(
                    "User {UserId} does not have permission {Permission} for resource access",
                    userId,
                    permission);
                return false;
            }

            // Check resource-level authorization (explicit access grants)
            var hasResourceAccess = await _resourceAuthorizationRepository.HasResourcePermissionAsync(
                userId,
                resourceType,
                resourceId,
                permission,
                cancellationToken);

            if (hasResourceAccess)
            {
                return true;
            }

            // SECURITY FIX: Medical staff (Doctor, Nurse) must have explicit resource authorization
            // This implements HIPAA's "minimum necessary" rule - staff only access patients they're treating
            // Remove blanket access that was previously granting access to ALL patient records

            // For medical staff accessing patient data without explicit authorization,
            // we can implement care team assignment checking here in the future
            // For now, they must have explicit resource authorization or be on the care team

            _logger.LogDebug(
                "User {UserId} does not have explicit access to resource {ResourceType}/{ResourceId}",
                userId,
                resourceType,
                resourceId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking resource access for user {UserId} on {ResourceType}/{ResourceId}",
                userId,
                resourceType,
                resourceId);
            // SECURITY: Fail closed - deny access on errors
            return false;
        }
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            return Enumerable.Empty<Permission>();
        }

        return await GetUserPermissionsAsync(userId.Value, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return Enumerable.Empty<Permission>();
            }

            // Collect all permissions from all user roles
            var permissions = new HashSet<Permission>();
            foreach (var role in user.Roles)
            {
                var rolePermissions = RolePermissionMatrix.GetPermissionsForRole(role);
                foreach (var permission in rolePermissions)
                {
                    permissions.Add(permission);
                }
            }

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return Enumerable.Empty<Permission>();
        }
    }

    public async Task<IEnumerable<Guid>> GetAuthorizedResourceIdsAsync(
        ResourceType resourceType,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            return Enumerable.Empty<Guid>();
        }

        try
        {
            var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return Enumerable.Empty<Guid>();
            }

            // SECURITY FIX: Only Admin has blanket access to all resources
            // Doctors and Nurses must have explicit resource authorization (HIPAA minimum necessary)
            if (user.HasRole(UserRole.Admin))
            {
                return Enumerable.Empty<Guid>(); // Empty = no filtering needed for Admin
            }

            // SECURITY FIX: Handle Patient role for patient searches
            // Patient role users can only see their own patient record
            if (resourceType == ResourceType.Patient &&
                permission == Permission.PatientsView &&
                user.HasRole(UserRole.Patient))
            {
                // Find the patient record that belongs to this user (by email match)
                var ownPatientId = await GetOwnPatientIdAsync(userId.Value, cancellationToken);
                if (ownPatientId.HasValue)
                {
                    return new[] { ownPatientId.Value };
                }
                return Enumerable.Empty<Guid>();
            }

            // For all other roles (including Doctors/Nurses), get specific authorized resources
            // This enforces HIPAA's "minimum necessary" standard
            return await _resourceAuthorizationRepository.GetAuthorizedResourceIdsAsync(
                userId.Value,
                resourceType,
                permission,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting authorized resource IDs for user on {ResourceType}",
                resourceType);
            return Enumerable.Empty<Guid>();
        }
    }

    public async Task RequirePermissionAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        var hasPermission = await HasPermissionAsync(permission, cancellationToken);
        if (!hasPermission)
        {
            var userId = _currentUserService.GetUserId();
            _logger.LogWarning(
                "Access denied: User {UserId} lacks permission {Permission}",
                userId,
                permission);
            throw new UnauthorizedAccessException($"You do not have permission to perform this action. Required permission: {permission}");
        }
    }

    public async Task RequireResourceAccessAsync(
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await HasResourceAccessAsync(resourceType, resourceId, permission, cancellationToken);
        if (!hasAccess)
        {
            var userId = _currentUserService.GetUserId();
            _logger.LogWarning(
                "Access denied: User {UserId} lacks access to resource {ResourceType}/{ResourceId} with permission {Permission}",
                userId,
                resourceType,
                resourceId,
                permission);
            throw new UnauthorizedAccessException($"You do not have access to this resource");
        }
    }

    /// <summary>
    /// Async version of HasAnyRole - preferred to avoid deadlocks
    /// </summary>
    public async Task<bool> HasAnyRoleAsync(CancellationToken cancellationToken, params UserRole[] roles)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        return user.Roles.Any(r => roles.Contains(r));
    }

    /// <summary>
    /// Sync version - uses cached user only, no blocking DB calls
    /// </summary>
    public bool HasAnyRole(params UserRole[] roles)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            return false;
        }

        // SECURITY FIX: Only use cached user to avoid deadlocks
        // If cache miss, return false (fail closed) rather than block
        var cacheKey = $"{UserCacheKeyPrefix}{userId}";
        if (_cache.TryGetValue(cacheKey, out User? cachedUser) && cachedUser != null)
        {
            return cachedUser.Roles.Any(r => roles.Contains(r));
        }

        // Log warning about cache miss - caller should use async version
        _logger.LogWarning(
            "HasAnyRole called without cached user for {UserId}. Use HasAnyRoleAsync for reliable results.",
            userId);
        return false;
    }

    /// <summary>
    /// Async version of IsAdmin - preferred to avoid deadlocks
    /// </summary>
    public async Task<bool> IsAdminAsync(CancellationToken cancellationToken = default)
    {
        return await HasAnyRoleAsync(cancellationToken, UserRole.Admin);
    }

    public bool IsAdmin()
    {
        return HasAnyRole(UserRole.Admin);
    }

    public Guid? GetCurrentUserId()
    {
        return _currentUserService.GetUserId();
    }

    /// <summary>
    /// Async version of GetCurrentUserRoles - preferred to avoid deadlocks
    /// </summary>
    public async Task<IEnumerable<UserRole>> GetCurrentUserRolesAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user == null || !user.IsActive)
        {
            return Enumerable.Empty<UserRole>();
        }

        return user.Roles;
    }

    /// <summary>
    /// Sync version - uses cached user only, no blocking DB calls
    /// </summary>
    public IEnumerable<UserRole> GetCurrentUserRoles()
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            return Enumerable.Empty<UserRole>();
        }

        // SECURITY FIX: Only use cached user to avoid deadlocks
        // If cache miss, return empty (fail closed) rather than block
        var cacheKey = $"{UserCacheKeyPrefix}{userId}";
        if (_cache.TryGetValue(cacheKey, out User? cachedUser) && cachedUser != null)
        {
            return cachedUser.Roles;
        }

        // Log warning about cache miss - caller should use async version
        _logger.LogWarning(
            "GetCurrentUserRoles called without cached user for {UserId}. Use GetCurrentUserRolesAsync for reliable results.",
            userId);
        return Enumerable.Empty<UserRole>();
    }

    private async Task<bool> IsOwnPatientRecordAsync(
        Guid userId,
        ResourceType resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        // SECURITY FIX: Properly validate patient record ownership
        // Only allow access if the patient record belongs to the authenticated user

        if (resourceType != ResourceType.Patient)
        {
            return false;
        }

        try
        {
            // Get the user to verify their identity
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning(
                    "User {UserId} not found or inactive during patient ownership check",
                    userId);
                return false;
            }

            // Get the patient record
            var patient = await _patientRepository.GetByIdAsync(resourceId, cancellationToken);
            if (patient == null)
            {
                _logger.LogWarning(
                    "Patient record {ResourceId} not found during ownership check for user {UserId}",
                    resourceId,
                    userId);
                return false;
            }

            // Check ownership by matching email addresses (case-insensitive)
            // Patient role users have a corresponding patient record with the same email
            var isOwner = !string.IsNullOrEmpty(patient.Email) &&
                          !string.IsNullOrEmpty(user.Email) &&
                          string.Equals(patient.Email, user.Email, StringComparison.OrdinalIgnoreCase);

            if (!isOwner)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access patient record {ResourceId} that belongs to another user",
                    userId,
                    resourceId);
            }

            return isOwner;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking patient record ownership for user {UserId} on resource {ResourceId}",
                userId,
                resourceId);
            // SECURITY: Fail closed - deny access on errors
            return false;
        }
    }

    /// <summary>
    /// Gets the patient ID that belongs to a user (by email match)
    /// Used for Patient role users to find their own record
    /// </summary>
    private async Task<Guid?> GetOwnPatientIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                return null;
            }

            // Find patient by matching email
            var patients = await _patientRepository.GetByEmailAsync(user.Email, cancellationToken);
            // Return the first active patient with matching email
            var patient = patients.FirstOrDefault(p => p.IsActive);
            return patient?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting own patient ID for user {UserId}", userId);
            return null;
        }
    }
}
