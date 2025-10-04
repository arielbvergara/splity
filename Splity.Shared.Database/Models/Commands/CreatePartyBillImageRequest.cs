namespace Splity.Shared.Database.Models.Commands;

public class CreatePartyBillImageRequest
{
    public required Guid BillId { get; set; }
    public required Guid PartyId { get; set; }
    public required string Title { get; set; }
    public required string ImageUrl { get; set; }
}