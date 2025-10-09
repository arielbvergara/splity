using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;

namespace Splity.Shared.Database.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<UserDto?> GetUserByIdWithDetailsAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> CreateUserAsync(CreateUserRequest request);
    Task<User?> UpdateUserAsync(UpdateUserRequest request);
    Task<bool> DeleteUserAsync(Guid userId);
}
