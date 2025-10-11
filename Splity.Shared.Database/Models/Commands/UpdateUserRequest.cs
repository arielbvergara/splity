namespace Splity.Shared.Database.Models.Commands;

public class UpdateUserRequest
{
    public Guid UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}