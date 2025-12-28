using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Audit.DTOs;

namespace EMR.Application.Features.Audit.Queries.GetResourceAuditTrail;

/// <summary>
/// Handler for retrieving audit trail for a specific resource
/// </summary>
public sealed class GetResourceAuditTrailQueryHandler
    : IQueryHandler<GetResourceAuditTrailQuery, ResultDto<IEnumerable<AuditLogDto>>>
{
    private readonly IAuditService _auditService;

    public GetResourceAuditTrailQueryHandler(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<ResultDto<IEnumerable<AuditLogDto>>> Handle(
        GetResourceAuditTrailQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResourceType))
        {
            return ResultDto<IEnumerable<AuditLogDto>>.Failure("Resource type is required");
        }

        if (string.IsNullOrWhiteSpace(request.ResourceId))
        {
            return ResultDto<IEnumerable<AuditLogDto>>.Failure("Resource ID is required");
        }

        var auditTrail = await _auditService.GetResourceAuditTrailAsync(
            request.ResourceType,
            request.ResourceId,
            cancellationToken);

        return ResultDto<IEnumerable<AuditLogDto>>.Success(auditTrail);
    }
}
