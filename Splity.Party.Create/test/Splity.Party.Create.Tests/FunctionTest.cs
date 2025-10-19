using System.Data;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using Splity.Shared.Authentication.Models;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Database.Models.DTOs;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories.Interfaces;
using Xunit;

namespace Splity.Party.Create.Tests;

public class FunctionTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WhenCalledWithConnectionAndServices()
    {
        // This test verifies we can create mock constructors without real DB connections
        // The parameterless constructor tries to create a real connection, so we skip testing it
        // in unit tests. Integration tests would test the real connection.
        
        // Instead, we test that our constructor dependency injection works properly
        var mockConnection = new Mock<IDbConnection>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var function = new Function(mockConnection.Object, null, mockAuthService.Object);
        
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateInstanceWithDefaultRepository_WhenCalledWithConnectionAndAuthService()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockAuthService = new Mock<IAuthenticationService>();

        // Act
        var function = new Function(mockConnection.Object, null, mockAuthService.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateInstanceWithProvidedRepositoryAndAuthService_WhenAllDependenciesProvided()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();

        // Act
        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnUnauthorized_WhenAuthenticationFails()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync((CognitoUser?)null);

        var createPartyRequest = new CreatePartyRequest
        {
            Name = "Test Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(401, "because requests without valid authentication should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Authentication required", "because the error should specify authentication is needed");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Never, "because unauthenticated requests should not reach business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var unauthenticatedUser = new CognitoUser
        {
            Email = "test@example.com"
            // CognitoUserId is empty, so IsAuthenticated will be false
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(unauthenticatedUser);

        var createPartyRequest = new CreatePartyRequest
        {
            Name = "Test Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(401, "because unauthenticated users should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Authentication required", "because the error should specify authentication is needed");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Never, "because unauthenticated requests should not reach business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnUnauthorized_WhenAuthenticationServiceThrowsException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid token"));

        var createPartyRequest = new CreatePartyRequest
        {
            Name = "Test Party"
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(401, "because authentication exceptions should result in unauthorized response");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Authentication failed", "because the error should specify authentication failure");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Never, "because failed authentication should not reach business logic");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Authentication error"))), Times.Once, "because authentication errors should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnCreatedAndCallRepository_WhenValidCreateRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUserId = Guid.NewGuid();
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "cognito-user-123", // This makes IsAuthenticated = true
            Email = "test@example.com",
            SplityUserId = authenticatedUserId
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(authenticatedUser))
            .ReturnsAsync(authenticatedUserId);
        
        var expectedParty = new PartyDto
        {
            PartyId = Guid.NewGuid(),
            OwnerId = authenticatedUserId, // Should use the authenticated user's ID
            Name = "Test Party",
            CreatedAt = DateTime.UtcNow
        };
        
        mockRepository.Setup(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), authenticatedUserId))
            .ReturnsAsync(expectedParty);

        var createPartyRequest = new CreatePartyRequest
        {
            Name = expectedParty.Name
            // Note: OwnerId is not provided in request as it should come from authentication
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        res.StatusCode.Should().Be(201, "because a successful creation should return HTTP 201 Created");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        res.Body.Should().Contain(expectedParty.PartyId.ToString(), "because the response should contain the created party ID");
        res.Body.Should().Contain(expectedParty.Name, "because the response should contain the party name");
        
        mockAuthService.Verify(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()), Times.Once, "because authentication should be checked");
        mockAuthService.Verify(a => a.EnsureUserExistsAsync(authenticatedUser), Times.Once, "because user should be ensured to exist");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), authenticatedUserId), Times.Once, "because the repository method should be called with authenticated user ID");
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains($"Authenticated user: {authenticatedUser.Email}"))), Times.Once, "because authentication should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnOkWithCorsHeaders_WhenOptionsRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        Environment.SetEnvironmentVariable("ALLOWED_ORIGINS", "*");

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "OPTIONS";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(200, "because OPTIONS requests should return OK for CORS preflight");
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin", "because CORS headers must be present for preflight requests");
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods", "because allowed methods should be specified");
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers", "because allowed headers should be specified");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Never, "because OPTIONS requests should not process any business logic");
        mockAuthService.Verify(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()), Times.Never, "because OPTIONS requests should not require authentication");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnMethodNotAllowed_WhenInvalidHttpMethod()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(405, "because only POST and OPTIONS methods should be allowed");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
        mockAuthService.Verify(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()), Times.Never, "because invalid methods should not reach authentication");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenRequestBodyIsEmptyButAuthenticated()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUserId = Guid.NewGuid();
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "cognito-user-123", // This makes IsAuthenticated = true
            Email = "test@example.com",
            SplityUserId = authenticatedUserId
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(authenticatedUser))
            .ReturnsAsync(authenticatedUserId);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = null
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without body should be rejected even when authenticated");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Request body is required", "because the error message should be specific");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Never, "because invalid requests should not reach the repository");
        mockAuthService.Verify(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()), Times.Once, "because authentication should still be checked first");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenJsonIsInvalidButAuthenticated()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUserId = Guid.NewGuid();
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "cognito-user-123", // This makes IsAuthenticated = true
            Email = "test@example.com",
            SplityUserId = authenticatedUserId
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(authenticatedUser))
            .ReturnsAsync(authenticatedUserId);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "invalid json"
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because invalid JSON should be rejected even when authenticated");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Invalid JSON format", "because the error should be specific about JSON parsing failure");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Never, "because malformed requests should not reach the repository");
        mockAuthService.Verify(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()), Times.Once, "because authentication should be checked first");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenNameIsEmptyButAuthenticated()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUserId = Guid.NewGuid();
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "cognito-user-123", // This makes IsAuthenticated = true
            Email = "test@example.com",
            SplityUserId = authenticatedUserId
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(authenticatedUser))
            .ReturnsAsync(authenticatedUserId);

        var createPartyRequest = new CreatePartyRequest
        {
            Name = "" // Empty name - OwnerId will come from authentication
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with empty Name should be rejected even when authenticated");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Name is required", "because the error should specify the required field");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Never, "because invalid requests should not reach the repository");
        mockAuthService.Verify(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()), Times.Once, "because authentication should be checked first");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnInternalServerError_WhenRepositoryThrowsExceptionButAuthenticated()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUserId = Guid.NewGuid();
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "cognito-user-123", // This makes IsAuthenticated = true
            Email = "test@example.com",
            SplityUserId = authenticatedUserId
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(authenticatedUser))
            .ReturnsAsync(authenticatedUserId);
            
        mockRepository.Setup(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var createPartyRequest = new CreatePartyRequest
        {
            Name = "Test Party" // OwnerId will come from authentication
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(500, "because repository exceptions should result in internal server error");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Internal server error", "because internal errors should not expose implementation details");
        mockAuthService.Verify(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()), Times.Once, "because authentication should be checked first");
        mockRepository.Verify(r => r.CreateParty(It.IsAny<CreatePartyRequest>(), It.IsAny<Guid>()), Times.Once, "because the repository method should still be called");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Error creating party"))), Times.Once, "because errors should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldUseAuthenticatedUserIdAsOwner_WhenValidRequestWithoutOwnerId()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUserId = Guid.NewGuid();
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "cognito-user-123", // This makes IsAuthenticated = true
            Email = "test@example.com",
            SplityUserId = authenticatedUserId
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(authenticatedUser))
            .ReturnsAsync(authenticatedUserId);
        
        var expectedParty = new PartyDto
        {
            PartyId = Guid.NewGuid(),
            OwnerId = authenticatedUserId,
            Name = "Test Party",
            CreatedAt = DateTime.UtcNow
        };
        
        // Verify the repository is called with the authenticated user's ID
        mockRepository.Setup(r => r.CreateParty(It.Is<CreatePartyRequest>(req => req.Name == "Test Party"), authenticatedUserId))
            .ReturnsAsync(expectedParty);

        var createPartyRequest = new CreatePartyRequest
        {
            Name = "Test Party"
            // Note: No OwnerId property - it's been removed from the model
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createPartyRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(201, "because a successful creation should return HTTP 201 Created");
        response.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        response.Body.Should().Contain(expectedParty.PartyId.ToString(), "because the response should contain the created party ID");
        response.Body.Should().Contain(expectedParty.Name, "because the response should contain the party name");
        
        // Verify that the repository was called with the authenticated user's ID as the owner
        mockRepository.Verify(r => r.CreateParty(It.Is<CreatePartyRequest>(req => req.Name == "Test Party"), authenticatedUserId), 
            Times.Once, 
            "because the repository should be called with authenticated user ID as the owner parameter");
            
        mockAuthService.Verify(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()), Times.Once, "because authentication should be checked");
        mockAuthService.Verify(a => a.EnsureUserExistsAsync(authenticatedUser), Times.Once, "because user should be ensured to exist");
    }
}
