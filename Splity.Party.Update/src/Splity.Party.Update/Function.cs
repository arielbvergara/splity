using System.Data;
using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Authentication;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Common;
using Splity.Shared.Database;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Party.Update;

public class Function(IDbConnection connection, IPartyRepository? partyRepository = null, IAuthenticationService? authService = null) : BaseAuthenticatedLambdaFunction(connection, authService)
{
    private readonly IPartyRepository _partyRepository = partyRepository ?? new PartyRepository(connection);

    public Function() : this(CreateDatabaseConnection())
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
        var httpMethod = HttpMethod.Put.ToString();

        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, httpMethod);
        if (methodValidation != null)
        {
            return methodValidation;
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required", httpMethod);
        }

        // Extract party ID from path parameters
        if (request.PathParameters?.TryGetValue("id", out var partyIdString) != true || !Guid.TryParse(partyIdString, out var partyId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Valid party ID is required in path", httpMethod);
        }

        try
        {
            context.Logger.LogInformation($"Updating party {partyId} with request body: {request.Body}");
            var updatePartyRequest = JsonSerializer.Deserialize<UpdatePartyRequest>(request.Body, JsonOptions);

            if (updatePartyRequest == null)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request format", httpMethod);
            }

            // Validate that at least one field is provided for update
            if (string.IsNullOrWhiteSpace(updatePartyRequest.Name))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "At least one field (Name) must be provided for update", httpMethod);
            }

            // Set the party ID from the path parameter
            updatePartyRequest.PartyId = partyId;

            var updatedParty = await _partyRepository.UpdateParty(updatePartyRequest);

            if (updatedParty == null)
            {
                return CreateErrorResponse(HttpStatusCode.NotFound, "Party not found", httpMethod);
            }

            return CreateSuccessResponse(HttpStatusCode.OK, updatedParty, httpMethod);
        }
        catch (JsonException)
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid JSON format", httpMethod);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error updating party: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error", httpMethod);
        }
    }
}
