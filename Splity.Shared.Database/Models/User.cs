using System.ComponentModel.DataAnnotations;

namespace Splity.Shared.Database.Models;

public class User
{
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
    [EmailAddress]
    public required string Email { get; set; }
    public string? CognitoUserId { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
}
