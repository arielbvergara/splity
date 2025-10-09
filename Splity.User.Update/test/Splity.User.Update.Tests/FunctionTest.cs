using System.Data;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories.Interfaces;
using Xunit;

namespace Splity.User.Update.Tests;

public class FunctionTests
{
    [Fact]
    public void Constructor_Should_CreateInstance_When_CalledWithConnection()
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
    public void Constructor_Should_CreateInstanceWithDefaultRepository_When_CalledWithConnectionOnly()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var function = new Function(mockConnection.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_CreateInstanceWithProvidedRepository_When_CalledWithConnectionAndRepository()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();

        // Act
        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnOkAndCallRepository_When_ValidUpdateRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var userId = Guid.NewGuid();
        var expectedUser = new Splity.Shared.Database.Models.User
        {
            UserId = userId,
            Name = "Jane Doe Updated",
            Email = "jane.doe.updated@example.com",
            CreatedAt = DateTime.UtcNow
        };
        
        mockRepository.Setup(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(expectedUser);

        var updateUserRequest = new UpdateUserRequest
        {
            UserId = userId,
            Name = expectedUser.Name,
            Email = expectedUser.Email
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updateUserRequest)
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
        response.Body.Should().Contain(expectedUser.UserId.ToString(), "because the response should contain the updated user ID");
        response.Body.Should().Contain(expectedUser.Name, "because the response should contain the updated user name");
        response.Body.Should().Contain(expectedUser.Email, "because the response should contain the updated user email");
        
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Once, "because the repository method should be called exactly once");
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Updating user with request body"))), Times.Once, "because the processing should be logged");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnOkWithCorsHeaders_When_OptionsRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
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
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Never, "because OPTIONS requests should not process any business logic");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnMethodNotAllowed_When_InvalidHttpMethod()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
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
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnBadRequest_When_RequestBodyIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = null
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
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnBadRequest_When_JsonIsInvalid()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "invalid json"
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
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Never, "because malformed requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnBadRequest_When_UserIdIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var updateUserRequest = new UpdateUserRequest
        {
            UserId = Guid.Empty, // Invalid user ID
            Name = "John Doe"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updateUserRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with empty UserId should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("UserId is required", "because the error should specify the required field");
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnBadRequest_When_NoFieldsProvidedForUpdate()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var updateUserRequest = new UpdateUserRequest
        {
            UserId = Guid.NewGuid(),
            Name = null, // No name
            Email = null // No email
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updateUserRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with no update fields should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("At least one field (Name or Email) must be provided for update", "because the error should specify what fields are required");
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnNotFound_When_UserDoesNotExist()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockRepository.Setup(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync((Splity.Shared.Database.Models.User?)null); // User not found

        var userId = Guid.NewGuid();
        var updateUserRequest = new UpdateUserRequest
        {
            UserId = userId,
            Name = "John Doe"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updateUserRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(404, "because non-existent users should result in not found status");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain($"User with ID", "because the error message should indicate which user was not found");
        response.Body.Should().Contain($"{userId}", "because the error message should contain the user ID");
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Once, "because the repository method should be called");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnConflict_When_EmailAlreadyExists()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockRepository.Setup(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ThrowsAsync(new InvalidOperationException("User with email 'test@example.com' already exists."));

        var updateUserRequest = new UpdateUserRequest
        {
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Email = "test@example.com"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updateUserRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(409, "because duplicate emails should result in conflict status");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("already exists", "because the error message should indicate the conflict");
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Once, "because the repository method should be called");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnInternalServerError_When_RepositoryThrowsException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockRepository.Setup(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var updateUserRequest = new UpdateUserRequest
        {
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Email = "test@example.com"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updateUserRequest)
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
        mockRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Once, "because the repository method should still be called");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Error updating user"))), Times.Once, "because errors should be logged");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnOk_When_OnlyNameIsUpdated()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var userId = Guid.NewGuid();
        var expectedUser = new Splity.Shared.Database.Models.User
        {
            UserId = userId,
            Name = "Updated Name Only",
            Email = "original@example.com",
            CreatedAt = DateTime.UtcNow
        };
        
        mockRepository.Setup(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(expectedUser);

        var updateUserRequest = new UpdateUserRequest
        {
            UserId = userId,
            Name = "Updated Name Only",
            Email = null // Only updating name
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updateUserRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because updating only name should be successful");
        response.Body.Should().Contain(expectedUser.Name, "because the response should contain the updated name");
        mockRepository.Verify(r => r.UpdateUserAsync(It.Is<UpdateUserRequest>(req => 
            req.UserId == userId && 
            req.Name == "Updated Name Only" && 
            req.Email == null)), Times.Once, "because the repository should be called with correct parameters");
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnOk_When_OnlyEmailIsUpdated()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IUserRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var userId = Guid.NewGuid();
        var expectedUser = new Splity.Shared.Database.Models.User
        {
            UserId = userId,
            Name = "Original Name",
            Email = "updated@example.com",
            CreatedAt = DateTime.UtcNow
        };
        
        mockRepository.Setup(r => r.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(expectedUser);

        var updateUserRequest = new UpdateUserRequest
        {
            UserId = userId,
            Name = null, // Only updating email
            Email = "updated@example.com"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(updateUserRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "PUT";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because updating only email should be successful");
        response.Body.Should().Contain(expectedUser.Email, "because the response should contain the updated email");
        mockRepository.Verify(r => r.UpdateUserAsync(It.Is<UpdateUserRequest>(req => 
            req.UserId == userId && 
            req.Name == null && 
            req.Email == "updated@example.com")), Times.Once, "because the repository should be called with correct parameters");
    }
}
