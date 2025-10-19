using System.Data;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Common;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.User.Create;

// Note: inherits from BaseLambdaFunction as this functionality doesn't require authentication
public class Function(IDbConnection connection, IUserRepository? userRepository = null) : BaseLambdaFunction
{
    private readonly IUserRepository _userRepository = userRepository ?? new UserRepository(connection);

    public Function() : this(CreateDatabaseConnection())
    {
    }

    /// <summary>
    /// Lambda function handler to create a new user
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

        if (string.IsNullOrEmpty(request.Body))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required", "POST");
        }

        try
        {
            context.Logger.LogInformation($"Creating user with request body: {request.Body}");
            var createUserRequest = JsonSerializer.Deserialize<CreateUserRequest>(request.Body, JsonOptions);

            if (createUserRequest == null || string.IsNullOrWhiteSpace(createUserRequest.Name) || string.IsNullOrWhiteSpace(createUserRequest.Email))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Name and Email are required", "POST");
            }

            var user = await _userRepository.CreateUserAsync(createUserRequest);

            return CreateSuccessResponse(HttpStatusCode.Created, user, "POST");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateErrorResponse(HttpStatusCode.Conflict, ex.Message, "POST");
        }
        catch (JsonException)
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid JSON format", "POST");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error creating user: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error", "POST");
        }
    }
}
