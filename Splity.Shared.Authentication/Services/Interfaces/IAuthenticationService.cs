using Amazon.Lambda.APIGatewayEvents;
using Splity.Shared.Authentication.Models;

namespace Splity.Shared.Authentication.Services.Interfaces;

public interface IAuthenticationService
{
    Task<CognitoUser?> GetUserFromRequestAsync(APIGatewayHttpApiV2ProxyRequest request);
    Task<Guid> EnsureUserExistsAsync(CognitoUser cognitoUser);
    string? ExtractTokenFromRequest(APIGatewayHttpApiV2ProxyRequest request);
}