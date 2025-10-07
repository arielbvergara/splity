namespace Splity.Shared.Database.Models.Commands;

public class DeleteExpensesResponse
{
    public bool Success { get; set; }
    public int DeletedCount { get; set; }
    public int RequestedCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public IEnumerable<Guid> DeletedExpenseIds { get; set; } = new List<Guid>();
}