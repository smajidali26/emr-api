using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Audit.DTOs;

namespace EMR.Application.Features.Audit.Queries.GetResourceAuditTrail;

/// <summary>
/// Query to retrieve audit trail for a specific resource
/// Shows complete history of who accessed/modified the resource
/// </summary>
public sealed class GetResourceAuditTrailQuery : IQuery<ResultDto<IEnumerable<AuditLogDto>>>
{
    /// <summary>
    /// Resource type (e.g., "Patient", "Encounter")
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Resource identifier
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;
}
