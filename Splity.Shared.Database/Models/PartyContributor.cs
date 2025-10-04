namespace Splity.Shared.Database.Models;

public class PartyContributor
{
    public required Guid PartyId { get; set; }

    public required Guid UserId { get; set; }

    // Navigation properties
    public Party? Party { get; set; }
    public User? User { get; set; }
}