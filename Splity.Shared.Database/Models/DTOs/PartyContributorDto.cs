namespace Splity.Shared.Database.Models;

public class PartyContributorDto : PartyContributor
{
    public Party? Party { get; set; }
    public User? User { get; set; }
}