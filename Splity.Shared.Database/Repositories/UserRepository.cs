using System.Data;
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
}