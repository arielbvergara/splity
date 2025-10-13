using System;
using System.Data;
using System.Net;
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

            // SQL commands to create all required tables
            var createUsersSql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId UUID PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    CognitoUserId TEXT,
                    CreatedAt TIMESTAMP DEFAULT NOW()
                );";

            var createPartiesSql = @"
                CREATE TABLE IF NOT EXISTS Parties (
                    PartyId UUID PRIMARY KEY,
                    OwnerId UUID NOT NULL,
                    Name TEXT NOT NULL,
                    CreatedAt TIMESTAMP DEFAULT NOW()
                );";

            var createExpensesSql = @"
                CREATE TABLE IF NOT EXISTS Expenses (
                    ExpenseId UUID PRIMARY KEY,
                    PartyId UUID NOT NULL,
                    PayerId UUID NOT NULL,
                    Description TEXT NOT NULL,
                    Amount NUMERIC(10, 2) NOT NULL,
                    CreatedAt TIMESTAMP DEFAULT NOW()
                );";

            var createPartyContributorsSql = @"
                CREATE TABLE IF NOT EXISTS PartyContributors (
                    PartyId UUID NOT NULL,
                    UserId UUID NOT NULL,
                    PRIMARY KEY (PartyId, UserId)
                );";

            var createExpenseParticipantsSql = @"
                CREATE TABLE IF NOT EXISTS ExpenseParticipants (
                    ExpenseId UUID NOT NULL,
                    UserId UUID NOT NULL,
                    Share NUMERIC(10, 2),
                    PRIMARY KEY (ExpenseId, UserId)
                );";

            var createPartyBillsImagesSql = @"
                CREATE TABLE IF NOT EXISTS PartyBillsImages (
                    BillId UUID PRIMARY KEY,
                    BillFileTitle TEXT NOT NULL,
                    PartyId UUID NOT NULL,
                    ImageURL TEXT NOT NULL
                );";

            // Execute each CREATE TABLE statement
            await ExecuteSqlAsync(_connection, createUsersSql, context);
            await ExecuteSqlAsync(_connection, createPartiesSql, context);
            await ExecuteSqlAsync(_connection, createExpensesSql, context);
            await ExecuteSqlAsync(_connection, createPartyContributorsSql, context);
            await ExecuteSqlAsync(_connection, createExpenseParticipantsSql, context);
            await ExecuteSqlAsync(_connection, createPartyBillsImagesSql, context);

            context.Logger.LogInformation("Database schema initialization completed successfully!");

            return CreateSuccessResponse(HttpStatusCode.OK, new { message = "Database schema initialized successfully", tablesCreated = 6 }, "GET");
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
}
