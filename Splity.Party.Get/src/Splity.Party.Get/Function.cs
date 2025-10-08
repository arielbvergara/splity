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

namespace Splity.Party.Get;

public class Function(IDbConnection connection, IPartyRepository? partyRepository = null) : BaseLambdaFunction
{
    private readonly IPartyRepository _partyRepository = partyRepository ?? new PartyRepository(connection);

    public Function() : this(CreateDatabaseConnection(), null)
    {
    }

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, "GET");
        if (methodValidation != null)
        {
            return methodValidation;
        }

        if (request.QueryStringParameters == null ||
            !request.QueryStringParameters.TryGetValue("partyId", out var partyId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Missing partyId query parameter", "GET");
        }

        if (string.IsNullOrEmpty(partyId) || !Guid.TryParse(partyId, out var guidPartyId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing partyId parameter", "GET");
        }

        var party = await _partyRepository.GetPartyById(guidPartyId);

        return CreateSuccessResponse(HttpStatusCode.OK, new { party }, "GET");
    }
}
