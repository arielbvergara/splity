using System.Data;
using System.Text.Json;
using Npgsql;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
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

    public async Task<Party> GetPartyById(Guid partyId)
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
    var party = JsonSerializer.Deserialize<Party>(partyJson, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    return party;
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
                                'Share', ep.Share,
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