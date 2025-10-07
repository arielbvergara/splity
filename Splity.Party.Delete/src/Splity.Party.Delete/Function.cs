using Amazon.Lambda.Core;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Splity.Shared.Common;
using Splity.Shared.Database;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Party.Delete;

public class Function(
    IDbConnection connection,
    IPartyRepository? partyRepository = null,
    IExpenseRepository? expenseRepository = null)
{
    private readonly IPartyRepository _partyRepository = partyRepository ?? new PartyRepository(connection);
    private readonly IExpenseRepository _expenseRepository = expenseRepository ?? new ExpenseRepository(connection);

    public Function() : this(
        DsqlConnectionHelper.CreateConnection(
            Environment.GetEnvironmentVariable("CLUSTER_USERNAME"),
            Environment.GetEnvironmentVariable("CLUSTER_HOSTNAME"),
            RegionEndpoint.EUWest2.SystemName,
            Environment.GetEnvironmentVariable("CLUSTER_DATABASE")))
    {
    }

    /// <summary>
    /// Lambda function to delete a party and its associated data
    /// </summary>
    /// <param name="request">The party deletion request containing PartyId</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>Deletion result with status and message</returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        if (request.RequestContext.Http.Method == "OPTIONS")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.OK, string.Empty, GetCorsHeaders());
        }

        if (request.RequestContext.Http.Method != "DELETE")
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
            !request.QueryStringParameters.TryGetValue("partyId", out var partyId))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest, JsonSerializer.Serialize(
                new
                {
                    Error = "Missing partyId query parameter"
                }));
        }

        if (string.IsNullOrEmpty(partyId) || !Guid.TryParse(partyId, out var guidPartyId))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest, JsonSerializer.Serialize(
                new
                {
                    Error = "Invalid or missing partyId parameter"
                }), GetCorsHeaders());
        }

        try
        {
            // 1. Check if party exists
            var party = await _partyRepository.GetPartyById(guidPartyId);
            if (party == null)
            {
                return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(
                        new
                        {
                            Error = $"Party not found {guidPartyId}"
                        }), GetCorsHeaders());
            }

            // 3. Delete the party
            await _partyRepository.DeletePartyById(guidPartyId);

            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
                PartyDeleteResponse
                {
                    Success = true,
                    Message = $"Party {guidPartyId} deleted successfully",
                    PartyId = guidPartyId
                }), GetCorsHeaders());
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting party {partyId}: {ex.Message}");

            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.InternalServerError,
                JsonSerializer.Serialize(new
                {
                    Error = $"Error deleting party {partyId}: {ex.Message}"
                }), GetCorsHeaders());
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

public class PartyDeleteRequest
{
    [Required] public Guid PartyId { get; set; }

    // Optional: Add user context for authorization
    public Guid? UserId { get; set; }
}

public class PartyDeleteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Guid? PartyId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}