namespace Splity.Shared.Database.Models.DTOs;

public class ExpenseDto : Expense
{
    public Party? Party { get; set; }
    public User? Payer { get; set; }
    public ICollection<ExpenseParticipant> Participants { get; set; } = new List<ExpenseParticipant>();
}