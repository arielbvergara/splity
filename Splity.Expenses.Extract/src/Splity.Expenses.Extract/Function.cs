using System.Data;
using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Splity.Shared.AI;
using Splity.Shared.Common;
using Splity.Shared.Database;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;
using Splity.Shared.Storage;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Splity.Expenses.Extract;

public class Function : BaseLambdaFunction
{
    private readonly IPartyRepository _partyRepository;
    private IS3BucketService _s3BucketService;
    private IDocumentIntelligenceService _documentIntelligenceService;
    private IAmazonS3 _s3Client;

    public Function(IDbConnection? connection = null,
        IS3BucketService? s3BucketService = null,
        IDocumentIntelligenceService? documentIntelligenceService = null,
        IPartyRepository? partyRepository = null)
    {
        var dbConnection = connection ?? CreateDatabaseConnection();
        _partyRepository = partyRepository ?? new PartyRepository(dbConnection);
        
        // Initialize services with configuration or fallback to environment variables
        if (s3BucketService != null && documentIntelligenceService != null)
        {
            _s3BucketService = s3BucketService;
            _documentIntelligenceService = documentIntelligenceService;
            _s3Client = new AmazonS3Client(RegionEndpoint.EUCentral1); // Default region
        }
        else
        {
            InitializeServices();
        }
    }

    public Function() : this(null, null, null, null)
    {
    }

    private void InitializeServices()
    {
        try
        {
            // Try to initialize with configuration service
            Configuration.InitializeAsync().Wait();
            
            var bucketRegion = Configuration.Aws.BucketRegion ?? "eu-central-1";
            _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(bucketRegion));
            
            _s3BucketService = new S3BucketService(
                _s3Client,
                Configuration.Aws.BucketName ?? throw new InvalidOperationException("AWS Bucket name not configured"),
                bucketRegion);
                
            _documentIntelligenceService = new DocumentIntelligenceService(
                Configuration.Azure.DocumentIntelligenceApiKey ?? throw new InvalidOperationException("Document Intelligence API key not configured"),
                Configuration.Azure.DocumentIntelligenceEndpoint ?? throw new InvalidOperationException("Document Intelligence endpoint not configured"));
        }
        catch
        {
            // Fallback to environment variables
            var bucketRegion = Environment.GetEnvironmentVariable("AWS_BUCKET_REGION") ?? "eu-central-1";
            _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(bucketRegion));
            
            _s3BucketService = new S3BucketService(
                _s3Client,
                Environment.GetEnvironmentVariable("AWS_BUCKET_NAME") ?? throw new InvalidOperationException("AWS_BUCKET_NAME environment variable is required"),
                bucketRegion);
                
            _documentIntelligenceService = new DocumentIntelligenceService(
                Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY") ?? throw new InvalidOperationException("DOCUMENT_INTELLIGENCE_API_KEY environment variable is required"),
                Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT") ?? throw new InvalidOperationException("DOCUMENT_INTELLIGENCE_ENDPOINT environment variable is required"));
        }
    }

    /// <summary>
    /// Lambda function handler for processing file uploads via API Gateway
    /// </summary>
    /// <param name="request">The API Gateway proxy request containing the file data</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>API Gateway proxy response</returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        try
        {
            // Validate HTTP method
            var methodValidation = ValidateHttpMethod(request, "PUT");
            if (methodValidation != null)
            {
                return methodValidation;
            }

            if (request.QueryStringParameters == null || !request.QueryStringParameters.TryGetValue("partyId", out var partyId))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Missing or invalid partyId", "PUT");
            }

            if (!Guid.TryParse(partyId, out var partyIdGuid))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid partyId format", "PUT");
            }

            if (request.QueryStringParameters == null || !request.QueryStringParameters.TryGetValue("fileName", out var fileName))
            {
                fileName = "default.png";
            }

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
                return CreateErrorResponse(HttpStatusCode.BadRequest, "No file content provided", "PUT");
            }

            // S3
            var uploadedFileUrl = await _s3BucketService.UploadFileAsync(fileContent, fileName, "splity");

            // Create party bill image
            var createPartyBillImageTask = _partyRepository.CreatePartyBillImageAsync(new CreatePartyBillImageRequest
            {
                BillId = Guid.NewGuid(),
                PartyId = partyIdGuid,
                ImageUrl = uploadedFileUrl,
                Title = fileName
            });

            // OCR
            var analyzeReceiptTask = _documentIntelligenceService.AnalyzeReceipt(uploadedFileUrl);

            // Await both tasks
            await Task.WhenAll(createPartyBillImageTask, analyzeReceiptTask);

            return CreateSuccessResponse(HttpStatusCode.OK, new
            {
                partyId,
                fileURL = uploadedFileUrl,
                analyzeReceiptTask.Result
            }, "PUT");
        }
        catch (ArgumentException argEx)
        {
            context.Logger.LogError($"Argument validation error: {argEx.Message}");
            return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request parameters", "PUT");
        }
        catch (UnauthorizedAccessException authEx)
        {
            context.Logger.LogError($"Authorization error: {authEx.Message}");
            return CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized access", "PUT");
        }
        catch (TimeoutException timeoutEx)
        {
            context.Logger.LogError($"Operation timeout: {timeoutEx.Message}");
            return CreateErrorResponse(HttpStatusCode.RequestTimeout, "Request timeout", "PUT");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Unexpected error processing file upload: {ex.Message}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error", "PUT");
        }
    }
}
