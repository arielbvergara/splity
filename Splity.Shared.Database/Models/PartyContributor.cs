namespace Splity.Shared.Database.Models;

public class PartyContributor
{
    public required Guid PartyId { get; set; }
    public required Guid UserId { get; set; }
}