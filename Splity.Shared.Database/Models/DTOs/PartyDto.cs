namespace Splity.Shared.Database.Models.DTOs;

public class PartyDto : Party
{
    public User? Owner { get; set; }
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<PartyContributor> Contributors { get; set; } = new List<PartyContributor>();
    public ICollection<PartyBillsImage> BillImages { get; set; } = new List<PartyBillsImage>();
}