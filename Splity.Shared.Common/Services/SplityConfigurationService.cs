using Splity.Shared.Common.Services;

namespace Splity.Shared.Common.Configuration;

/// <summary>
/// Configuration service for Splity application settings
/// Retrieves configuration from AWS Parameter Store with fallback to environment variables
/// </summary>
public interface ISplityConfigurationService
{
    /// <summary>
    /// Initialize configuration by loading from Parameter Store
    /// </summary>
    /// <param name="environment">Environment name (dev, staging, prod)</param>
    Task InitializeAsync(string? environment = null);

    /// <summary>
    /// Database configuration
    /// </summary>
    DatabaseConfiguration Database { get; }
    
    /// <summary>
    /// AWS configuration
    /// </summary>
    AwsConfiguration Aws { get; }
    
    /// <summary>
    /// Azure Document Intelligence configuration
    /// </summary>
    AzureConfiguration Azure { get; }
    
    /// <summary>
    /// Authentication configuration
    /// </summary>
    AuthenticationConfiguration Authentication { get; }
    
    /// <summary>
    /// Application configuration
    /// </summary>
    ApplicationConfiguration Application { get; }
}

public class SplityConfigurationService : ISplityConfigurationService
{
    private readonly IParameterStoreService _parameterStore;
    private bool _initialized = false;

    public SplityConfigurationService(IParameterStoreService? parameterStore = null)
    {
        _parameterStore = parameterStore ?? new ParameterStoreService();
        Database = new DatabaseConfiguration();
        Aws = new AwsConfiguration();
        Azure = new AzureConfiguration();
        Authentication = new AuthenticationConfiguration();
        Application = new ApplicationConfiguration();
    }

    public DatabaseConfiguration Database { get; private set; }
    public AwsConfiguration Aws { get; private set; }
    public AzureConfiguration Azure { get; private set; }
    public AuthenticationConfiguration Authentication { get; private set; }
    public ApplicationConfiguration Application { get; private set; }

    public async Task InitializeAsync(string? environment = null)
    {
        if (_initialized) return;

        environment ??= Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")?.Contains("-dev") == true ? "dev" :
                       Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")?.Contains("-staging") == true ? "staging" :
                       Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")?.Contains("-prod") == true ? "prod" : "dev";

        var parameterPath = $"/splity/{environment}/";

        try
        {
            // Load all parameters from Parameter Store
            var parameters = await _parameterStore.GetParametersByPathAsync(parameterPath);

            // Database Configuration
            Database = new DatabaseConfiguration
            {
                Username = GetConfigValue(parameters, "database/username", "CLUSTER_USERNAME"),
                Hostname = GetConfigValue(parameters, "database/hostname", "CLUSTER_HOSTNAME"),
                Database = GetConfigValue(parameters, "database/name", "CLUSTER_DATABASE"),
                Region = GetConfigValue(parameters, "database/region", "AWS_REGION") ?? "eu-west-2"
            };

            // AWS Configuration
            Aws = new AwsConfiguration
            {
                BucketName = GetConfigValue(parameters, "aws/bucket/name", "AWS_BUCKET_NAME"),
                BucketRegion = GetConfigValue(parameters, "aws/bucket/region", "AWS_BUCKET_REGION"),
                Region = GetConfigValue(parameters, "aws/region", "AWS_REGION") ?? "eu-west-2"
            };

            // Azure Configuration
            Azure = new AzureConfiguration
            {
                DocumentIntelligenceEndpoint = GetConfigValue(parameters, "azure/document-intelligence/endpoint", "DOCUMENT_INTELLIGENCE_ENDPOINT"),
                DocumentIntelligenceApiKey = GetConfigValue(parameters, "azure/document-intelligence/api-key", "DOCUMENT_INTELLIGENCE_API_KEY")
            };

            // Authentication Configuration
            Authentication = new AuthenticationConfiguration
            {
                CognitoUserPoolId = GetConfigValue(parameters, "cognito/user-pool-id", "COGNITO_USER_POOL_ID"),
                CognitoClientId = GetConfigValue(parameters, "cognito/client-id", "COGNITO_CLIENT_ID")
            };

            // Application Configuration
            Application = new ApplicationConfiguration
            {
                AllowedOrigins = GetConfigValue(parameters, "application/allowed-origins", "ALLOWED_ORIGINS") ?? "*",
                Environment = environment
            };

            _initialized = true;
        }
        catch (Exception)
        {
            // If Parameter Store fails, fall back to environment variables only
            await InitializeFallback();
        }
    }

    private Task InitializeFallback()
    {
        Database = new DatabaseConfiguration
        {
            Username = Environment.GetEnvironmentVariable("CLUSTER_USERNAME"),
            Hostname = Environment.GetEnvironmentVariable("CLUSTER_HOSTNAME"),
            Database = Environment.GetEnvironmentVariable("CLUSTER_DATABASE"),
            Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2"
        };

        Aws = new AwsConfiguration
        {
            BucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"),
            BucketRegion = Environment.GetEnvironmentVariable("AWS_BUCKET_REGION"),
            Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2"
        };

        Azure = new AzureConfiguration
        {
            DocumentIntelligenceEndpoint = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT"),
            DocumentIntelligenceApiKey = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY")
        };

        Authentication = new AuthenticationConfiguration
        {
            CognitoUserPoolId = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID"),
            CognitoClientId = Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID")
        };

        Application = new ApplicationConfiguration
        {
            AllowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "*",
            Environment = "dev"
        };

        _initialized = true;
        return Task.CompletedTask;
    }

    private static string? GetConfigValue(Dictionary<string, string> parameters, string parameterKey, string environmentKey)
    {
        // Try Parameter Store first
        if (parameters.TryGetValue(parameterKey, out var paramValue) && !string.IsNullOrEmpty(paramValue))
        {
            return paramValue;
        }

        // Fall back to environment variable
        return Environment.GetEnvironmentVariable(environmentKey);
    }
}

public class DatabaseConfiguration
{
    public string? Username { get; set; }
    public string? Hostname { get; set; }
    public string? Database { get; set; }
    public string? Region { get; set; }
}

public class AwsConfiguration
{
    public string? BucketName { get; set; }
    public string? BucketRegion { get; set; }
    public string? Region { get; set; }
}

public class AzureConfiguration
{
    public string? DocumentIntelligenceEndpoint { get; set; }
    public string? DocumentIntelligenceApiKey { get; set; }
}

public class AuthenticationConfiguration
{
    public string? CognitoUserPoolId { get; set; }
    public string? CognitoClientId { get; set; }
}

public class ApplicationConfiguration
{
    public string AllowedOrigins { get; set; } = "*";
    public string Environment { get; set; } = "dev";
}