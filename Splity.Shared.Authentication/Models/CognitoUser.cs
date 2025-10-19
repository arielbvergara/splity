namespace Splity.Shared.Authentication.Models;

public class CognitoUser
{
    public string CognitoUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? SplityUserId { get; set; }
    public List<string> Roles { get; set; } = new();
    
    public bool IsAuthenticated => !string.IsNullOrEmpty(CognitoUserId);
}

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public CognitoUser? User { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public string? RefreshToken { get; set; }
}