using System.Data;
using Amazon;
using Amazon.Lambda.Core;
using Splity.Shared.Database;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Queries;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Expenses.Get;

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

    public async Task<IEnumerable<Expense>> FunctionHandler(GetExpensesRequest request, ILambdaContext context)
    {
        return await _expenseRepository.GetExpensesAsync(request);
    }
}