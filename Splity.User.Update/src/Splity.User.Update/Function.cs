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

namespace Splity.User.Update;

public class Function(IDbConnection connection, IUserRepository? userRepository = null) : BaseLambdaFunction
{
    private readonly IUserRepository _userRepository = userRepository ?? new UserRepository(connection);

    public Function() : this(CreateDatabaseConnection(), null)
    {
    }

    /// <summary>
    /// Lambda function handler to update an existing user
    /// </summary>
    /// <param name="request">The API Gateway proxy request</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
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

        // Extract user ID from path parameters
        if (request.PathParameters?.TryGetValue("id", out var userIdString) != true || !Guid.TryParse(userIdString, out var userId))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Valid user ID is required in path", httpMethod);
        }

        try
        {
            context.Logger.LogInformation($"Updating user {userId} with request body: {request.Body}");
            var updateUserRequest = JsonSerializer.Deserialize<UpdateUserRequest>(request.Body, JsonOptions);

            if (updateUserRequest == null)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request format", httpMethod);
            }

            // Set the user ID from the path parameter
            updateUserRequest.UserId = userId;

            // Validate that at least one field is provided for update
            if (string.IsNullOrWhiteSpace(updateUserRequest.Name) && string.IsNullOrWhiteSpace(updateUserRequest.Email))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest,
                    "At least one field (Name or Email) must be provided for update", httpMethod);
            }

            var updatedUser = await _userRepository.UpdateUserAsync(updateUserRequest);

            if (updatedUser == null)
            {
                return CreateErrorResponse(HttpStatusCode.NotFound,
                    $"User with ID '{updateUserRequest.UserId}' not found", httpMethod);
            }

            return CreateSuccessResponse(HttpStatusCode.OK, updatedUser, httpMethod);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateErrorResponse(HttpStatusCode.Conflict, ex.Message, httpMethod);
        }
        catch (JsonException)
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid JSON format", httpMethod);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error updating user: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error", httpMethod);
        }
    }
}