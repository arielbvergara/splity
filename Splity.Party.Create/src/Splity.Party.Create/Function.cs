using System.Data;
using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Authentication;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Database;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Party.Create;

public class Function(IDbConnection connection, IPartyRepository? partyRepository = null, IAuthenticationService? authService = null) : BaseAuthenticatedLambdaFunction(connection, authService)
{
    private readonly IPartyRepository _partyRepository = partyRepository ?? new PartyRepository(connection);

    public Function() : this(CreateDatabaseConnection(), null, null)
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
        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, "POST");
        if (methodValidation != null)
        {
            return methodValidation;
        }

        // Authenticate user
        var authResult = await AuthenticateAsync(request, context);
        if (authResult != null)
        {
            return authResult; // Authentication failed
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required", "POST");
        }

        try
        {
            context.Logger.LogInformation($"Creating party with request body: {request.Body}");
            var createPartyRequest = JsonSerializer.Deserialize<CreatePartyRequest>(request.Body, JsonOptions);

            if (createPartyRequest == null || string.IsNullOrWhiteSpace(createPartyRequest.Name))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Name is required", "POST");
            }

            // Use the authenticated user's ID as the owner
            createPartyRequest.OwnerId = CurrentUserId;
            var party = await _partyRepository.CreateParty(createPartyRequest);

            return CreateSuccessResponse(HttpStatusCode.Created, party, "POST");
        }
        catch (JsonException)
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid JSON format", "POST");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error creating party: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error", "POST");
        }
    }
}
