using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMR.Api.Controllers;

/// <summary>
/// Health check controller for monitoring API status
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get API health status
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        var response = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Application = _configuration["ApplicationSettings:ApplicationName"],
            Version = _configuration["ApplicationSettings:Version"],
            Environment = _configuration["ApplicationSettings:Environment"]
        };

        return Ok(response);
    }

    /// <summary>
    /// Get detailed health information
    /// </summary>
    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDetailedHealth()
    {
        var response = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Application = _configuration["ApplicationSettings:ApplicationName"],
            Version = _configuration["ApplicationSettings:Version"],
            Environment = _configuration["ApplicationSettings:Environment"],
            ServerTime = DateTime.UtcNow,
            Uptime = Environment.TickCount64,
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet
        };

        return Ok(response);
    }
}
