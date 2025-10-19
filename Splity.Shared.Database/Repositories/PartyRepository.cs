using System.Data;
using System.Text.Json;
using Npgsql;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Models.DTOs;
using Splity.Shared.Database.Repositories.Interfaces;

namespace Splity.Shared.Database.Repositories;

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

    public async Task<PartyDto?> GetPartyById(Guid partyId)
    {
        await using var select =
            new NpgsqlCommand(GetPartyByIdSql, (NpgsqlConnection)connection);
        select.Parameters.AddWithValue("partyId", partyId);
        await using var reader = await select.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null; // or throw an exception if party not found
        }

        var partyJson = reader.GetString("PartyJson");

        // Deserialize the JSON to Party object
        return JsonSerializer.Deserialize<PartyDto>(partyJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public async Task<PartyDto> CreateParty(CreatePartyRequest request, Guid ownerId)
    {
        var partyId = Guid.NewGuid();

        var sql = "INSERT INTO Parties(PartyId, OwnerId, Name) VALUES(@partyId, @ownerId, @name)";

        await using var insert = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        insert.Parameters.AddWithValue("@partyId", partyId);
        insert.Parameters.AddWithValue("@ownerId", ownerId);
        insert.Parameters.AddWithValue("@name", request.Name);

        await insert.ExecuteNonQueryAsync();

        // Return the created party with all its details
        return await GetPartyById(partyId);
    }

    public async Task<PartyDto?> UpdateParty(UpdatePartyRequest request)
    {
        // First check if party exists
        var existingParty = await GetPartyById(request.PartyId);
        if (existingParty == null)
        {
            return null;
        }

        var sql = "UPDATE Parties SET Name = @name WHERE PartyId = @partyId";

        await using var update = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        update.Parameters.AddWithValue("@partyId", request.PartyId);
        update.Parameters.AddWithValue("@name", request.Name ?? existingParty.Name);

        var rowsAffected = await update.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return null;
        }

        // Return the updated party with all its details
        return await GetPartyById(request.PartyId);
    }

    public async Task<int> DeletePartyById(Guid partyId)
    {
        const string sql = @"
        DELETE FROM ExpenseParticipants 
        WHERE ExpenseId IN (SELECT ExpenseId FROM Expenses WHERE PartyId = @partyId);
        
        DELETE FROM Expenses WHERE PartyId = @partyId;
        DELETE FROM PartyContributors WHERE PartyId = @partyId;
        DELETE FROM PartyBillsImages WHERE PartyId = @partyId;
        DELETE FROM Parties WHERE PartyId = @partyId;
    ";

        await using var delete = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        delete.Parameters.AddWithValue("@partyId", partyId);

        return await delete.ExecuteNonQueryAsync();
    }

    private const string GetPartyByIdSql = @"WITH party_data AS (
    SELECT
        p.PartyId,
        p.OwnerId,
        p.Name,
        p.CreatedAt,
        jsonb_build_object(
            'UserId', u.UserId,
            'Name', u.Name,
            'Email', u.Email
        ) AS Owner,
        (
            SELECT COALESCE(jsonb_agg(
                jsonb_build_object(
                    'ExpenseId', e.ExpenseId,
                    'PartyId', e.PartyId,
                    'PayerId', e.PayerId,
                    'Description', e.Description,
                    'Amount', e.Amount,
                    'CreatedAt', e.CreatedAt,
                    'Participants', (
                        SELECT COALESCE(jsonb_agg(
                            jsonb_build_object(
                                'ExpenseId', ep.ExpenseId,
                                'UserId', ep.UserId,
                                'Quantity', ep.Quantity,
                                'User', jsonb_build_object(
                                    'UserId', uep.UserId,
                                    'Name', uep.Name,
                                    'Email', uep.Email
                                )
                            )
                        ), '[]'::jsonb)
                        FROM ExpenseParticipants ep
                        JOIN Users uep ON ep.UserId = uep.UserId
                        WHERE ep.ExpenseId = e.ExpenseId
                    )
                )
            ), '[]'::jsonb)
            FROM Expenses e
            WHERE e.PartyId = p.PartyId
        ) AS Expenses,
        (
            SELECT COALESCE(jsonb_agg(
                jsonb_build_object(
                    'PartyId', pc.PartyId,
                    'UserId', pc.UserId,
                    'User', jsonb_build_object(
                        'UserId', uc.UserId,
                        'Name', uc.Name,
                        'Email', uc.Email
                    )
                )
            ), '[]'::jsonb)
            FROM PartyContributors pc
            JOIN Users uc ON pc.UserId = uc.UserId
            WHERE pc.PartyId = p.PartyId
        ) AS Contributors,
        (
            SELECT COALESCE(jsonb_agg(
                jsonb_build_object(
                    'BillId', pb.BillId,
                    'BillFileTitle', pb.BillFileTitle,
                    'PartyId', pb.PartyId,
                    'ImageURL', pb.ImageURL
                )
            ), '[]'::jsonb)
            FROM PartyBillsImages pb
            WHERE pb.PartyId = p.PartyId
        ) AS BillImages
    FROM Parties p
    JOIN Users u ON p.OwnerId = u.UserId
    WHERE p.PartyId = :partyId
)
SELECT
    jsonb_build_object(
        'PartyId', pd.PartyId,
        'OwnerId', pd.OwnerId,
        'Name', pd.Name,
        'CreatedAt', pd.CreatedAt,
        'Owner', pd.Owner,
        'Expenses', pd.Expenses,
        'Contributors', pd.Contributors,
        'BillImages', pd.BillImages
    )::text AS PartyJson
FROM party_data pd;
";
}