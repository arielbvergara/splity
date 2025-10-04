using Splity.Shared.Database.Models;

namespace Splity.Shared.Database.Repositories;

public interface IExpenseRepository
{
    Task<int> CreateExpensesAsync(ExpensesCreationRequest request);
    Task<IEnumerable<Expense>> GetExpensesAsync(GetExpensesRequest request);
}