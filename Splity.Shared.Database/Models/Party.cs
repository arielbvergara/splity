namespace Splity.Shared.Database.Models;

public class Party
{
    public required Guid PartyId { get; set; }
    public required Guid OwnerId { get; set; }
    public required string Name { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
}