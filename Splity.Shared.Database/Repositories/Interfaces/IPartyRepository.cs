using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;

namespace Splity.Shared.Database.Repositories.Interfaces;

public interface IPartyRepository
{
    Task<int> CreatePartyBillImageAsync(CreatePartyBillImageRequest request);
    Task<Party> GetPartyById(Guid partyId);
}