using System.ComponentModel.DataAnnotations;

namespace Splity.Shared.Database.Models.Commands;

public class DeleteExpensesRequest
{
    [Required]
    public IEnumerable<Guid> ExpenseIds { get; set; } = new List<Guid>();
}