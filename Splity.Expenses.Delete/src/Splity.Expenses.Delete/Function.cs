using System.Data;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Splity.Shared.Authentication;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Common;
using Splity.Shared.Database;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Expenses.Delete;

public class Function(
    IDbConnection connection,
    IExpenseRepository? expenseRepository = null, IAuthenticationService? authService = null) : BaseAuthenticatedLambdaFunction(connection, authService)
{
    private readonly IExpenseRepository _expenseRepository = expenseRepository ?? new ExpenseRepository(connection);

    public Function() : this(CreateDatabaseConnection())
    {
    }

    /// <summary>
    /// Lambda function to delete expenses by list of Ids
    /// </summary>
    /// <param name="request">The API Gateway request containing the expense ID</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>Deletion result with status and message</returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        // Validate HTTP method
        var methodValidation = ValidateHttpMethod(request, "DELETE");
        if (methodValidation != null)
        {
            return methodValidation;
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required", "DELETE");
        }

        try
        {
            context.Logger.LogInformation($"Processing bulk delete with request body: {request.Body}");

            var deleteExpensesRequest = JsonSerializer.Deserialize<DeleteExpensesRequest>(request.Body, JsonOptions);

            if (deleteExpensesRequest == null || !deleteExpensesRequest.ExpenseIds.Any())
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "ExpenseIds are required and cannot be empty", "DELETE");
            }

            var expenseIdsList = deleteExpensesRequest.ExpenseIds.ToList();
            context.Logger.LogInformation($"Attempting to delete {expenseIdsList.Count} expenses");

            // Delete the expenses
            var deletedCount = await _expenseRepository.DeleteExpensesByIdsAsync(expenseIdsList);

            var response = new DeleteExpensesResponse
            {
                Success = deletedCount > 0,
                DeletedCount = deletedCount,
                RequestedCount = expenseIdsList.Count,
                DeletedExpenseIds = expenseIdsList,
                Message = deletedCount > 0 
                    ? $"Successfully deleted {deletedCount} out of {expenseIdsList.Count} expenses" 
                    : "No expenses were deleted"
            };

            context.Logger.LogInformation($"Successfully deleted {deletedCount} out of {expenseIdsList.Count} expenses");

            var statusCode = deletedCount > 0 ? HttpStatusCode.OK : HttpStatusCode.NotFound;
            return CreateSuccessResponse(statusCode, response, "DELETE");
        }
        catch (JsonException)
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid JSON format", "DELETE");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting expenses: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, $"Error deleting expenses: {ex.Message}", "DELETE");
        }
    }
}
