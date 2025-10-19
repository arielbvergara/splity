using System.Data;
using System.Net;
using System.Reflection;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Common;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Database.Initialize;

public class Function : BaseLambdaFunction
{
    private readonly IDbConnection _connection;
    
    public Function(IDbConnection? connection = null)
    {
        _connection = connection ?? CreateDatabaseConnection();
    }
    
    public Function() : this(null)
    {
    }
    
    /// <summary>
    /// Lambda function to initialize the database schema
    /// </summary>
    /// <param name="request">The API Gateway proxy request</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation("Initializing database schema...");

            // Read SQL script from embedded resource
            var sqlScript = ReadEmbeddedSqlScript();
            context.Logger.LogInformation($"Loaded SQL script with {sqlScript.Length} characters");

            // Split script by semicolons and filter/sort statements
            var allStatements = sqlScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) && !s.StartsWith("--"))
                .ToList();
            
            context.Logger.LogInformation($"Parsed {allStatements.Count} SQL statements from script");
            foreach (var stmt in allStatements)
            {
                var preview = stmt.Substring(0, Math.Min(50, stmt.Length)).Replace("\n", " ").Replace("\r", "");
                context.Logger.LogInformation($"Statement: {preview}...");
            }
            
            // Separate CREATE TABLE from CREATE INDEX statements to ensure proper execution order
            var createTableStatements = allStatements.Where(s => s.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase)).ToList();
            var createIndexStatements = allStatements.Where(s => s.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase)).ToList();
            var otherStatements = allStatements.Except(createTableStatements).Except(createIndexStatements).ToList();
            
            context.Logger.LogInformation($"Found {createTableStatements.Count} CREATE TABLE, {createIndexStatements.Count} CREATE INDEX, {otherStatements.Count} other statements");
            
            // Execute in proper order: CREATE TABLE first, then CREATE INDEX, then others
            var orderedStatements = createTableStatements.Concat(createIndexStatements).Concat(otherStatements);
            
            foreach (var statement in orderedStatements)
            {
                await ExecuteSqlAsync(_connection, statement, context);
            }

            var executedCount = orderedStatements.Count();
            context.Logger.LogInformation($"Database schema initialization completed successfully! Executed {executedCount} statements.");

            return CreateSuccessResponse(HttpStatusCode.OK, new { message = "Database schema initialized successfully", statementsExecuted = executedCount }, "GET");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error initializing database schema: {ex.Message}");
            context.Logger.LogError($"Stack trace: {ex.StackTrace}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, $"Database initialization failed: {ex.Message}", "GET");
        }
    }

    private static async Task ExecuteSqlAsync(IDbConnection connection, string sql, ILambdaContext context)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        context.Logger.LogInformation($"Executing SQL: {sql.Substring(0, Math.Min(100, sql.Length))}...");
        
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
        
        await Task.Run(() => command.ExecuteNonQuery());
    }
    
    private static string ReadEmbeddedSqlScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Splity.Database.Initialize.database-schema.sql";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
