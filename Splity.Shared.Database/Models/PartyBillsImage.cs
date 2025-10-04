using System.ComponentModel.DataAnnotations;

namespace Splity.Shared.Database.Models;

public class PartyBillsImage
{
    public required Guid BillId { get; set; }
    public required string BillFileTitle { get; set; }
    public required Guid PartyId { get; set; }

    [Url]
    public required string ImageURL { get; set; }
}