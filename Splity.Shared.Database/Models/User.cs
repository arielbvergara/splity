using System.ComponentModel.DataAnnotations;

namespace Splity.Shared.Database.Models;

public class User
{
    public required Guid UserId { get; set; }
    public required string Name { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    // Navigation properties
    public ICollection<Party> OwnedParties { get; set; } = new List<Party>();
    public ICollection<Expense> PaidExpenses { get; set; } = new List<Expense>();
    public ICollection<PartyContributor> PartyContributions { get; set; } = new List<PartyContributor>();
    public ICollection<ExpenseParticipant> ExpenseParticipators { get; set; } = new List<ExpenseParticipant>();
}