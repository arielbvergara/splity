using System.Data;
using System.Text.Json;
using Npgsql;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories.Interfaces;

namespace Splity.Shared.Database.Repositories;

public class UserRepository(IDbConnection connection) : IUserRepository
{
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        const string sql = "SELECT UserId, Name, Email, CreatedAt FROM Users WHERE UserId = @userId";
        
        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@userId", userId);
        
        await using var reader = await command.ExecuteReaderAsync();
        
        if (!await reader.ReadAsync())
        {
            return null;
        }
        
        return new User
        {
            UserId = reader.GetGuid("UserId"),
            Name = reader.GetString("Name"),
            Email = reader.GetString("Email"),
            CreatedAt = reader.IsDBNull("CreatedAt") ? null : reader.GetDateTime("CreatedAt")
        };
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        const string sql = "SELECT UserId, Name, Email, CreatedAt FROM Users WHERE Email = @email";
        
        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@email", email);
        
        await using var reader = await command.ExecuteReaderAsync();
        
        if (!await reader.ReadAsync())
        {
            return null;
        }
        
        return new User
        {
            UserId = reader.GetGuid("UserId"),
            Name = reader.GetString("Name"),
            Email = reader.GetString("Email"),
            CreatedAt = reader.IsDBNull("CreatedAt") ? null : reader.GetDateTime("CreatedAt")
        };
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        // Check if user already exists with this email
        var existingUser = await GetUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email '{request.Email}' already exists.");
        }

        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO Users (UserId, Name, Email, CreatedAt) 
            VALUES (@userId, @name, @email, @createdAt)";

        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@email", request.Email);
        command.Parameters.AddWithValue("@createdAt", createdAt);

        await command.ExecuteNonQueryAsync();

        return new User
        {
            UserId = userId,
            Name = request.Name,
            Email = request.Email,
            CreatedAt = createdAt
        };
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        const string sql = "DELETE FROM Users WHERE UserId = @userId";
        
        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@userId", userId);
        
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<UserDto?> GetUserByIdWithDetailsAsync(Guid userId)
    {
        await using var select =
            new NpgsqlCommand(GetUserByIdWithDetailsSql, (NpgsqlConnection)connection);
        select.Parameters.AddWithValue("userId", userId);
        await using var reader = await select.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null; // User not found
        }

        var userJson = reader.GetString("UserJson");

        // Deserialize the JSON to UserDto object
        return JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private const string GetUserByIdWithDetailsSql = @"WITH user_data AS (
    SELECT
        u.UserId,
        u.Name,
        u.Email,
        u.CreatedAt,
        (
            SELECT COALESCE(jsonb_agg(
                jsonb_build_object(
                    'PartyId', p.PartyId,
                    'OwnerId', p.OwnerId,
                    'Name', p.Name,
                    'CreatedAt', p.CreatedAt
                )
            ), '[]'::jsonb)
            FROM Parties p
            WHERE p.OwnerId = u.UserId
        ) AS OwnedParties,
        (
            SELECT COALESCE(jsonb_agg(
                jsonb_build_object(
                    'ExpenseId', e.ExpenseId,
                    'PartyId', e.PartyId,
                    'PayerId', e.PayerId,
                    'Description', e.Description,
                    'Amount', e.Amount,
                    'CreatedAt', e.CreatedAt
                )
            ), '[]'::jsonb)
            FROM Expenses e
            WHERE e.PayerId = u.UserId
        ) AS PaidExpenses,
        (
            SELECT COALESCE(jsonb_agg(
                jsonb_build_object(
                    'PartyId', pc.PartyId,
                    'UserId', pc.UserId
                )
            ), '[]'::jsonb)
            FROM PartyContributors pc
            WHERE pc.UserId = u.UserId
        ) AS PartyContributions,
        (
            SELECT COALESCE(jsonb_agg(
                jsonb_build_object(
                    'ExpenseId', ep.ExpenseId,
                    'UserId', ep.UserId,
                    'Share', ep.Share
                )
            ), '[]'::jsonb)
            FROM ExpenseParticipants ep
            WHERE ep.UserId = u.UserId
        ) AS ExpenseParticipators
    FROM Users u
    WHERE u.UserId = :userId
)
SELECT
    jsonb_build_object(
        'UserId', ud.UserId,
        'Name', ud.Name,
        'Email', ud.Email,
        'CreatedAt', ud.CreatedAt,
        'OwnedParties', ud.OwnedParties,
        'PaidExpenses', ud.PaidExpenses,
        'PartyContributions', ud.PartyContributions,
        'ExpenseParticipators', ud.ExpenseParticipators
    )::text AS UserJson
FROM user_data ud;";
}
