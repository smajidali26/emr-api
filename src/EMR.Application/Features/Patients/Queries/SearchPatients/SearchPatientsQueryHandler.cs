using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Patients.DTOs;
using EMR.Domain.Enums;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Patients.Queries.SearchPatients;

/// <summary>
/// Handler for SearchPatientsQuery
/// HIPAA Compliance: Logs all patient search activities
/// SECURITY: Results filtered based on user authorization
/// </summary>
public class SearchPatientsQueryHandler : IQueryHandler<SearchPatientsQuery, ResultDto<PagedResultDto<PatientSearchResultDto>>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<SearchPatientsQueryHandler> _logger;

    public SearchPatientsQueryHandler(
        IPatientRepository patientRepository,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IAuditLogger auditLogger,
        ILogger<SearchPatientsQueryHandler> logger)
    {
        _patientRepository = patientRepository;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<ResultDto<PagedResultDto<PatientSearchResultDto>>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();
        var performedBy = _currentUserService.GetUserEmail() ?? "system";
        var userId = _currentUserService.GetUserId();

        try
        {
            _logger.LogInformation("Searching patients. SearchTerm: {SearchTerm}, Page: {PageNumber}, PageSize: {PageSize}",
                request.SearchTerm ?? "N/A", request.PageNumber, request.PageSize);

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return ResultDto<PagedResultDto<PatientSearchResultDto>>.Failure("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return ResultDto<PagedResultDto<PatientSearchResultDto>>.Failure("Page size must be between 1 and 100");
            }

            // SECURITY FIX: Get authorized patient IDs for the current user
            // This enforces HIPAA's "minimum necessary" standard
            var authorizedPatientIds = await _authorizationService.GetAuthorizedResourceIdsAsync(
                ResourceType.Patient,
                Permission.PatientsView,
                cancellationToken);

            // Determine authorization filter for database query
            // Empty set from Admin means "no filtering" (all access) - pass null
            // Non-empty set means filter to those IDs only
            var isAdmin = await _authorizationService.IsAdminAsync(cancellationToken);
            var authorizedIdsSet = authorizedPatientIds.ToHashSet();

            // SECURITY: Determine the filter to pass to repository
            // null = no filter (admin), HashSet = filter to specific IDs
            IReadOnlySet<Guid>? authorizationFilter = null;
            if (!isAdmin || authorizedIdsSet.Count > 0)
            {
                // Non-admin or admin with specific restrictions
                authorizationFilter = authorizedIdsSet;
            }

            // SECURITY FIX: Search with authorization filter at database level
            // This fixes pagination and prevents leaking total patient count
            var (patients, totalCount) = await _patientRepository.SearchPatientsAsync(
                request.SearchTerm,
                request.PageNumber,
                request.PageSize,
                authorizationFilter,
                cancellationToken);

            // Log if non-admin user has no authorized patients
            if (authorizationFilter != null && authorizationFilter.Count == 0)
            {
                _logger.LogWarning(
                    "SEARCH_FILTERED | UserId: {UserId} | Reason: No authorized patient records",
                    userId);
            }

            // Audit log the search
            await _auditLogger.LogDataAccessAsync(
                userId: userId?.ToString() ?? "system",
                action: "SearchPatients",
                resourceType: "Patient",
                resourceId: null,
                ipAddress: ipAddress,
                details: $"Search term: {request.SearchTerm ?? "N/A"}, Results: {patients.Count}, TotalAuthorized: {totalCount}",
                cancellationToken: cancellationToken);

            // Map to DTOs
            var patientDtos = patients.Select(MapToSearchResultDto).ToList();

            var pagedResult = PagedResultDto<PatientSearchResultDto>.Create(
                patientDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return ResultDto<PagedResultDto<PatientSearchResultDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching patients. SearchTerm: {SearchTerm}", request.SearchTerm ?? "N/A");
            return ResultDto<PagedResultDto<PatientSearchResultDto>>.Failure("An error occurred while searching patients");
        }
    }

    private static PatientSearchResultDto MapToSearchResultDto(Domain.Entities.Patient patient)
    {
        return new PatientSearchResultDto
        {
            Id = patient.Id,
            MedicalRecordNumber = patient.MedicalRecordNumber.Value,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            Age = patient.Age,
            Gender = patient.Gender,
            PhoneNumber = patient.PhoneNumber,
            Email = patient.Email,
            IsActive = patient.IsActive
        };
    }
}
