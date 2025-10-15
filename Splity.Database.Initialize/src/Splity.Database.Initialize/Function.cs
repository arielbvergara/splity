using System;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Splity.Shared.Common;
using Splity.Shared.Database;

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

            // Split script by semicolons and execute each statement
            var sqlStatements = sqlScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var statement in sqlStatements)
            {
                var trimmedStatement = statement.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedStatement) && !trimmedStatement.StartsWith("--"))
                {
                    await ExecuteSqlAsync(_connection, trimmedStatement, context);
                }
            }

            context.Logger.LogInformation($"Database schema initialization completed successfully! Executed {sqlStatements.Length} statements.");

            return CreateSuccessResponse(HttpStatusCode.OK, new { message = "Database schema initialized successfully", statementsExecuted = sqlStatements.Length }, "GET");
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
