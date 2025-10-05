using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Models.DTOs;

namespace Splity.Shared.Database.Repositories.Interfaces;

public interface IPartyRepository
{
    Task<int> CreatePartyBillImageAsync(CreatePartyBillImageRequest request);
    Task<PartyDto> GetPartyById(Guid partyId);
    Task<PartyDto> CreateParty(CreatePartyRequest request);
}