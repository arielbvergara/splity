using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace Splity.Shared.Common.Services;

/// <summary>
/// Service for retrieving configuration values from AWS Systems Manager Parameter Store
/// </summary>
public interface IParameterStoreService
{
    /// <summary>
    /// Get a parameter value from Parameter Store
    /// </summary>
    /// <param name="parameterName">The parameter name (can include path)</param>
    /// <param name="withDecryption">Whether to decrypt SecureString parameters</param>
    /// <returns>The parameter value</returns>
    Task<string?> GetParameterAsync(string parameterName, bool withDecryption = true);
    
    /// <summary>
    /// Get multiple parameters by path from Parameter Store
    /// </summary>
    /// <param name="path">The parameter path (e.g., /splity/dev/)</param>
    /// <param name="recursive">Whether to retrieve parameters recursively</param>
    /// <param name="withDecryption">Whether to decrypt SecureString parameters</param>
    /// <returns>Dictionary of parameter names and values</returns>
    Task<Dictionary<string, string>> GetParametersByPathAsync(string path, bool recursive = true, bool withDecryption = true);
}

/// <summary>
/// Implementation of Parameter Store service using AWS SDK
/// </summary>
public class ParameterStoreService : IParameterStoreService
{
    private readonly IAmazonSimpleSystemsManagement _ssmClient;

    public ParameterStoreService(IAmazonSimpleSystemsManagement? ssmClient = null)
    {
        _ssmClient = ssmClient ?? new AmazonSimpleSystemsManagementClient(RegionEndpoint.EUWest2);
    }

    /// <inheritdoc />
    public async Task<string?> GetParameterAsync(string parameterName, bool withDecryption = true)
    {
        try
        {
            var request = new GetParameterRequest
            {
                Name = parameterName,
                WithDecryption = withDecryption
            };

            var response = await _ssmClient.GetParameterAsync(request);
            return response.Parameter?.Value;
        }
        catch (ParameterNotFoundException)
        {
            // Parameter doesn't exist - return null
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve parameter '{parameterName}' from Parameter Store: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetParametersByPathAsync(string path, bool recursive = true, bool withDecryption = true)
    {
        var parameters = new Dictionary<string, string>();
        string? nextToken = null;

        try
        {
            do
            {
                var request = new GetParametersByPathRequest
                {
                    Path = path,
                    Recursive = recursive,
                    WithDecryption = withDecryption,
                    NextToken = nextToken,
                    MaxResults = 10 // AWS limit is 10 for GetParametersByPath
                };

                var response = await _ssmClient.GetParametersByPathAsync(request);
                
                foreach (var parameter in response.Parameters)
                {
                    // Remove the path prefix to get clean parameter names
                    var paramName = parameter.Name;
                    if (paramName.StartsWith(path))
                    {
                        paramName = paramName.Substring(path.Length).TrimStart('/');
                    }
                    
                    parameters[paramName] = parameter.Value;
                }

                nextToken = response.NextToken;
            } 
            while (!string.IsNullOrEmpty(nextToken));

            return parameters;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve parameters by path '{path}' from Parameter Store: {ex.Message}", ex);
        }
    }
}