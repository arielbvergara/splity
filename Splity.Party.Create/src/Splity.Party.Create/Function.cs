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

namespace Splity.Party.Create;

public class Function(IDbConnection connection, IPartyRepository? partyRepository = null)
{
    private readonly IPartyRepository _partyRepository = partyRepository ?? new PartyRepository(connection);

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
    /// Lambda function handler to create a new party
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
            context.Logger.LogInformation($"Creating party with request body: {request.Body}");
            var createPartyRequest = JsonSerializer.Deserialize<CreatePartyRequest>(request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (createPartyRequest == null || createPartyRequest.OwnerId == Guid.Empty || string.IsNullOrWhiteSpace(createPartyRequest.Name))
            {
                return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(new { error = "OwnerId and Name are required" }), GetCorsHeaders());
            }

            var party = await _partyRepository.CreateParty(createPartyRequest);

            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.Created, JsonSerializer.Serialize(party), GetCorsHeaders());
        }
        catch (JsonException)
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = "Invalid JSON format" }), GetCorsHeaders());
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error creating party: {ex.Message}");
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