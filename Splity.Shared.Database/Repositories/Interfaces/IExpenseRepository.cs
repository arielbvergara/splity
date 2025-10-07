using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Models.Queries;

namespace Splity.Shared.Database.Repositories.Interfaces;

public interface IExpenseRepository
{
    Task<int> CreateExpensesAsync(CreateExpensesRequest request);
    Task<IEnumerable<Expense>> GetExpensesAsync(GetExpensesRequest request);
    Task<int> DeleteExpensesByPartyIdAsync(Guid partyId);
    Task<bool> DeleteExpenseByIdAsync(Guid expenseId);
    Task<int> DeleteExpensesByIdsAsync(IEnumerable<Guid> expenseIds);
}
