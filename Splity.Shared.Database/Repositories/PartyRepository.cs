using System.Data;
using Npgsql;
using Splity.Shared.Database.Models;

namespace Splity.Shared.Database.Repositories;

public interface IPartyRepository
{
    Task<int> CreatePartyBillImageAsync(CreatePartyBillImageRequest request);
}

public class PartyRepository(IDbConnection connection) : IPartyRepository
{
    public async Task<int> CreatePartyBillImageAsync(CreatePartyBillImageRequest request)
    {
        var sql =
            $"INSERT INTO PartyBillsImages(BillId, BillFileTitle, PartyId, ImageURL) VALUES('{Guid.NewGuid()}', '{request.Title}', '{request.PartyId}', '{request.ImageUrl}')";

        await using var insert =
            new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        return insert.ExecuteNonQuery();
    }
}