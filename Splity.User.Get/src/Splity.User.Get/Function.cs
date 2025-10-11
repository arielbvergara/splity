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
        var httpMethod = HttpMethod.Get.ToString();

        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, httpMethod);
        if (methodValidation != null)
        {
            return methodValidation;
        }

        if (request.PathParameters?.TryGetValue("id", out var userIdString) != true || !Guid.TryParse(userIdString, out var userId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Valid user ID is required in path", httpMethod);
        }

        var user = await _userRepository.GetUserByIdWithDetailsAsync(userId);

        if (user == null)
        {
            return CreateErrorResponse(HttpStatusCode.NotFound, "User not found", httpMethod);
        }

        return CreateSuccessResponse(HttpStatusCode.OK, new { user }, httpMethod);
    }
}
