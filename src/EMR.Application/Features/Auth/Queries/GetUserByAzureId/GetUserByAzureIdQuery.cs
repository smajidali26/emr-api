using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Auth.DTOs;

namespace EMR.Application.Features.Auth.Queries.GetUserByAzureId;

/// <summary>
/// Query to get user by Azure AD B2C identifier
/// </summary>
public record GetUserByAzureIdQuery : IQuery<ResultDto<UserDto>>
{
    /// <summary>
    /// Azure AD B2C unique identifier
    /// </summary>
    public string AzureAdB2CId { get; init; }

    public GetUserByAzureIdQuery(string azureAdB2CId)
    {
        AzureAdB2CId = azureAdB2CId;
    }
}
