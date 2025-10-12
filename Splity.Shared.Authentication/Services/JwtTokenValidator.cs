using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Splity.Shared.Authentication.Models;
using Splity.Shared.Authentication.Services.Interfaces;

namespace Splity.Shared.Authentication.Services;

public class JwtTokenValidator : IJwtTokenValidator
{
    private readonly HttpClient _httpClient;
    private readonly string _cognitoIssuer;
    private readonly string _cognitoAudience;
    
    public JwtTokenValidator(HttpClient httpClient, string userPoolId, string clientId, string region = "eu-west-2")
    {
        _httpClient = httpClient;
        _cognitoIssuer = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";
        _cognitoAudience = clientId;
    }

    public async Task<CognitoUser?> ValidateTokenAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // Get the JWT keys from Cognito
            var keys = await GetJwtKeysAsync();
            if (keys == null || !keys.Any())
            {
                return null;
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys,
                ValidateIssuer = true,
                ValidIssuer = _cognitoIssuer,
                ValidateAudience = true,
                ValidAudience = _cognitoAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            return ExtractUserFromClaims(principal.Claims);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<IEnumerable<SecurityKey>?> GetJwtKeysAsync()
    {
        try
        {
            var jwksUri = $"{_cognitoIssuer}/.well-known/jwks.json";
            var response = await _httpClient.GetAsync(jwksUri);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jwksJson = await response.Content.ReadAsStringAsync();
            var jwks = System.Text.Json.JsonSerializer.Deserialize<JwksResponse>(jwksJson);
            
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
        catch
        {
            return null;
        }
    }

    private static byte[] FromBase64UrlString(string base64Url)
    {
        string padded = base64Url.Length % 4 == 0
            ? base64Url
            : base64Url + "=====".Substring(base64Url.Length % 4);
        string base64 = padded.Replace("-", "+").Replace("_", "/");
        return Convert.FromBase64String(base64);
    }

    private static CognitoUser ExtractUserFromClaims(IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();
        
        return new CognitoUser
        {
            CognitoUserId = claimsList.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
            Email = claimsList.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
            Name = claimsList.FirstOrDefault(c => c.Type == "name" || c.Type == "given_name")?.Value ?? string.Empty,
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