using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Splity.Shared.Authentication.Models;
using Splity.Shared.Authentication.Services.Interfaces;

namespace Splity.Shared.Authentication.Services;

public class JwtTokenValidator(HttpClient httpClient, string userPoolId, string clientId, string region = "eu-west-2")
    : IJwtTokenValidator
{
    private readonly string _cognitoIssuer = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

    public async Task<CognitoUser?> ValidateTokenAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // Parse token to check claims before validation
            var jsonToken = handler.ReadJwtToken(token);

            // Get the JWT keys from Cognito
            var keys = await GetJwtKeysAsync();
            if (keys == null || !keys.Any())
            {
                return null;
            }

            // Check if token has audience claim
            var hasAudience = jsonToken.Claims.Any(c => c.Type == "aud" && !string.IsNullOrEmpty(c.Value));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys,
                ValidateIssuer = true,
                ValidIssuer = _cognitoIssuer,
                ValidateAudience = hasAudience,
                ValidAudience = hasAudience ? clientId : null,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            return ExtractUserFromClaims(principal.Claims);
        }
        catch (Exception e)
        {
            Console.WriteLine("ValidateTokenAsync ERROR: " + e.Message);
            return null;
        }
    }

    private async Task<IEnumerable<SecurityKey>?> GetJwtKeysAsync()
    {
        try
        {
            var jwksUri = $"{_cognitoIssuer}/.well-known/jwks.json";
            var response = await httpClient.GetAsync(jwksUri);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jwksJson = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var jwks = JsonSerializer.Deserialize<JwksResponse>(jwksJson, options);

            return jwks?.Keys?.Select(key => {
                var rsaParameters = new System.Security.Cryptography.RSAParameters
                {
                    Modulus = FromBase64UrlString(key.N),
                    Exponent = FromBase64UrlString(key.E)
                };
                var rsa = System.Security.Cryptography.RSA.Create();
                rsa.ImportParameters(rsaParameters);
                return new RsaSecurityKey(rsa) { KeyId = key.Kid };
            });
        }
        catch(Exception e)
        {
            Console.WriteLine("GetJwtKeysAsync ERROR: " + e.Message);
            return null;
        }
    }

    private static byte[] FromBase64UrlString(string base64Url)
    {
        // Replace URL-safe characters
        string base64 = base64Url.Replace('-', '+').Replace('_', '/');
        
        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        return Convert.FromBase64String(base64);
    }

    private static CognitoUser ExtractUserFromClaims(IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();
        
        // For access tokens, subject is in nameidentifier claim
        var userId = claimsList.FirstOrDefault(c => c.Type == "sub")?.Value ?? 
                     claimsList.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? 
                     string.Empty;
        
        // Username might be available in access tokens
        var username = claimsList.FirstOrDefault(c => c.Type == "username")?.Value ?? string.Empty;
        
        return new CognitoUser
        {
            CognitoUserId = userId,
            Email = claimsList.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
            Name = claimsList.FirstOrDefault(c => c.Type == "name" || c.Type == "given_name")?.Value ?? username,
            Roles = claimsList.Where(c => c.Type == "cognito:groups").Select(c => c.Value).ToList()
        };
    }

    private class JwksResponse
    {
        public List<JwkKey>? Keys { get; set; }
    }

    private class JwkKey
    {
        public string Kid { get; set; } = string.Empty;
        public string Kty { get; set; } = string.Empty;
        public string Use { get; set; } = string.Empty;
        public string N { get; set; } = string.Empty;
        public string E { get; set; } = string.Empty;
    }
}