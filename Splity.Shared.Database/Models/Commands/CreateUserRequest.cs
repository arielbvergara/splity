using System.ComponentModel.DataAnnotations;

namespace Splity.Shared.Database.Models.Commands;

public class CreateUserRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? CognitoUserId { get; set; }
}
