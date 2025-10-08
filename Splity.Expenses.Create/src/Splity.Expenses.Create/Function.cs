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

public class Function(IDbConnection connection, IExpenseRepository? expenseRepository = null) : BaseLambdaFunction
{
    private readonly IExpenseRepository _expenseRepository = expenseRepository ?? new ExpenseRepository(connection);

    public Function() : this(CreateDatabaseConnection(), null)
    {
    }

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
            context.Logger.LogInformation($"Creating expenses with request body: {request.Body}");

            var createExpensesRequest = JsonSerializer.Deserialize<CreateExpensesRequest>(request.Body, JsonOptions);

            if (createExpensesRequest == null || createExpensesRequest.PartyId == Guid.Empty ||
                createExpensesRequest.PayerId == Guid.Empty || !createExpensesRequest.Expenses.Any())
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "PartyId, PayerId and Expenses are required", "POST");
            }

            context.Logger.LogInformation(
                $"Processing expenses creation for party {createExpensesRequest.PartyId}, payer {createExpensesRequest.PayerId} and {createExpensesRequest.Expenses.Count()} expenses");

            var createdExpensesCount = await _expenseRepository.CreateExpensesAsync(createExpensesRequest);

            context.Logger.LogInformation($"Expenses created: {createdExpensesCount}");

            return CreateSuccessResponse(HttpStatusCode.Created, new { expenses = createdExpensesCount }, "POST");
        }
        catch (JsonException)
        {
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid JSON format", "POST");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error creating expenses: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error", "POST");
        }
    }
}
