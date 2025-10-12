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
    IExpenseRepository? expenseRepository = null) : BaseLambdaFunction
{
    private readonly IPartyRepository _partyRepository = partyRepository ?? new PartyRepository(connection);
    private readonly IExpenseRepository _expenseRepository = expenseRepository ?? new ExpenseRepository(connection);

    public Function() : this(CreateDatabaseConnection())
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
        var httpMethod = HttpMethod.Delete.ToString();

        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, httpMethod);
        if (methodValidation != null)
        {
            return methodValidation;
        }

        // Extract party ID from path parameters
        if (request.PathParameters?.TryGetValue("id", out var partyIdString) != true || !Guid.TryParse(partyIdString, out var partyId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Valid party ID is required in path", httpMethod);
        }

        try
        {
            // 1. Check if party exists
            var party = await _partyRepository.GetPartyById(partyId);
            if (party == null)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, $"Party not found {partyId}", httpMethod);
            }

            // 3. Delete the party
            await _partyRepository.DeletePartyById(partyId);

            return CreateSuccessResponse(HttpStatusCode.OK, new PartyDeleteResponse
            {
                Success = true,
                Message = $"Party {partyId} deleted successfully",
                PartyId = partyId
            }, httpMethod);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting party {partyId}: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, $"Error deleting party {partyId}: {ex.Message}", httpMethod);
        }
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