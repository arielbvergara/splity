namespace Splity.Shared.Database.Models.Commands;

public class CreateExpensesRequest
{
    public Guid PartyId { get; set; }
    public Guid PayerId { get; set; }
    public IEnumerable<CreateExpenseRequest> Expenses { get; set; }
}