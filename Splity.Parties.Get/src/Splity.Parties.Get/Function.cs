using System.Data;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Authentication;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Common;
using Splity.Shared.Database;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Parties.Get;

public class Function(IDbConnection connection, IPartyRepository? partyRepository = null, IAuthenticationService? authService = null) : BaseAuthenticatedLambdaFunction(connection, authService)
{
    private readonly IPartyRepository _partyRepository = partyRepository ?? new PartyRepository(connection);

    public Function() : this(CreateDatabaseConnection(), null)
    {
    }

    /// <summary>
    /// Lambda function that retrieves all parties owned by the authenticated user
    /// </summary>
    /// <param name="request">The event for the Lambda function handler to process.</param>
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

        // Authenticate user
        var authResult = await AuthenticateAsync(request, context);
        if (authResult != null)
        {
            return authResult; // Authentication failed
        }

        // Get parties owned by the current user
        var parties = await _partyRepository.GetPartiesByUserId(CurrentUser!.SplityUserId!.Value);

        return CreateSuccessResponse(HttpStatusCode.OK, new { parties }, httpMethod);
    }
}
