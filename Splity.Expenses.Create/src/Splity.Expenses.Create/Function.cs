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

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Expenses.Create;

public class Function(IDbConnection connection, IExpenseRepository? expenseRepository = null)
{
    private readonly IExpenseRepository _expenseRepository = expenseRepository ?? new ExpenseRepository(connection);

    public Function() : this(
        DsqlConnectionHelper.CreateConnection(
            Environment.GetEnvironmentVariable("CLUSTER_USERNAME"),
            Environment.GetEnvironmentVariable("CLUSTER_HOSTNAME"),
            RegionEndpoint.EUWest2.SystemName,
            Environment.GetEnvironmentVariable("CLUSTER_DATABASE")),
        null)
    {
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        if (request.RequestContext.Http.Method == "OPTIONS")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.OK, string.Empty, GetCorsHeaders());
        }

        if (request.RequestContext.Http.Method != "POST")
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.MethodNotAllowed, string.Empty,
                GetCorsHeaders());
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = "Request body is required" }), GetCorsHeaders());
        }

        try
        {
            context.Logger.LogInformation($"Creating expenses with request body: {request.Body}");

            var createExpensesRequest = JsonSerializer.Deserialize<CreateExpensesRequest>(request.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (createExpensesRequest == null || createExpensesRequest.PartyId == Guid.Empty ||
                createExpensesRequest.PayerId == Guid.Empty || !createExpensesRequest.Expenses.Any())
            {
                return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(new { error = "PartyId, PayerId and Expenses are required" }),
                    GetCorsHeaders());
            }

            context.Logger.LogInformation(
                $"Processing expenses creation for party {createExpensesRequest.PartyId}, payer {createExpensesRequest.PayerId} and {createExpensesRequest.Expenses.Count()} expenses");

            var createdExpensesCount = await _expenseRepository.CreateExpensesAsync(createExpensesRequest);

            context.Logger.LogInformation($"Expenses created: {createdExpensesCount}");

            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.Created, JsonSerializer.Serialize(new
            {
                expenses = createdExpensesCount
            }), GetCorsHeaders());
        }
        catch (JsonException)
        {
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = "Invalid JSON format" }), GetCorsHeaders());
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error creating expenses: {ex.Message}");
            return ApiGatewayHelper.CreateApiGatewayProxyResponse(HttpStatusCode.InternalServerError,
                JsonSerializer.Serialize(new { error = "Internal server error" }), GetCorsHeaders());
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
            { "Access-Control-Allow-Methods", "POST" },
            { "Access-Control-Max-Age", "86400" }, // Cache preflight for 24 hours
            { "Content-Type", "application/json" }
        };
    }
}