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
        var httpMethod = HttpMethod.Get.ToString();

        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, httpMethod);
        if (methodValidation != null)
        {
            return methodValidation;
        }

        // Extract user ID from path parameters
        if (request.PathParameters?.TryGetValue("id", out var partyIdString) != true || !Guid.TryParse(partyIdString, out var partyId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Valid party ID is required in path", httpMethod);
        }

        var party = await _partyRepository.GetPartyById(partyId);

        return CreateSuccessResponse(HttpStatusCode.OK, new { party }, httpMethod);
    }
}
