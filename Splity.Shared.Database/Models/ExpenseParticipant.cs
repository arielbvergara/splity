namespace Splity.Shared.Database.Models;

public class ExpenseParticipant
{
    public required Guid ExpenseId { get; set; }
    public required Guid UserId { get; set; }
    public decimal? Share { get; set; }
}