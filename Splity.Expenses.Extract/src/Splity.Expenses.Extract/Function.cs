using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Splity.Shared.AI;
using Splity.Shared.Storage;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Expenses.Extract;

public class Function
{
    private readonly IS3BucketService _s3BucketService;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;

    private static readonly Lazy<IAmazonS3> S3Client = new(() =>
        new AmazonS3Client(RegionEndpoint.GetBySystemName(
            Environment.GetEnvironmentVariable("AWS_BUCKET_REGION")!)));

    public Function() : this(
        null)
    {
    }

    public Function(IS3BucketService? s3BucketService = null,
        IDocumentIntelligenceService? documentIntelligenceService = null)
    {
        _s3BucketService = s3BucketService ?? new S3BucketService(
            S3Client.Value,
            Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")!,
            Environment.GetEnvironmentVariable("AWS_BUCKET_REGION")!);

        _documentIntelligenceService = documentIntelligenceService ??
                                       new DocumentIntelligenceService(
                                           Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY")!,
                                           Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT")!);
    }

    /// <summary>
    /// Lambda function handler for processing file uploads via API Gateway
    /// </summary>
    /// <param name="request">The API Gateway proxy request containing the file data</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>API Gateway proxy response</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing request: {request.HttpMethod} {request.Path}");
        try
        {
            // if (request.HttpMethod != "PUT")
            // {
            //     return CreateResponse(405, $"Method not allowed: {request.HttpMethod}", GetCorsHeaders());
            // }

            // Get file content
            byte[] fileContent;
            const string fileName = "uploaded-file";

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
            var uploadedFileUrl = await _s3BucketService.UploadFileAsync(fileContent, "expenses", "splity");

            // OCR
            var receipt = await _documentIntelligenceService.AnalyzeReceipt(uploadedFileUrl);

            return CreateResponse(200, JsonSerializer.Serialize(new
            {
                fileURL = uploadedFileUrl,
                receipt
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
        Dictionary<string, string> headers = null)
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
            { "Access-Control-Allow-Origin", "*" },
            {
                "Access-Control-Allow-Headers",
                "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,x-filename"
            },
            { "Access-Control-Allow-Methods", "GET,POST,OPTIONS" },
            { "Content-Type", "application/json" }
        };
    }
}