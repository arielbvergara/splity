using Splity.Shared.Authentication.Models;

namespace Splity.Shared.Authentication.Services.Interfaces;

public interface IJwtTokenValidator
{
    Task<CognitoUser?> ValidateTokenAsync(string token);
}