namespace Splity.Shared.Database.Models;

public class UserDto : User
{
    public ICollection<Party> OwnedParties { get; set; } = new List<Party>();
    public ICollection<Expense> PaidExpenses { get; set; } = new List<Expense>();
    public ICollection<PartyContributor> PartyContributions { get; set; } = new List<PartyContributor>();
    public ICollection<ExpenseParticipant> ExpenseParticipators { get; set; } = new List<ExpenseParticipant>();
}