using System.Data;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using Splity.Shared.Database.Models.DTOs;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories.Interfaces;
using Xunit;

namespace Splity.Party.Create.Tests;

public class FunctionTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WhenCalledWithConnection()
    {
        // This test verifies we can create mock constructors without real DB connections
        // The parameterless constructor tries to create a real connection, so we skip testing it
        // in unit tests. Integration tests would test the real connection.
        
        // Instead, we test that our constructor dependency injection works properly
        var mockConnection = new Mock<IDbConnection>();
        var function = new Function(mockConnection.Object);
        
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateInstanceWithDefaultRepository_WhenCalledWithConnectionOnly()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var function = new Function(mockConnection.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateInstanceWithProvidedRepository_WhenCalledWithConnectionAndRepository()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();

        // Act
        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnCreatedAndCallRepository_WhenValidCreateRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var expectedParty = new PartyDto
        {
            PartyId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Party",
            CreatedAt = DateTime.UtcNow
        };
        
        mockRepository.Setup(r => r.CreateParty(It.IsAny<CreatePartyRequest>()))
            .ReturnsAsync(expectedParty);

        var createPartyRequest = new CreatePartyRequest
        {
            OwnerId = expectedParty.OwnerId,
            Name = expectedParty.Name
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        res.StatusCode.Should().Be(201, "because a successful creation should return HTTP 201 Created");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        res.Body.Should().Contain(expectedParty.PartyId.ToString(), "because the response should contain the created party ID");
        res.Body.Should().Contain(expectedParty.Name, "because the response should contain the party name");
        
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>()), Times.Once, "because the repository method should be called exactly once");
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Creating party with request body"))), Times.Once, "because the processing should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnOkWithCorsHeaders_WhenOptionsRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        Environment.SetEnvironmentVariable("ALLOWED_ORIGINS", "*");

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "OPTIONS";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because OPTIONS requests should return OK for CORS preflight");
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin", "because CORS headers must be present for preflight requests");
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods", "because allowed methods should be specified");
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers", "because allowed headers should be specified");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>()), Times.Never, "because OPTIONS requests should not process any business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnMethodNotAllowed_WhenInvalidHttpMethod()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(405, "because only POST and OPTIONS methods should be allowed");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenRequestBodyIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = null
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without body should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Request body is required", "because the error message should be specific");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenJsonIsInvalid()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "invalid json"
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because invalid JSON should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Invalid JSON format", "because the error should be specific about JSON parsing failure");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>()), Times.Never, "because malformed requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenOwnerIdIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var createPartyRequest = new CreatePartyRequest
        {
            OwnerId = Guid.Empty, // Invalid owner ID
            Name = "Test Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with empty OwnerId should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("OwnerId and Name are required", "because the error should specify the required fields");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var createPartyRequest = new CreatePartyRequest
        {
            OwnerId = Guid.NewGuid(),
            Name = "" // Empty name
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with empty Name should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("OwnerId and Name are required", "because the error should specify the required fields");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnInternalServerError_WhenRepositoryThrowsException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockRepository.Setup(r => r.CreateParty(It.IsAny<CreatePartyRequest>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var createPartyRequest = new CreatePartyRequest
        {
            OwnerId = Guid.NewGuid(),
            Name = "Test Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(500, "because repository exceptions should result in internal server error");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Internal server error", "because internal errors should not expose implementation details");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>()), Times.Once, "because the repository method should still be called");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Error creating party"))), Times.Once, "because errors should be logged");
    }
}