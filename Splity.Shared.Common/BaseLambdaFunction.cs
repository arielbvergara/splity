using System.Data;
using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Splity.Shared.Common.Configuration;
using Splity.Shared.Database;

namespace Splity.Shared.Common;

/// <summary>
/// Base class for Lambda functions providing common functionality like CORS headers, 
/// database connection setup, and standardized error responses
/// </summary>
public abstract class BaseLambdaFunction
{
    /// <summary>
    /// Standard JSON serialization options used across all Lambda functions
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Configuration service instance (lazy-loaded)
    /// </summary>
    private static readonly Lazy<ISplityConfigurationService> ConfigurationService = new(() => new SplityConfigurationService());

    /// <summary>
    /// Get the configuration service instance
    /// </summary>
    protected static ISplityConfigurationService Configuration => ConfigurationService.Value;

    /// <summary>
    /// Creates a database connection using configuration service
    /// </summary>
    /// <returns>Database connection</returns>
    protected static async Task<IDbConnection> CreateDatabaseConnectionAsync()
    {
        await Configuration.InitializeAsync();
        return DsqlConnectionHelper.CreateConnection(
            Configuration.Database.Username,
            Configuration.Database.Hostname,
            RegionEndpoint.EUWest2.SystemName,
            Configuration.Database.Database);
    }

    /// <summary>
    /// Creates a database connection using environment variables (for backward compatibility)
    /// </summary>
    /// <returns>Database connection</returns>
    protected static IDbConnection CreateDatabaseConnection()
    {
        return DsqlConnectionHelper.CreateConnection(
            Environment.GetEnvironmentVariable("CLUSTER_USERNAME"),
            Environment.GetEnvironmentVariable("CLUSTER_HOSTNAME"),
            RegionEndpoint.EUWest2.SystemName,
            Environment.GetEnvironmentVariable("CLUSTER_DATABASE"));
    }

    /// <summary>
    /// Handles OPTIONS requests for CORS preflight
    /// </summary>
    /// <param name="allowedMethods">Comma-separated list of allowed HTTP methods</param>
    /// <returns>CORS preflight response</returns>
    protected APIGatewayHttpApiV2ProxyResponse HandleOptionsRequest(string allowedMethods = "GET,POST,PUT,DELETE")
    {
        return ApiGatewayHelper.CreateApiGatewayProxyResponse(
            HttpStatusCode.OK,
            string.Empty,
            GetCorsHeaders(allowedMethods));
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="allowedMethods">Allowed HTTP methods for CORS</param>
    /// <returns>Standardized error response</returns>
    protected APIGatewayHttpApiV2ProxyResponse CreateErrorResponse(
        HttpStatusCode statusCode,
        string errorMessage,
        string allowedMethods = "GET,POST,PUT,DELETE")
    {
        return ApiGatewayHelper.CreateApiGatewayProxyResponse(
            statusCode,
            JsonSerializer.Serialize(LambdaFunctionResponseHelper.CreateErrorResponse(errorMessage), JsonOptions),
            GetCorsHeaders(allowedMethods));
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="data">Response data</param>
    /// <param name="allowedMethods">Allowed HTTP methods for CORS</param>
    /// <returns>Standardized success response</returns>
    protected APIGatewayHttpApiV2ProxyResponse CreateSuccessResponse(
        HttpStatusCode statusCode,
        object data,
        string allowedMethods = "GET,POST,PUT,DELETE")
    {
        return ApiGatewayHelper.CreateApiGatewayProxyResponse(
            statusCode,
            JsonSerializer.Serialize(LambdaFunctionResponseHelper.CreateSuccessResponse(data), JsonOptions),
            GetCorsHeaders(allowedMethods));
    }

    /// <summary>
    /// Validates that the HTTP method is allowed
    /// </summary>
    /// <param name="request">API Gateway request</param>
    /// <param name="allowedMethods">Array of allowed HTTP methods</param>
    /// <returns>Validation response or null if valid</returns>
    protected APIGatewayHttpApiV2ProxyResponse? ValidateHttpMethod(
        APIGatewayHttpApiV2ProxyRequest request,
        params string[] allowedMethods)
    {
        var method = request.RequestContext.Http.Method;
        
        if (method == "OPTIONS")
        {
            return HandleOptionsRequest(string.Join(",", allowedMethods));
        }

        if (!allowedMethods.Contains(method))
        {
            return CreateErrorResponse(
                HttpStatusCode.MethodNotAllowed,
                $"Invalid request method: {method}",
                string.Join(",", allowedMethods));
        }

        return null;
    }

    /// <summary>
    /// Get CORS headers for cross-origin requests (async version using configuration service)
    /// </summary>
    /// <param name="allowedMethods">Comma-separated list of allowed HTTP methods</param>
    /// <returns>Dictionary of CORS headers</returns>
    protected async Task<Dictionary<string, string>> GetCorsHeadersAsync(string allowedMethods = "GET,POST,PUT,DELETE")
    {
        await Configuration.InitializeAsync();
        return new Dictionary<string, string>
        {
            { "Access-Control-Allow-Origin", Configuration.Application.AllowedOrigins },
            {
                "Access-Control-Allow-Headers",
                "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,x-filename"
            },
            { "Access-Control-Allow-Methods", allowedMethods },
            { "Access-Control-Max-Age", "86400" }, // Cache preflight for 24 hours
            { "Content-Type", "application/json" }
        };
    }

    /// <summary>
    /// Get CORS headers for cross-origin requests (fallback using environment variables)
    /// </summary>
    /// <param name="allowedMethods">Comma-separated list of allowed HTTP methods</param>
    /// <returns>Dictionary of CORS headers</returns>
    protected Dictionary<string, string> GetCorsHeaders(string allowedMethods = "GET,POST,PUT,DELETE")
    {
        return new Dictionary<string, string>
        {
            { "Access-Control-Allow-Origin", Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "*" },
            {
                "Access-Control-Allow-Headers",
                "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,x-filename"
            },
            { "Access-Control-Allow-Methods", allowedMethods },
            { "Access-Control-Max-Age", "86400" }, // Cache preflight for 24 hours
            { "Content-Type", "application/json" }
        };
    }
}