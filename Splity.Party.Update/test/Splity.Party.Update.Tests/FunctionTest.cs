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

namespace Splity.Party.Update.Tests;

public class FunctionTest
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
    public async Task FunctionHandler_ShouldReturnOkAndCallRepository_WhenValidUpdateRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var partyId = Guid.NewGuid();
        var expectedParty = new PartyDto
        {
            PartyId = partyId,
            OwnerId = Guid.NewGuid(),
            Name = "Updated Party",
            CreatedAt = DateTime.UtcNow
        };
        
        mockRepository.Setup(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()))
            .ReturnsAsync(expectedParty);

        var updatePartyRequest = new UpdatePartyRequest
        {
            Name = "Updated Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updatePartyRequest),
            PathParameters = new Dictionary<string, string> { { "id", partyId.ToString() } }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because a successful update should return HTTP 200 OK");
        response.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        response.Body.Should().Contain(expectedParty.PartyId.ToString(), "because the response should contain the updated party ID");
        response.Body.Should().Contain(expectedParty.Name, "because the response should contain the updated party name");
        
        mockRepository.Verify(r => r.UpdateParty(It.Is<UpdatePartyRequest>(req => 
            req.PartyId == partyId && req.Name == "Updated Party")), Times.Once, "because the repository method should be called exactly once with correct parameters");
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains($"Updating party {partyId}"))), Times.Once, "because the processing should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnNotFound_WhenPartyDoesNotExist()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var partyId = Guid.NewGuid();
        
        mockRepository.Setup(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()))
            .ReturnsAsync((PartyDto?)null);

        var updatePartyRequest = new UpdatePartyRequest
        {
            Name = "Updated Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updatePartyRequest),
            PathParameters = new Dictionary<string, string> { { "id", partyId.ToString() } }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(404, "because updating a non-existent party should return Not Found");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Party not found", "because the error should be specific about the missing party");
        
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Once, "because the repository method should still be called");
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
        response.Headers["Access-Control-Allow-Methods"].Should().Contain("PUT", "because PUT method should be allowed for updates");
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Never, "because OPTIONS requests should not process any business logic");
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
        response.StatusCode.Should().Be(405, "because only PUT and OPTIONS methods should be allowed");
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
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
            Body = null,
            PathParameters = new Dictionary<string, string> { { "id", Guid.NewGuid().ToString() } }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without body should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Request body is required", "because the error message should be specific");
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenPartyIdIsMissing()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var updatePartyRequest = new UpdatePartyRequest
        {
            Name = "Updated Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updatePartyRequest)
            // No PathParameters provided
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without party ID should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Valid party ID is required in path", "because the error should be specific about the missing party ID");
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenPartyIdIsInvalid()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var updatePartyRequest = new UpdatePartyRequest
        {
            Name = "Updated Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updatePartyRequest),
            PathParameters = new Dictionary<string, string> { { "id", "invalid-guid" } }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with invalid party ID should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Valid party ID is required in path", "because the error should be specific about the invalid party ID");
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Never, "because invalid requests should not reach the repository");
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
            Body = "invalid json",
            PathParameters = new Dictionary<string, string> { { "id", Guid.NewGuid().ToString() } }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because invalid JSON should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Invalid JSON format", "because the error should be specific about JSON parsing failure");
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Never, "because malformed requests should not reach the repository");
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

        var updatePartyRequest = new UpdatePartyRequest
        {
            Name = "" // Empty name
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updatePartyRequest),
            PathParameters = new Dictionary<string, string> { { "id", Guid.NewGuid().ToString() } }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with empty Name should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("At least one field (Name) must be provided for update", "because the error should specify what fields are required");
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Never, "because invalid requests should not reach the repository");
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
        mockRepository.Setup(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var updatePartyRequest = new UpdatePartyRequest
        {
            Name = "Updated Party"
        };

        var partyId = Guid.NewGuid();
        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updatePartyRequest),
            PathParameters = new Dictionary<string, string> { { "id", partyId.ToString() } }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(500, "because repository exceptions should result in internal server error");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Internal server error", "because internal errors should not expose implementation details");
        mockRepository.Verify(r => r.UpdateParty(It.IsAny<UpdatePartyRequest>()), Times.Once, "because the repository method should still be called");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Error updating party"))), Times.Once, "because errors should be logged");
    }
}
