namespace Splity.Shared.Database.Models.Commands;

public class CreatePartyRequest
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
}