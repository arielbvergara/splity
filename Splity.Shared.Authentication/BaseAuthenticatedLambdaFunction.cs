using System.Data;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Authentication.Models;
using Splity.Shared.Authentication.Services;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Common;
using Splity.Shared.Database;
using Splity.Shared.Database.Repositories;

namespace Splity.Shared.Authentication;

public abstract class BaseAuthenticatedLambdaFunction : BaseLambdaFunction
{
    private readonly IAuthenticationService _authService;
    protected CognitoUser? CurrentUser { get; private set; }
    protected Guid CurrentUserId { get; private set; }

    protected BaseAuthenticatedLambdaFunction(IDbConnection connection, IAuthenticationService? authService = null)
    {
        var httpClient = new HttpClient();
        var userRepository = new UserRepository(connection);
        
        // Use provided auth service or create default one
        if (authService != null)
        {
            _authService = authService;
        }
        else
        {
            var userPoolId = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID") ?? throw new InvalidOperationException("COGNITO_USER_POOL_ID environment variable is required");
            var clientId = Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID") ?? throw new InvalidOperationException("COGNITO_CLIENT_ID environment variable is required");
            var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2";
            
            var tokenValidator = new JwtTokenValidator(httpClient, userPoolId, clientId, region);
            _authService = new AuthenticationService(tokenValidator, userRepository);
        }
    }

    protected async Task<APIGatewayHttpApiV2ProxyResponse?> AuthenticateAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        try
        {
            CurrentUser = await _authService.GetUserFromRequestAsync(request);
            
            if (CurrentUser == null || !CurrentUser.IsAuthenticated)
            {
                return CreateErrorResponse(HttpStatusCode.Unauthorized, "Authentication required", request.RequestContext.Http.Method);
            }

            // Ensure user exists in our database and get the UserId
            CurrentUserId = await _authService.EnsureUserExistsAsync(CurrentUser);
            CurrentUser.SplityUserId = CurrentUserId;

            context.Logger.LogInformation($"Authenticated user: {CurrentUser.Email} (ID: {CurrentUserId})");
            return null; // Success - no response needed
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Authentication error: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.Unauthorized, "Authentication failed", request.RequestContext.Http.Method);
        }
    }

    protected APIGatewayHttpApiV2ProxyResponse CreateUnauthorizedResponse(string method)
    {
        return CreateErrorResponse(HttpStatusCode.Unauthorized, "Authentication required", method);
    }
}