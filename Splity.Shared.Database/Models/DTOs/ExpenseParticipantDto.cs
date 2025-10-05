namespace Splity.Shared.Database.Models;

public class ExpenseParticipantDto : ExpenseParticipant
{
    public Expense? Expense { get; set; }
    public User? User { get; set; }
}