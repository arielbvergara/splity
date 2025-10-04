using System.Data;
using Amazon.Lambda.Core;
using Amazon;
using Splity.Shared.Database;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Models.Queries;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Expenses.Create;

public class Function
{
    private readonly IExpenseRepository _expenseRepository;

    public Function() : this(
        DsqlConnectionHelper.CreateConnection(
            Environment.GetEnvironmentVariable("CLUSTER_USERNAME"),
            Environment.GetEnvironmentVariable("CLUSTER_HOSTNAME"),
            RegionEndpoint.EUWest2.SystemName,
            Environment.GetEnvironmentVariable("CLUSTER_DATABASE")),
        null)
    {
    }

    public Function(IDbConnection connection, IExpenseRepository? expenseRepository = null)
    {
        _expenseRepository = expenseRepository ?? new ExpenseRepository(connection);
    }

    public async Task FunctionHandler(CreateExpensesRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation(
            $"Processing expenses creation for party {request.PartyId}, payer {request.PayerId} and {request.Expenses.Count()} expenses");

        var createdExpenses = await _expenseRepository.CreateExpensesAsync(request);

        context.Logger.LogInformation($"Expenses created: {createdExpenses}");
    }
}