using System.Data;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Common;
using Splity.Shared.Database;
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
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, "PUT");
        if (methodValidation != null)
        {
            return methodValidation;
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required", "PUT");
        }

        try
        {
            context.Logger.LogInformation($"Updating user with request body: {request.Body}");
            var updateUserRequest = JsonSerializer.Deserialize<UpdateUserRequest>(request.Body, JsonOptions);

            if (updateUserRequest == null || updateUserRequest.UserId == Guid.Empty)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "UserId is required", "PUT");
            }

            // Validate that at least one field is provided for update
            if (string.IsNullOrWhiteSpace(updateUserRequest.Name) && string.IsNullOrWhiteSpace(updateUserRequest.Email))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "At least one field (Name or Email) must be provided for update", "PUT");
            }

            var updatedUser = await _userRepository.UpdateUserAsync(updateUserRequest);

            if (updatedUser == null)
            {
                return CreateErrorResponse(HttpStatusCode.NotFound, $"User with ID '{updateUserRequest.UserId}' not found", "PUT");
            }

            return CreateSuccessResponse(HttpStatusCode.OK, updatedUser, "PUT");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateErrorResponse(HttpStatusCode.Conflict, ex.Message, "PUT");
        }
        catch (JsonException)
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid JSON format", "PUT");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error updating user: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error", "PUT");
        }
    }
}
