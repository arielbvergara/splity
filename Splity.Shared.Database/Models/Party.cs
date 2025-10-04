namespace Splity.Shared.Database.Models;

public class Party
{
    public required Guid PartyId { get; set; }
    public required Guid OwnerId { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? Owner { get; set; }
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<PartyContributor> Contributors { get; set; } = new List<PartyContributor>();
    public ICollection<PartyBillsImage> BillImages { get; set; } = new List<PartyBillsImage>();
}