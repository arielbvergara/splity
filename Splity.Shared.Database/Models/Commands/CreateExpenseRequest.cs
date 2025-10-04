namespace Splity.Shared.Database.Models.Commands;

public class CreateExpenseRequest
{
    public string Description { get; set; }
    public decimal Amount { get; set; }
}