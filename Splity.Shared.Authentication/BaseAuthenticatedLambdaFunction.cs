using System.Data;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Authentication.Models;
using Splity.Shared.Authentication.Services;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Common;
using Splity.Shared.Database.Repositories;

namespace Splity.Shared.Authentication;

public abstract class BaseAuthenticatedLambdaFunction : BaseLambdaFunction
{
    private readonly IAuthenticationService _authService;
    protected CognitoUser? CurrentUser { get; private set; }

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
            // Try to get values from configuration service first, fallback to environment variables
            var userPoolId = GetConfigurationValue("COGNITO_USER_POOL_ID") ?? throw new InvalidOperationException("COGNITO_USER_POOL_ID is required");
            var clientId = GetConfigurationValue("COGNITO_CLIENT_ID") ?? throw new InvalidOperationException("COGNITO_CLIENT_ID is required");
            var region = GetConfigurationValue("AWS_REGION") ?? "eu-west-2";
            
            var tokenValidator = new JwtTokenValidator(httpClient, userPoolId, clientId, region);
            _authService = new AuthenticationService(tokenValidator, userRepository);
        }
    }

    /// <summary>
    /// Get configuration value from Parameter Store or environment variables
    /// </summary>
    /// <param name="environmentKey">Environment variable key</param>
    /// <returns>Configuration value</returns>
    private string? GetConfigurationValue(string environmentKey)
    {
        try
        {
            // Try to initialize configuration and get value
            Configuration.InitializeAsync().Wait();
            return environmentKey switch
            {
                "COGNITO_USER_POOL_ID" => Configuration.Authentication.CognitoUserPoolId,
                "COGNITO_CLIENT_ID" => Configuration.Authentication.CognitoClientId,
                "AWS_REGION" => Configuration.Aws.Region,
                _ => Environment.GetEnvironmentVariable(environmentKey)
            };
        }
        catch
        {
            // Fallback to environment variable if Parameter Store fails
            return Environment.GetEnvironmentVariable(environmentKey);
        }
    }

    protected async Task<APIGatewayHttpApiV2ProxyResponse?> AuthenticateAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        try
        {
            CurrentUser = await _authService.GetUserFromRequestAsync(request);
            
            if (CurrentUser is not { IsAuthenticated: true })
            {
                return CreateUnauthorizedResponse(request.RequestContext.Http.Method);
            }

            // Ensure user exists in our database and get the UserId
            CurrentUser.SplityUserId = await _authService.EnsureUserExistsAsync(CurrentUser);

            context.Logger.LogInformation($"Authenticated user: {CurrentUser.Email} (ID: {CurrentUser.SplityUserId})");
            return null; // Success - no response needed
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Authentication error: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.Unauthorized, "Authentication failed", request.RequestContext.Http.Method);
        }
    }

    private APIGatewayHttpApiV2ProxyResponse CreateUnauthorizedResponse(string method)
    {
        return CreateErrorResponse(HttpStatusCode.Unauthorized, "Authentication required", method);
    }
}