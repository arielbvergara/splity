using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Models.DTOs;

namespace Splity.Shared.Database.Repositories.Interfaces;

public interface IPartyRepository
{
    Task<int> CreatePartyBillImageAsync(CreatePartyBillImageRequest request);
    Task<PartyDto?> GetPartyById(Guid partyId);
    Task<IEnumerable<PartyDto>> GetPartiesByUserId(Guid userId);
    Task<PartyDto> CreateParty(CreatePartyRequest request, Guid ownerId);
    Task<PartyDto?> UpdateParty(UpdatePartyRequest request);
    Task<int> DeletePartyById(Guid partyId);
}
