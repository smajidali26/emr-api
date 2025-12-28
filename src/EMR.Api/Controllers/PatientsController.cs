using EMR.Application.Common.DTOs;
using EMR.Application.Common.Utilities;
using EMR.Application.Common.Validation;
using EMR.Application.Features.Patients.Commands.RegisterPatient;
using EMR.Application.Features.Patients.Commands.UpdatePatientDemographics;
using EMR.Application.Features.Patients.DTOs;
using EMR.Application.Features.Patients.Queries.GetPatientById;
using EMR.Application.Features.Patients.Queries.GetPatientByMRN;
using EMR.Application.Features.Patients.Queries.SearchPatients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EMR.Api.Controllers;

/// <summary>
/// Patient registration and demographics management controller
/// HIPAA Compliance: All endpoints log access to patient data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IMediator mediator, ILogger<PatientsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new patient in the EMR system
    /// Requires Admin, Doctor, or Nurse role
    /// </summary>
    /// <param name="command">Patient registration data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registered patient details</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor,Nurse")]
    [EnableRateLimiting("patient-registration")] // SECURITY: Rate limit to prevent registration flooding/abuse
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterPatient(
        [FromBody] RegisterPatientCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("RegisterPatient endpoint called for patient: {FirstName} {LastName}",
            LogSanitizer.SanitizePersonName(command.FirstName),
            LogSanitizer.SanitizePersonName(command.LastName));

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Patient registration failed. Error: {Error}", result.ErrorMessage);

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

        _logger.LogInformation("Patient registered successfully. MRN: {MRN}",
            result.Data!.MedicalRecordNumber);

        return CreatedAtAction(
            nameof(GetPatientById),
            new { id = result.Data.Id },
            result.Data);
    }

    /// <summary>
    /// Get patient by ID
    /// Requires authentication
    /// </summary>
    /// <param name="id">Patient ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Patient details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPatientById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetPatientById endpoint called for patient: {PatientId}", id);

        var query = new GetPatientByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get patient: {PatientId}. Error: {Error}", id, result.ErrorMessage);
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get patient by Medical Record Number (MRN)
    /// Requires authentication
    /// </summary>
    /// <param name="mrn">Medical Record Number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Patient details</returns>
    [HttpGet("mrn/{mrn}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPatientByMRN(string mrn, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetPatientByMRN endpoint called for MRN: {MRN}", mrn);

        var query = new GetPatientByMRNQuery(mrn);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get patient by MRN: {MRN}. Error: {Error}", mrn, result.ErrorMessage);
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Search patients by criteria
    /// Requires authentication
    /// </summary>
    /// <param name="searchTerm">Search term (searches in name, MRN, email)</param>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of patients</returns>
    [HttpGet("search")]
    [EnableRateLimiting("patient-search")] // SECURITY: Rate limit search to prevent data scraping
    [ProducesResponseType(typeof(PagedResultDto<PatientSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchPatients(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // SECURITY FIX: Task #2 - Add input validation for search params (Maria Rodriguez - 8h)
        // Validate and sanitize search parameters to prevent SQL injection
        if (!SearchParameterValidator.ValidateSearchParameters(
            searchTerm, pageNumber, pageSize, out var sanitizedSearchTerm, out var validationError))
        {
            _logger.LogWarning("Invalid search parameters. Error: {Error}, SearchTerm: {SearchTerm}, Page: {PageNumber}, PageSize: {PageSize}",
                validationError, searchTerm ?? "N/A", pageNumber, pageSize);

            return BadRequest(new { message = validationError });
        }

        _logger.LogInformation("SearchPatients endpoint called. SearchTerm: {SearchTerm}, Page: {PageNumber}, PageSize: {PageSize}",
            sanitizedSearchTerm ?? "N/A", pageNumber, pageSize);

        var query = new SearchPatientsQuery
        {
            SearchTerm = sanitizedSearchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Patient search failed. Error: {Error}", result.ErrorMessage);
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Update patient demographics
    /// Requires Admin, Doctor, or Nurse role
    /// </summary>
    /// <param name="id">Patient ID</param>
    /// <param name="command">Updated demographics data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated patient details</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor,Nurse")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePatientDemographics(
        Guid id,
        [FromBody] UpdatePatientDemographicsCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.PatientId)
        {
            return BadRequest(new { message = "Patient ID in URL does not match request body" });
        }

        _logger.LogInformation("UpdatePatientDemographics endpoint called for patient: {PatientId}", id);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update patient demographics: {PatientId}. Error: {Error}",
                id, result.ErrorMessage);

            if (result.Errors != null && result.Errors.Any())
            {
                return BadRequest(new
                {
                    message = result.ErrorMessage,
                    errors = result.Errors
                });
            }

            // Check if it's a not found error
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Patient demographics updated successfully. PatientId: {PatientId}", id);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all patients (paginated)
    /// Requires authentication
    /// </summary>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of all patients</returns>
    [HttpGet]
    [EnableRateLimiting("patient-search")] // SECURITY: Rate limit listing to prevent data scraping
    [ProducesResponseType(typeof(PagedResultDto<PatientSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPatients(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // SECURITY FIX: Task #2 - Add input validation for search params (Maria Rodriguez - 8h)
        // Validate pagination parameters to prevent resource exhaustion
        if (!SearchParameterValidator.ValidatePageNumber(pageNumber, out var pageNumberError))
        {
            _logger.LogWarning("Invalid page number: {PageNumber}. Error: {Error}", pageNumber, pageNumberError);
            return BadRequest(new { message = pageNumberError });
        }

        if (!SearchParameterValidator.ValidatePageSize(pageSize, out var pageSizeError))
        {
            _logger.LogWarning("Invalid page size: {PageSize}. Error: {Error}", pageSize, pageSizeError);
            return BadRequest(new { message = pageSizeError });
        }

        _logger.LogInformation("GetAllPatients endpoint called. Page: {PageNumber}, PageSize: {PageSize}",
            pageNumber, pageSize);

        // Use SearchPatients with null search term to get all patients
        var query = new SearchPatientsQuery
        {
            SearchTerm = null,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get all patients. Error: {Error}", result.ErrorMessage);
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }
}
