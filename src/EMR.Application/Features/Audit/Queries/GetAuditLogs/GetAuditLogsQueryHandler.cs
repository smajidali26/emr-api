using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Audit.DTOs;

namespace EMR.Application.Features.Audit.Queries.GetAuditLogs;

/// <summary>
/// Handler for retrieving audit logs with filtering and pagination
/// </summary>
public sealed class GetAuditLogsQueryHandler : IQueryHandler<GetAuditLogsQuery, PagedResultDto<AuditLogDto>>
{
    private readonly IAuditService _auditService;

    public GetAuditLogsQueryHandler(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<PagedResultDto<AuditLogDto>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        // Validate and sanitize query parameters
        request.QueryParams.Validate();

        // Query audit logs
        var (logs, totalCount) = await _auditService.QueryAuditLogsAsync(
            request.QueryParams,
            cancellationToken);

        return new PagedResultDto<AuditLogDto>
        {
            Items = logs.ToList(),
            TotalCount = totalCount,
            PageNumber = request.QueryParams.PageNumber,
            PageSize = request.QueryParams.PageSize
        };
    }
}
