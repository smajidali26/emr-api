using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Audit.DTOs;

namespace EMR.Application.Features.Audit.Queries.GetAuditLogs;

/// <summary>
/// Query to retrieve audit logs with filtering and pagination
/// Admin-only access for HIPAA compliance review
/// </summary>
public sealed class GetAuditLogsQuery : IQuery<PagedResultDto<AuditLogDto>>
{
    /// <summary>
    /// Query parameters for filtering
    /// </summary>
    public AuditLogQueryDto QueryParams { get; set; } = new();
}
