using System.Data;
using System.Globalization;
using Npgsql;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Models.Queries;
using Splity.Shared.Database.Repositories.Interfaces;

namespace Splity.Shared.Database.Repositories;

public class ExpenseRepository(IDbConnection connection) : IExpenseRepository
{
    public async Task<int> CreateExpensesAsync(CreateExpensesRequest request)
    {
        var sql = request.Expenses.Select(expense =>
            $"INSERT INTO Expenses(ExpenseId, PartyId, PayerId, Description, Amount, CreatedAt) VALUES('{Guid.NewGuid()}', '{request.PartyId}', '{request.PayerId}', '{expense.Description}', {expense.Amount}, '{DateTime.Now.ToString(CultureInfo.InvariantCulture)}')");

        await using var insert =
            new NpgsqlCommand(sql.Aggregate((a, b) => a + "; " + b), (NpgsqlConnection)connection);
        return insert.ExecuteNonQuery();
    }

    public async Task<IEnumerable<Expense>> GetExpensesAsync(GetExpensesRequest request)
    {
        await using var select =
            new NpgsqlCommand(
                "SELECT e.*, pbi.imageUrl FROM Expenses e LEFT JOIN PartyBillsImages pbi ON e.PartyId = pbi.PartyId where e.partyId=@partyId",
                (NpgsqlConnection)connection);
        select.Parameters.AddWithValue("partyId", request.PartyId);
        await using var reader = await select.ExecuteReaderAsync();
        var expenses = new List<Expense>();

        while (await reader.ReadAsync())
        {
            expenses.Add(new Expense
            {
                ExpenseId = reader.GetGuid(0),
                PartyId = reader.GetGuid(1),
                PayerId = reader.GetGuid(2),
                Description = reader.GetString(3),
                Amount = reader.GetInt32(4),
                CreatedAt = reader.GetDateTime(5)
            });
        }

        return expenses;
    }

    public async Task<int> DeleteExpensesByPartyIdAsync(Guid partyId)
    {
        const string sql = "DELETE FROM Expenses WHERE PartyId = @partyId";

        await using var delete = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        delete.Parameters.AddWithValue("partyId", partyId);

        return await delete.ExecuteNonQueryAsync();
    }
}