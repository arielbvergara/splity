namespace Splity.Shared.Database.Models;

public class Expense
{
    public required Guid ExpenseId { get; set; }
    public required Guid PartyId { get; set; }
    public required Guid PayerId { get; set; }
    public required string Description { get; set; }
    public required decimal Amount { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
}