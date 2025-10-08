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

namespace Splity.Party.Update;

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
    /// Lambda function handler to update an existing party
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

        if (request.RequestContext.Http.Method != "PUT")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.MethodNotAllowed, string.Empty, GetCorsHeaders());
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = "Request body is required" }), GetCorsHeaders());
        }

        // Extract party ID from path parameters
        if (request.PathParameters?.TryGetValue("id", out var partyIdString) != true ||
            !Guid.TryParse(partyIdString, out var partyId))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = "Valid party ID is required in path" }), GetCorsHeaders());
        }

        try
        {
            context.Logger.LogInformation($"Updating party {partyId} with request body: {request.Body}");
            var updatePartyRequest = JsonSerializer.Deserialize<UpdatePartyRequest>(request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updatePartyRequest == null)
            {
                return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(new { error = "Invalid request format" }), GetCorsHeaders());
            }

            // Validate that at least one field is provided for update
            if (string.IsNullOrWhiteSpace(updatePartyRequest.Name))
            {
                return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(new { error = "At least one field (Name) must be provided for update" }), GetCorsHeaders());
            }

            // Set the party ID from the path parameter
            updatePartyRequest.PartyId = partyId;

            var updatedParty = await _partyRepository.UpdateParty(updatePartyRequest);

            if (updatedParty == null)
            {
                return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.NotFound,
                    JsonSerializer.Serialize(new { error = "Party not found" }), GetCorsHeaders());
            }

            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.OK, JsonSerializer.Serialize(updatedParty), GetCorsHeaders());
        }
        catch (JsonException)
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = "Invalid JSON format" }), GetCorsHeaders());
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error updating party: {ex.Message}");
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
            { "Access-Control-Allow-Methods", "PUT" },
            { "Access-Control-Max-Age", "86400" }, // Cache preflight for 24 hours
            { "Content-Type", "application/json" }
        };
    }
}
