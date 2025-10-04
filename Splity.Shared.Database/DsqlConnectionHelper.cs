using Amazon;
using Amazon.DSQL.Util;
using Amazon.Runtime.Credentials;
using Npgsql;

namespace Splity.Shared.Database;

public static class DsqlConnectionHelper
{
    public static NpgsqlConnection CreateConnection(
        string? clusterUser, string? clusterEndpoint, string regionName, string? databaseName = "postgres")
    {
        var region = RegionEndpoint.GetBySystemName(regionName);
        var awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Generate a fresh password token for each connection, to ensure the token is not expired when the connection
        // is established
        string password;
        string schema;
        if (clusterUser == "admin")
        {
            password = DSQLAuthTokenGenerator.GenerateDbConnectAdminAuthToken(
                awsCredentials, region, clusterEndpoint);
            schema = "public";
        }
        else
        {
            password =
                DSQLAuthTokenGenerator.GenerateDbConnectAuthToken(awsCredentials, region, clusterEndpoint);
            schema = "myschema";
        }

        var connBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = clusterEndpoint,
            Port = 5432,
            SslMode = SslMode.VerifyFull,
            SslNegotiation = SslNegotiation.Direct,
            Database = databaseName,
            Username = clusterUser,
            Password = password,
            IncludeErrorDetail = true
        };

        var conn = new NpgsqlConnection(connBuilder.ConnectionString);
        conn.Open();

        try
        {
            using var setSearchPath = new NpgsqlCommand($"SET search_path = {schema}", conn);
            setSearchPath.ExecuteNonQuery();
        }
        catch
        {
            conn.Close();
            throw;
        }

        return conn;
    }
}