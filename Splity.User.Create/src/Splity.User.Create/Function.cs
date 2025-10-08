using System.Data;
using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Common;
using Splity.Shared.Database;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.User.Create;

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
    /// Lambda function handler to create a new user
    /// </summary>
    /// <param name="request">The API Gateway proxy request</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        if (request.RequestContext.Http.Method == "OPTIONS")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.OK, string.Empty, GetCorsHeaders());
        }

        if (request.RequestContext.Http.Method != "POST")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.MethodNotAllowed, string.Empty, GetCorsHeaders());
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = "Request body is required" }), GetCorsHeaders());
        }

        try
        {
            context.Logger.LogInformation($"Creating user with request body: {request.Body}");
            var createUserRequest = JsonSerializer.Deserialize<CreateUserRequest>(request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (createUserRequest == null || string.IsNullOrWhiteSpace(createUserRequest.Name) || string.IsNullOrWhiteSpace(createUserRequest.Email))
            {
                return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(new { error = "Name and Email are required" }), GetCorsHeaders());
            }

            var user = await _userRepository.CreateUserAsync(createUserRequest);

            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.Created, JsonSerializer.Serialize(user), GetCorsHeaders());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.Conflict,
                JsonSerializer.Serialize(new { error = ex.Message }), GetCorsHeaders());
        }
        catch (JsonException)
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = "Invalid JSON format" }), GetCorsHeaders());
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error creating user: {ex.Message}");
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.InternalServerError,
                JsonSerializer.Serialize(new { error = "Internal server error" }), GetCorsHeaders());
        }
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
            { "Access-Control-Allow-Methods", "POST" },
            { "Access-Control-Max-Age", "86400" }, // Cache preflight for 24 hours
            { "Content-Type", "application/json" }
        };
    }
}
