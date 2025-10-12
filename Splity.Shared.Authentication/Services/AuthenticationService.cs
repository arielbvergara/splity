using System.Data;
using Amazon.Lambda.APIGatewayEvents;
using Splity.Shared.Authentication.Models;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories.Interfaces;

namespace Splity.Shared.Authentication.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IJwtTokenValidator _tokenValidator;
    private readonly IUserRepository _userRepository;

    public AuthenticationService(IJwtTokenValidator tokenValidator, IUserRepository userRepository)
    {
        _tokenValidator = tokenValidator;
        _userRepository = userRepository;
    }

    public async Task<CognitoUser?> GetUserFromRequestAsync(APIGatewayHttpApiV2ProxyRequest request)
    {
        var token = ExtractTokenFromRequest(request);
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var cognitoUser = await _tokenValidator.ValidateTokenAsync(token);
        if (cognitoUser == null || !cognitoUser.IsAuthenticated)
        {
            return null;
        }

        // Try to find the corresponding Splity user
        var splityUser = await _userRepository.GetUserByEmailAsync(cognitoUser.Email);
        if (splityUser != null)
        {
            cognitoUser.SplityUserId = splityUser.UserId;
        }

        return cognitoUser;
    }

    public async Task<Guid> EnsureUserExistsAsync(CognitoUser cognitoUser)
    {
        if (cognitoUser.SplityUserId.HasValue)
        {
            return cognitoUser.SplityUserId.Value;
        }

        // Check if user exists by email
        var existingUser = await _userRepository.GetUserByEmailAsync(cognitoUser.Email);
        if (existingUser != null)
        {
            return existingUser.UserId;
        }

        // Create new user
        var createUserRequest = new CreateUserRequest
        {
            Email = cognitoUser.Email,
            Name = !string.IsNullOrEmpty(cognitoUser.Name) ? cognitoUser.Name : cognitoUser.Email
        };

        var newUser = await _userRepository.CreateUserAsync(createUserRequest);
        return newUser.UserId;
    }

    public string? ExtractTokenFromRequest(APIGatewayHttpApiV2ProxyRequest request)
    {
        // Try Authorization header first
        if (request.Headers?.TryGetValue("Authorization", out var authHeader) == true)
        {
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
        }

        // Try cookies
        if (request.Cookies != null)
        {
            var accessTokenCookie = request.Cookies.FirstOrDefault(c => c.StartsWith("splity_access_token="));
            if (!string.IsNullOrEmpty(accessTokenCookie))
            {
                return accessTokenCookie.Split('=', 2)[1];
            }
        }

        // Try query parameters as fallback
        if (request.QueryStringParameters?.TryGetValue("token", out var tokenParam) == true)
        {
            return tokenParam;
        }

        return null;
    }
}