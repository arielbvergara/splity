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

public class Function(IDbConnection connection,
    IS3BucketService? s3BucketService = null,
    IDocumentIntelligenceService? documentIntelligenceService = null,
    IPartyRepository? partyRepository = null) : BaseLambdaFunction
{
    private static readonly Lazy<IAmazonS3> S3Client = new(() =>
        new AmazonS3Client(RegionEndpoint.GetBySystemName(
            Environment.GetEnvironmentVariable("AWS_BUCKET_REGION")!)));

    private readonly IPartyRepository _partyRepository = partyRepository ?? new PartyRepository(connection);
    private readonly IS3BucketService _s3BucketService = s3BucketService ??
                       new S3BucketService(
                           S3Client.Value,
                           Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")!,
                           Environment.GetEnvironmentVariable("AWS_BUCKET_REGION")!);
    private readonly IDocumentIntelligenceService _documentIntelligenceService = documentIntelligenceService ??
                                   new DocumentIntelligenceService(
                                       Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY")!,
                                       Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT")!);

    public Function() : this(CreateDatabaseConnection(), null, null, null)
    {
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
