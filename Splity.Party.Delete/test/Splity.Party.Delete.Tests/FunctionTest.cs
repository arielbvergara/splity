using System.Data;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.DTOs;
using Splity.Shared.Database.Repositories.Interfaces;
using Xunit;

namespace Splity.Party.Delete.Tests;

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
    public void Constructor_ShouldCreateInstanceWithDefaultRepositories_WhenCalledWithConnectionOnly()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var function = new Function(mockConnection.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateInstanceWithProvidedRepositories_WhenCalledWithConnectionAndRepositories()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();

        // Act
        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnSuccessAndCallRepository_WhenValidDeleteRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var partyId = Guid.NewGuid();
        var existingParty = new PartyDto
        {
            PartyId = partyId,
            OwnerId = Guid.NewGuid(),
            Name = "Test Party",
            CreatedAt = DateTime.UtcNow
        };
        
        mockPartyRepository.Setup(r => r.GetPartyById(partyId))
            .ReturnsAsync(existingParty);
        mockPartyRepository.Setup(r => r.DeletePartyById(partyId))
            .ReturnsAsync(1);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", partyId.ToString() }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        res.StatusCode.Should().Be(200, "because a successful deletion should return HTTP 200 OK");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        res.Body.Should().Contain("success", "because the response should indicate success");
        res.Body.Should().Contain("true", "because the deletion was successful");
        res.Body.Should().Contain(partyId.ToString(), "because the response should contain the deleted party ID");
        
        mockPartyRepository.Verify(r => r.GetPartyById(partyId), Times.Once, "because the party existence should be checked");
        mockPartyRepository.Verify(r => r.DeletePartyById(partyId), Times.Once, "because the party should be deleted");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnOkWithCorsHeaders_WhenOptionsRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        Environment.SetEnvironmentVariable("ALLOWED_ORIGINS", "*");

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "OPTIONS";

        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because OPTIONS requests should return OK for CORS preflight");
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin", "because CORS headers must be present for preflight requests");
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods", "because allowed methods should be specified");
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers", "because allowed headers should be specified");
        mockPartyRepository.Verify(r => r.DeletePartyById(It.IsAny<Guid>()), Times.Never, "because OPTIONS requests should not process any business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnMethodNotAllowed_WhenInvalidHttpMethod()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(405, "because only DELETE and OPTIONS methods should be allowed");
        response.Body.Should().Contain("Invalid request method", "because the error should specify the invalid method");
        mockPartyRepository.Verify(r => r.DeletePartyById(It.IsAny<Guid>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenPartyIdQueryParameterIsMissing()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = null
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without partyId query parameter should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("errorMessage", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Missing partyId query parameter", "because the error message should be specific");
        mockPartyRepository.Verify(r => r.DeletePartyById(It.IsAny<Guid>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenPartyIdIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", "" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with empty partyId should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("errorMessage", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Invalid or missing partyId parameter", "because the error should specify the validation issue");
        mockPartyRepository.Verify(r => r.DeletePartyById(It.IsAny<Guid>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenPartyIdIsNotValidGuid()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", "not-a-valid-guid" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with invalid GUID format should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("errorMessage", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Invalid or missing partyId parameter", "because the error should specify the validation issue");
        mockPartyRepository.Verify(r => r.DeletePartyById(It.IsAny<Guid>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenPartyDoesNotExist()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var partyId = Guid.NewGuid();
        
        mockPartyRepository.Setup(r => r.GetPartyById(partyId))
            .ReturnsAsync((PartyDto?)null); // Party does not exist

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", partyId.ToString() }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because attempts to delete non-existent parties should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("errorMessage", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Party not found", "because the error should specify that the party was not found");
        response.Body.Should().Contain(partyId.ToString(), "because the error should include the party ID that was not found");
        
        mockPartyRepository.Verify(r => r.GetPartyById(partyId), Times.Once, "because the party existence should be checked");
        mockPartyRepository.Verify(r => r.DeletePartyById(It.IsAny<Guid>()), Times.Never, "because non-existent parties should not be deleted");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnInternalServerError_WhenRepositoryThrowsException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockPartyRepository = new Mock<IPartyRepository>();
        var mockExpenseRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var partyId = Guid.NewGuid();
        
        mockPartyRepository.Setup(r => r.GetPartyById(partyId))
            .ThrowsAsync(new Exception("Database connection failed"));

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "partyId", partyId.ToString() }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockPartyRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(500, "because repository exceptions should result in internal server error");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("errorMessage", "because the response should indicate what went wrong");
        response.Body.Should().Contain($"Error deleting party {partyId}", "because the error should specify which party deletion failed");
        
        mockPartyRepository.Verify(r => r.GetPartyById(partyId), Times.Once, "because the repository method should still be called");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains($"Error deleting party {partyId}"))), Times.Once, "because errors should be logged");
    }
}
