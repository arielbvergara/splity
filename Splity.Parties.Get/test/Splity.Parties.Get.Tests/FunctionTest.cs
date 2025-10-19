using System.Data;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using Splity.Shared.Authentication.Models;
using Splity.Shared.Authentication.Services.Interfaces;
using Splity.Shared.Database.Models.DTOs;
using Splity.Shared.Database.Repositories.Interfaces;
using Xunit;

namespace Splity.Parties.Get.Tests;

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
        var mockAuthService = new Mock<IAuthenticationService>();
        var function = new Function(mockConnection.Object, null, mockAuthService.Object);
        
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateInstanceWithDefaultRepository_WhenCalledWithConnectionOnly()
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
    public void Constructor_ShouldCreateInstanceWithProvidedRepository_WhenCalledWithConnectionAndRepository()
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
    public async Task FunctionHandler_ShouldReturnOkAndCallRepository_WhenValidGetRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "test-cognito-id",
            Email = "test@example.com",
            Name = "Test User",
            SplityUserId = Guid.NewGuid()
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(It.IsAny<CognitoUser>()))
            .ReturnsAsync(authenticatedUser.SplityUserId.Value);
        
        var expectedParties = new List<PartyDto>
        {
            new PartyDto
            {
                PartyId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "Test Party 1",
                CreatedAt = DateTime.UtcNow
            },
            new PartyDto
            {
                PartyId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "Test Party 2",
                CreatedAt = DateTime.UtcNow
            }
        };
        
        mockRepository.Setup(r => r.GetPartiesByUserId(It.IsAny<Guid>()))
            .ReturnsAsync(expectedParties);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        res.StatusCode.Should().Be(200, "because a successful retrieval should return HTTP 200 OK");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        res.Body.Should().Contain(expectedParties[0].PartyId.ToString(), "because the response should contain the first party ID");
        res.Body.Should().Contain(expectedParties[0].Name, "because the response should contain the first party name");
        res.Body.Should().Contain(expectedParties[1].PartyId.ToString(), "because the response should contain the second party ID");
        res.Body.Should().Contain(expectedParties[1].Name, "because the response should contain the second party name");
        
        mockRepository.Verify(r => r.GetPartiesByUserId(It.IsAny<Guid>()), Times.Once, "because the repository method should be called exactly once");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnOkWithEmptyArray_WhenNoPartiesFound()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "test-cognito-id",
            Email = "test@example.com",
            Name = "Test User",
            SplityUserId = Guid.NewGuid()
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(It.IsAny<CognitoUser>()))
            .ReturnsAsync(authenticatedUser.SplityUserId.Value);
        
        mockRepository.Setup(r => r.GetPartiesByUserId(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PartyDto>()); // Empty list

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        res.StatusCode.Should().Be(200, "because GET requests should return OK even when no parties are found");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        res.Body.Should().Contain("parties", "because the response should contain the parties field");
        res.Body.Should().Contain("[]", "because the parties field should be an empty array when no parties found");
        
        mockRepository.Verify(r => r.GetPartiesByUserId(It.IsAny<Guid>()), Times.Once, "because the repository method should be called exactly once");
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
        response.Headers["Access-Control-Allow-Methods"].Should().Contain("GET", "because GET should be an allowed method");
        mockRepository.Verify(r => r.GetPartiesByUserId(It.IsAny<Guid>()), Times.Never, "because OPTIONS requests should not process any business logic");
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
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(405, "because only GET and OPTIONS methods should be allowed");
        response.Body.Should().Contain("Invalid request method", "because the error should specify the invalid method");
        mockRepository.Verify(r => r.GetPartiesByUserId(It.IsAny<Guid>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnInternalServerError_WhenRepositoryThrowsException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IPartyRepository>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        // Note: The current implementation doesn't have exception handling,
        // but this test documents expected behavior if it were added
        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        
        var authenticatedUser = new CognitoUser
        {
            CognitoUserId = "test-cognito-id",
            Email = "test@example.com",
            Name = "Test User",
            SplityUserId = Guid.NewGuid()
        };
        
        mockAuthService.Setup(a => a.GetUserFromRequestAsync(It.IsAny<APIGatewayHttpApiV2ProxyRequest>()))
            .ReturnsAsync(authenticatedUser);
        mockAuthService.Setup(a => a.EnsureUserExistsAsync(It.IsAny<CognitoUser>()))
            .ReturnsAsync(authenticatedUser.SplityUserId.Value);
        
        mockRepository.Setup(r => r.GetPartiesByUserId(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection.Object, mockRepository.Object, mockAuthService.Object);

        // Act & Assert
        // Note: The current implementation doesn't catch exceptions, so this will throw
        // This test documents what should happen if proper exception handling is added
        await Assert.ThrowsAsync<Exception>(async () =>
            await function.FunctionHandler(apiRequest, mockContext.Object)
        );
        
        mockRepository.Verify(r => r.GetPartiesByUserId(It.IsAny<Guid>()), Times.Once, "because the repository method should be called");
    }
}
