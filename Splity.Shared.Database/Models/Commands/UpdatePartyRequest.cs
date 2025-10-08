namespace Splity.Shared.Database.Models.Commands;

public class UpdatePartyRequest
{
    public Guid PartyId { get; set; }
    public string? Name { get; set; }
}