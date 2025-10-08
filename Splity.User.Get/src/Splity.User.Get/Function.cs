using System.Data;
using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Common;
using Splity.Shared.Database;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.User.Get;

public class Function(IDbConnection connection, IUserRepository? userRepository = null)
{
    private readonly IUserRepository _userRepository = userRepository ?? new UserRepository(connection);

    public Function() : this(
        DsqlConnectionHelper.CreateConnection(
            Environment.GetEnvironmentVariable("CLUSTER_USERNAME"),
            Environment.GetEnvironmentVariable("CLUSTER_HOSTNAME"),
            RegionEndpoint.EUWest2.SystemName,
            Environment.GetEnvironmentVariable("CLUSTER_DATABASE")),
        null)
    {
    }

    /// <summary>
    /// Lambda function handler for getting a user by ID
    /// </summary>
    /// <param name="request">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        if (request.RequestContext.Http.Method == "OPTIONS")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.OK, string.Empty, GetCorsHeaders());
        }

        if (request.RequestContext.Http.Method != "GET")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.MethodNotAllowed,
                JsonSerializer.Serialize(
                    new
                    {
                        Error = $"Invalid request method: {request.RequestContext.Http.Method}"
                    }),
                GetCorsHeaders());
        }

        if (request.QueryStringParameters == null ||
            !request.QueryStringParameters.TryGetValue("userId", out var userId))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest, JsonSerializer.Serialize(
                new
                {
                    Error = "Missing userId query parameter"
                }), GetCorsHeaders());
        }

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guidUserId))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest, JsonSerializer.Serialize(
                new
                {
                    Error = "Invalid or missing userId parameter"
                }), GetCorsHeaders());
        }

        var user = await _userRepository.GetUserById(guidUserId);

        if (user == null)
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.NotFound, JsonSerializer.Serialize(
                new
                {
                    Error = "User not found"
                }), GetCorsHeaders());
        }

        return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            user
        }), GetCorsHeaders());
    }

    /// <summary>
    /// Get CORS headers for cross-origin requests
    /// </summary>
    /// <returns>Dictionary of CORS headers</returns>
    private Dictionary<string, string> GetCorsHeaders()
    {
        return new Dictionary<string, string>
        {
            { "Access-Control-Allow-Origin", Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "*" },
            {
                "Access-Control-Allow-Headers",
                "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,x-filename"
            },
            { "Access-Control-Allow-Methods", "GET" },
            { "Access-Control-Max-Age", "86400" }, // Cache preflight for 24 hours
            { "Content-Type", "application/json" }
        };
    }
}
