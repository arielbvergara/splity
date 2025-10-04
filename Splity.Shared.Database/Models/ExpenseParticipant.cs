namespace Splity.Shared.Database.Models;

public class ExpenseParticipant
{
    public required Guid ExpenseId { get; set; }
    public required Guid UserId { get; set; }
    public decimal? Share { get; set; }

    // Navigation properties
    public Expense? Expense { get; set; }
    public User? User { get; set; }
}