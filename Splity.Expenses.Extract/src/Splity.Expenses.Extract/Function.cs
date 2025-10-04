using System.Data;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Splity.Shared.AI;
using Splity.Shared.Database;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Storage;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Expenses.Extract;

public class Function
{
    private readonly IPartyRepository _partyRepository;
    private readonly IS3BucketService _s3BucketService;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;

    private static readonly Lazy<IAmazonS3> S3Client = new(() =>
        new AmazonS3Client(RegionEndpoint.GetBySystemName(
            Environment.GetEnvironmentVariable("AWS_BUCKET_REGION")!)));

    public Function() : this(
        DsqlConnectionHelper.CreateConnection(
            Environment.GetEnvironmentVariable("CLUSTER_USERNAME"),
            Environment.GetEnvironmentVariable("CLUSTER_HOSTNAME"),
            RegionEndpoint.EUWest2.SystemName,
            Environment.GetEnvironmentVariable("CLUSTER_DATABASE")))
    {
    }

    public Function(IDbConnection connection,
        IS3BucketService? s3BucketService = null,
        IDocumentIntelligenceService? documentIntelligenceService = null,
        IPartyRepository? partyRepository = null)
    {
        _s3BucketService = s3BucketService ??
                           new S3BucketService(
                               S3Client.Value,
                               Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")!,
                               Environment.GetEnvironmentVariable("AWS_BUCKET_REGION")!);

        _documentIntelligenceService = documentIntelligenceService ??
                                       new DocumentIntelligenceService(
                                           Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY")!,
                                           Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT")!);
        _partyRepository = partyRepository ??
                           new PartyRepository(connection);
    }

    /// <summary>
    /// Lambda function handler for processing file uploads via API Gateway
    /// </summary>
    /// <param name="request">The API Gateway proxy request containing the file data</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>API Gateway proxy response</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var fileName = request.Headers["x-filename"] ?? "uploaded-file";
        try
        {
            // Handle CORS preflight requests
            if (request.HttpMethod == "OPTIONS")
            {
                return CreateResponse(200, "", GetCorsHeaders());
            }

            // // Enable your PUT method validation
            // if (request.HttpMethod != "PUT")
            // {
            //     return CreateResponse(405, $"Method not allowed: {request.HttpMethod}", GetCorsHeaders());
            // }

            // Get file content
            byte[] fileContent;

            // Check if request body is base64 encoded
            if (request.IsBase64Encoded && !string.IsNullOrEmpty(request.Body))
            {
                fileContent = Convert.FromBase64String(request.Body);
            }
            else if (!string.IsNullOrEmpty(request.Body))
            {
                fileContent = Encoding.UTF8.GetBytes(request.Body);
            }
            else
            {
                return CreateResponse(400, "No file content provided", GetCorsHeaders());
            }

            // S3
            var uploadedFileUrl = await _s3BucketService.UploadFileAsync(fileContent, fileName, "splity");

            // Create party bill image
            var createPartyBillImageTask = _partyRepository.CreatePartyBillImageAsync(new CreatePartyBillImageRequest
            {
                BillId = Guid.NewGuid(),
                PartyId = Guid.Parse(request.QueryStringParameters["partyId"]),
                ImageUrl = uploadedFileUrl,
                Title = fileName
            });

            // OCR
            var analyzeReceiptTask = _documentIntelligenceService.AnalyzeReceipt(uploadedFileUrl);

            // Await both tasks
            await Task.WhenAll(createPartyBillImageTask, analyzeReceiptTask);

            return CreateResponse(200, JsonSerializer.Serialize(new
            {
                fileURL = uploadedFileUrl,
                analyzeReceiptTask.Result
            }), GetCorsHeaders());
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing file upload: {ex.Message}");

            var errorResponse = new
            {
                error = "Internal server error",
                message = ex.Message
            };

            return CreateResponse(500, JsonSerializer.Serialize(errorResponse), GetCorsHeaders());
        }
    }

    /// <summary>
    /// Create a standardized API Gateway response
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="body">Response body</param>
    /// <param name="headers">Response headers</param>
    /// <returns>API Gateway proxy response</returns>
    private APIGatewayProxyResponse CreateResponse(int statusCode, string body,
        Dictionary<string, string>? headers = null)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = body,
            Headers = headers ?? new Dictionary<string, string>(),
            IsBase64Encoded = false
        };
    }

    /// <summary>
    /// Get CORS headers for cross-origin requests
    /// </summary>
    /// <returns>Dictionary of CORS headers</returns>
    private Dictionary<string, string> GetCorsHeaders()
    {
        return new Dictionary<string, string>
        {
            { "Access-Control-Allow-Origin", Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "*" },
            {
                "Access-Control-Allow-Headers",
                "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,x-filename"
            },
            { "Access-Control-Allow-Methods", "PUT,OPTIONS" }, // Match your actual HTTP method
            { "Access-Control-Max-Age", "86400" }, // Cache preflight for 24 hours
            { "Content-Type", "application/json" }
        };
    }
}