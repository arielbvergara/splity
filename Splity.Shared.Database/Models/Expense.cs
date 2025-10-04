namespace Splity.Shared.Database.Models;

public class Expense
{
    public Guid? ExpenseId { get; set; }
    public required Guid PartyId { get; set; }
    public required Guid PayerId { get; set; }
    public required string Description { get; set; }
    public required decimal Amount { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class ExpenseCreate
{
    public string Description { get; set; }
    public decimal Amount { get; set; }
}

public class ExpensesCreationRequest
{
    public Guid PartyId { get; set; }
    public Guid PayerId { get; set; }
    public IEnumerable<ExpenseCreate> Expenses { get; set; }
}

public class ExpensesCreationResponse
{
    public List<Guid> CreatedExpenses { get; set; }
}

public class GetExpensesRequest
{
    public Guid PartyId { get; set; }
}

public class CreatePartyBillImageRequest
{
    public required Guid BillId { get; set; }
    public required Guid PartyId { get; set; }
    public required string Title { get; set; }
    public required string ImageUrl { get; set; }
}