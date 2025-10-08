using System.Data;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using Splity.Shared.AI;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories.Interfaces;
using Splity.Shared.Storage;
using Xunit;

namespace Splity.Expenses.Extract.Tests;

public class FunctionTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WhenCalledWithConnection()
    {
        // This test verifies we can create mock constructors without real DB connections
        // The parameterless constructor tries to create a real connection, so we skip testing it
        // in unit tests. Integration tests would test the real connection.
        
        // Instead, we test that our constructor dependency injection works properly
        // Set required environment variables to prevent AWS S3 client initialization errors
        Environment.SetEnvironmentVariable("AWS_BUCKET_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("AWS_BUCKET_NAME", "test-bucket");
        Environment.SetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY", "test-key");
        Environment.SetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT", "https://test.cognitiveservices.azure.com/");
        
        var mockConnection = new Mock<IDbConnection>();
        var function = new Function(mockConnection.Object);
        
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateInstanceWithDefaultServices_WhenCalledWithConnectionOnly()
    {
        // Arrange
        // Set required environment variables to prevent AWS S3 client initialization errors
        Environment.SetEnvironmentVariable("AWS_BUCKET_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("AWS_BUCKET_NAME", "test-bucket");
        Environment.SetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY", "test-key");
        Environment.SetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT", "https://test.cognitiveservices.azure.com/");
        
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var function = new Function(mockConnection.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateInstanceWithProvidedServices_WhenCalledWithAllDependencies()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();

        // Act
        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnOkAndProcessFile_WhenValidBase64EncodedFileUpload()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var partyId = Guid.NewGuid();
        var uploadedUrl = "https://bucket.s3.amazonaws.com/receipt.jpg";
        var receipt = new Receipt
        {
            MerchantName = "Test Store",
            TransactionDate = DateTimeOffset.Now,
            Items = new List<ReceiptItem>
            {
                new() { Description = "Item 1", TotalItemPrice = 10.99, Quantity = 1 },
                new() { Description = "Item 2", TotalItemPrice = 5.50, Quantity = 2 }
            }
        };

        mockS3Service.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);
        mockDocumentService.Setup(d => d.AnalyzeReceipt(uploadedUrl))
            .ReturnsAsync(receipt);
        mockPartyRepository.Setup(r => r.CreatePartyBillImageAsync(It.IsAny<CreatePartyBillImageRequest>()))
            .ReturnsAsync(1);

        var fileContent = "fake file content";
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileContent));
        
        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = base64Content,
            IsBase64Encoded = true,
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", partyId.ToString() },
                { "fileName", "receipt.jpg" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because a successful file upload should return HTTP 200 OK");
        response.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        response.Body.Should().Contain(partyId.ToString(), "because the response should contain the party ID");
        response.Body.Should().Contain(uploadedUrl, "because the response should contain the uploaded file URL");
        response.Body.Should().Contain("Test Store", "because the response should contain the extracted merchant name");
        
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), "receipt.jpg", "splity"), Times.Once, "because the file should be uploaded to S3");
        mockDocumentService.Verify(d => d.AnalyzeReceipt(uploadedUrl), Times.Once, "because the receipt should be analyzed");
        mockPartyRepository.Verify(r => r.CreatePartyBillImageAsync(It.Is<CreatePartyBillImageRequest>(req => 
            req.PartyId == partyId && req.ImageUrl == uploadedUrl && req.Title == "receipt.jpg")), Times.Once, "because the bill image should be created");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnOkAndProcessFile_WhenValidTextFileUpload()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var partyId = Guid.NewGuid();
        var uploadedUrl = "https://bucket.s3.amazonaws.com/default.png";
        var receipt = new Receipt { MerchantName = "Another Store" };

        mockS3Service.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);
        mockDocumentService.Setup(d => d.AnalyzeReceipt(uploadedUrl))
            .ReturnsAsync(receipt);
        mockPartyRepository.Setup(r => r.CreatePartyBillImageAsync(It.IsAny<CreatePartyBillImageRequest>()))
            .ReturnsAsync(1);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "text file content",
            IsBase64Encoded = false,
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", partyId.ToString() }
                // No fileName provided, should default to "default.png"
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because a successful file upload should return HTTP 200 OK");
        response.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        response.Body.Should().Contain("Another Store", "because the response should contain the extracted merchant name");
        
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), "default.png", "splity"), Times.Once, "because the file should be uploaded with default filename");
        mockPartyRepository.Verify(r => r.CreatePartyBillImageAsync(It.Is<CreatePartyBillImageRequest>(req => 
            req.Title == "default.png")), Times.Once, "because the bill image should be created with default filename");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnOkWithCorsHeaders_WhenOptionsRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        Environment.SetEnvironmentVariable("ALLOWED_ORIGINS", "*");

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "OPTIONS";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because OPTIONS requests should return OK for CORS preflight");
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin", "because CORS headers must be present for preflight requests");
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods", "because allowed methods should be specified");
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers", "because allowed headers should be specified");
        response.Headers["Access-Control-Allow-Methods"].Should().Be("PUT", "because PUT method should be allowed");
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never, "because OPTIONS requests should not process any business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnMethodNotAllowed_WhenInvalidHttpMethod()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(405, "because only PUT and OPTIONS methods should be allowed");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenPartyIdIsMissing()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "some content",
            QueryStringParameters = new Dictionary<string, string>()
            // No partyId parameter
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without partyId should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("Missing or invalid partyId", "because the response should indicate what went wrong");
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never, "because invalid requests should not reach S3");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenPartyIdIsInvalidGuid()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "some content",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", "invalid-guid-format" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with invalid partyId format should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("Invalid partyId format", "because the response should indicate the specific validation error");
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never, "because invalid requests should not reach S3");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenRequestBodyIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = null, // Empty body
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", Guid.NewGuid().ToString() }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without file content should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("No file content provided", "because the response should indicate what went wrong");
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never, "because requests without content should not reach S3");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnInternalServerError_WhenS3ServiceThrowsGenericException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockS3Service.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("S3 upload failed"));

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "some content",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", Guid.NewGuid().ToString() },
                { "fileName", "test.jpg" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(500, "because generic service exceptions should result in HTTP 500 Internal Server Error");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), "test.jpg", "splity"), Times.Once, "because the S3 service should be called before the exception");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Unexpected error processing file upload"))), Times.Once, "because the error should be logged with context");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnInternalServerError_WhenDocumentServiceThrowsGenericException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        var uploadedUrl = "https://bucket.s3.amazonaws.com/test.jpg";
        
        mockS3Service.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);
        mockDocumentService.Setup(d => d.AnalyzeReceipt(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Document analysis failed"));
        mockPartyRepository.Setup(r => r.CreatePartyBillImageAsync(It.IsAny<CreatePartyBillImageRequest>()))
            .ReturnsAsync(1);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "some content",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", Guid.NewGuid().ToString() },
                { "fileName", "test.jpg" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(500, "because generic service exceptions should result in HTTP 500 Internal Server Error");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), "test.jpg", "splity"), Times.Once, "because the S3 service should be called");
        mockDocumentService.Verify(d => d.AnalyzeReceipt(uploadedUrl), Times.Once, "because the document service should be called before the exception");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Unexpected error processing file upload"))), Times.Once, "because the error should be logged with context");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenS3ServiceThrowsArgumentException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockS3Service.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Invalid file format"));

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "some content",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", Guid.NewGuid().ToString() },
                { "fileName", "test.jpg" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because ArgumentException should result in HTTP 400 Bad Request");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("Invalid request parameters", "because the response should indicate parameter validation failed");
        
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), "test.jpg", "splity"), Times.Once, "because the S3 service should be called before the exception");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Argument validation error"))), Times.Once, "because the argument error should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnUnauthorized_WhenS3ServiceThrowsUnauthorizedAccessException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockS3Service.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied to S3 bucket"));

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "some content",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", Guid.NewGuid().ToString() },
                { "fileName", "test.jpg" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(401, "because UnauthorizedAccessException should result in HTTP 401 Unauthorized");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("Unauthorized access", "because the response should indicate authorization failed");
        
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), "test.jpg", "splity"), Times.Once, "because the S3 service should be called before the exception");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Authorization error"))), Times.Once, "because the authorization error should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnTimeout_WhenDocumentServiceThrowsTimeoutException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        var uploadedUrl = "https://bucket.s3.amazonaws.com/test.jpg";
        
        mockS3Service.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);
        mockDocumentService.Setup(d => d.AnalyzeReceipt(It.IsAny<string>()))
            .ThrowsAsync(new TimeoutException("Document analysis timed out after 30 seconds"));
        mockPartyRepository.Setup(r => r.CreatePartyBillImageAsync(It.IsAny<CreatePartyBillImageRequest>()))
            .ReturnsAsync(1);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "some content",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", Guid.NewGuid().ToString() },
                { "fileName", "test.jpg" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(408, "because TimeoutException should result in HTTP 408 Request Timeout");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), "test.jpg", "splity"), Times.Once, "because the S3 service should be called");
        mockDocumentService.Verify(d => d.AnalyzeReceipt(uploadedUrl), Times.Once, "because the document service should be called before the timeout");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Operation timeout"))), Times.Once, "because the timeout error should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldProcessAnalyzeReceiptSuccessfully_WhenCompleteReceiptData()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockS3Service = new Mock<IS3BucketService>();
        var mockDocumentService = new Mock<IDocumentIntelligenceService>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var partyId = Guid.NewGuid();
        var uploadedUrl = "https://bucket.s3.amazonaws.com/detailed-receipt.jpg";
        var detailedReceipt = new Receipt
        {
            MerchantName = "Whole Foods Market",
            TransactionDate = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero),
            Items = new List<ReceiptItem>
            {
                new() { Description = "Organic Bananas", TotalItemPrice = 3.99, Quantity = 2 },
                new() { Description = "Almond Milk", TotalItemPrice = 4.99, Quantity = 1 },
                new() { Description = "Bread - Whole Wheat", TotalItemPrice = 2.49, Quantity = 1 },
                new() { Description = "Greek Yogurt", TotalItemPrice = 5.99, Quantity = 3 }
            }
        };

        mockS3Service.Setup(s => s.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);
        mockDocumentService.Setup(d => d.AnalyzeReceipt(uploadedUrl))
            .ReturnsAsync(detailedReceipt);
        mockPartyRepository.Setup(r => r.CreatePartyBillImageAsync(It.IsAny<CreatePartyBillImageRequest>()))
            .ReturnsAsync(1);

        var fileContent = "fake receipt image data";
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileContent));
        
        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = base64Content,
            IsBase64Encoded = true,
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", partyId.ToString() },
                { "fileName", "grocery-receipt.jpg" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockS3Service.Object, mockDocumentService.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because a successful file analysis should return HTTP 200 OK");
        response.Body.Should().NotBeNullOrEmpty("because the response should contain the analysis result data");
        response.Body.Should().Contain(partyId.ToString(), "because the response should contain the party ID");
        response.Body.Should().Contain(uploadedUrl, "because the response should contain the uploaded file URL");
        response.Body.Should().Contain("Whole Foods Market", "because the response should contain the extracted merchant name");
        response.Body.Should().Contain("Organic Bananas", "because the response should contain extracted item descriptions");
        response.Body.Should().Contain("3.99", "because the response should contain extracted item prices");
        response.Body.Should().Contain("2024-01-15", "because the response should contain the transaction date");
        
        // Verify all services were called correctly
        mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<byte[]>(), "grocery-receipt.jpg", "splity"), Times.Once, "because the file should be uploaded to S3");
        mockDocumentService.Verify(d => d.AnalyzeReceipt(uploadedUrl), Times.Once, "because the receipt should be analyzed");
        mockPartyRepository.Verify(r => r.CreatePartyBillImageAsync(It.Is<CreatePartyBillImageRequest>(req => 
            req.PartyId == partyId && req.ImageUrl == uploadedUrl && req.Title == "grocery-receipt.jpg")), Times.Once, "because the bill image should be created");

        // Verify no errors were logged
        mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never, "because no errors should occur during successful processing");
    }
}
