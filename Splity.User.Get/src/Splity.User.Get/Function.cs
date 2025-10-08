using System.Data;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Common;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.User.Get;

public class Function(IDbConnection connection, IUserRepository? userRepository = null) : BaseLambdaFunction
{
    private readonly IUserRepository _userRepository = userRepository ?? new UserRepository(connection);

    public Function() : this(CreateDatabaseConnection())
    {
    }

    /// <summary>
    /// Lambda function handler for getting a user by ID
    /// </summary>
    /// <param name="request">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, HttpMethod.Get.ToString());
        if (methodValidation != null)
        {
            return methodValidation;
        }

        if (request.QueryStringParameters == null ||
            !request.QueryStringParameters.TryGetValue("userId", out var userId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Missing userId query parameter", HttpMethod.Get.ToString());
        }

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guidUserId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing userId parameter", HttpMethod.Get.ToString());
        }

        var user = await _userRepository.GetUserByIdWithDetailsAsync(guidUserId);

        if (user == null)
        {
            return CreateErrorResponse(HttpStatusCode.NotFound, "User not found", HttpMethod.Get.ToString());
        }

        return CreateSuccessResponse(HttpStatusCode.OK, new { user }, HttpMethod.Get.ToString());
    }
}
