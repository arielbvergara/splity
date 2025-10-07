using System.Data;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
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
    IExpenseRepository? expenseRepository = null)
{
    private readonly IExpenseRepository _expenseRepository = expenseRepository ?? new ExpenseRepository(connection);

    public Function() : this(
        DsqlConnectionHelper.CreateConnection(
            Environment.GetEnvironmentVariable("CLUSTER_USERNAME"),
            Environment.GetEnvironmentVariable("CLUSTER_HOSTNAME"),
            RegionEndpoint.EUWest2.SystemName,
            Environment.GetEnvironmentVariable("CLUSTER_DATABASE")))
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
        if (request.RequestContext.Http.Method == "OPTIONS")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse2(HttpStatusCode.OK, string.Empty, GetCorsHeaders());
        }

        if (request.RequestContext.Http.Method != "DELETE")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse2(HttpStatusCode.MethodNotAllowed,
                JsonSerializer.Serialize(
                    new
                    {
                        Error = $"Invalid request method: {request.RequestContext.Http.Method}"
                    }),
                GetCorsHeaders());
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse2(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { Error = "Request body is required" }), GetCorsHeaders());
        }

        try
        {
            context.Logger.LogInformation($"Processing bulk delete with request body: {request.Body}");

            var deleteExpensesRequest = JsonSerializer.Deserialize<DeleteExpensesRequest>(request.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (deleteExpensesRequest == null || !deleteExpensesRequest.ExpenseIds.Any())
            {
                return ApiGatewayHelper.CreateApiGatewayProxyResponse2(HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(new { Error = "ExpenseIds are required and cannot be empty" }),
                    GetCorsHeaders());
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
            return ApiGatewayHelper.CreateApiGatewayProxyResponse2(statusCode, 
                JsonSerializer.Serialize(response), GetCorsHeaders());
        }
        catch (JsonException)
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse2(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { Error = "Invalid JSON format" }), GetCorsHeaders());
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting expenses: {ex.Message}");

            return ApiGatewayHelper.CreateApiGatewayProxyResponse2(HttpStatusCode.InternalServerError,
                JsonSerializer.Serialize(new
                {
                    Error = $"Error deleting expenses: {ex.Message}"
                }), GetCorsHeaders());
        }
    }

    /// <summary>
    /// Get CORS headers for cross-origin requests
    /// </summary>
    /// <returns>Dictionary of CORS headers</returns>
    private Dictionary<string, string> GetCorsHeaders()
    {
        return new Dictionary<string, string>
        {
            { "Access-Control-Allow-Origin", Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "*" },
            {
                "Access-Control-Allow-Headers",
                "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,x-filename"
            },
            { "Access-Control-Allow-Methods", "DELETE" },
            { "Access-Control-Max-Age", "86400" }, // Cache preflight for 24 hours
            { "Content-Type", "application/json" }
        };
    }
}