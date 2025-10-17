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
        const string sql = "SELECT UserId, Name, Email, CognitoUserId, CreatedAt FROM Users WHERE UserId = @userId";
        
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
            CognitoUserId = reader.IsDBNull("CognitoUserId") ? null : reader.GetString("CognitoUserId"),
            CreatedAt = reader.IsDBNull("CreatedAt") ? null : reader.GetDateTime("CreatedAt")
        };
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        const string sql = "SELECT UserId, Name, Email, CognitoUserId, CreatedAt FROM Users WHERE Email = @email";
        
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
            CognitoUserId = reader.IsDBNull("CognitoUserId") ? null : reader.GetString("CognitoUserId"),
            CreatedAt = reader.IsDBNull("CreatedAt") ? null : reader.GetDateTime("CreatedAt")
        };
    }

    public async Task<User?> GetUserByCognitoIdAsync(string cognitoUserId)
    {
        const string sql = "SELECT UserId, Name, Email, CognitoUserId, CreatedAt FROM Users WHERE CognitoUserId = @cognitoUserId";
        
        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@cognitoUserId", cognitoUserId);
        
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
            CognitoUserId = reader.IsDBNull("CognitoUserId") ? null : reader.GetString("CognitoUserId"),
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
            INSERT INTO Users (UserId, Name, Email, CognitoUserId, CreatedAt) 
            VALUES (@userId, @name, @email, @cognitoUserId, @createdAt)";

        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@email", request.Email);
        command.Parameters.AddWithValue("@cognitoUserId", request.CognitoUserId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@createdAt", createdAt);

        await command.ExecuteNonQueryAsync();

        return new User
        {
            UserId = userId,
            Name = request.Name,
            Email = request.Email,
            CognitoUserId = request.CognitoUserId,
            CreatedAt = createdAt
        };
    }

    public async Task<User?> UpdateUserAsync(UpdateUserRequest request)
    {
        // First check if the user exists
        var existingUser = await GetUserByIdAsync(request.UserId);
        if (existingUser == null)
        {
            return null;
        }
        
        // If email is being updated, check it doesn't already exist for another user
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != existingUser.Email)
        {
            var userWithEmail = await GetUserByEmailAsync(request.Email);
            if (userWithEmail != null && userWithEmail.UserId != request.UserId)
            {
                throw new InvalidOperationException($"User with email '{request.Email}' already exists.");
            }
        }

        // Build dynamic SQL based on what fields are being updated
        var setClauses = new List<string>();
        var parameters = new List<(string name, object value)>();
        
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            setClauses.Add("Name = @name");
            parameters.Add(("@name", request.Name));
        }
        
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            setClauses.Add("Email = @email");
            parameters.Add(("@email", request.Email));
        }
        
        if (setClauses.Count == 0)
        {
            // Nothing to update, return existing user
            return existingUser;
        }
        
        var sql = $"UPDATE Users SET {string.Join(", ", setClauses)} WHERE UserId = @userId";
        
        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@userId", request.UserId);
        
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        
        await command.ExecuteNonQueryAsync();
        
        // Return the updated user
        return await GetUserByIdAsync(request.UserId);
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
                    'Quantity', ep.Quantity
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
