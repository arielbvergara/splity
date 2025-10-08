using System.Data;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using FluentAssertions;
using NSubstitute;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Repositories.Interfaces;
using Xunit;

namespace Splity.User.Get.Tests;

public class FunctionTests
{
    [Fact]
    public void Constructor_Should_CreateInstance_When_CalledWithConnection()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();

        // Act
        var function = new Function(mockConnection);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_CreateInstanceWithDefaultRepository_When_CalledWithConnectionOnly()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();

        // Act
        var function = new Function(mockConnection);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_CreateInstanceWithProvidedRepository_When_CalledWithConnectionAndRepository()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();

        // Act
        var function = new Function(mockConnection, mockRepository);

        // Assert
        function.Should().NotBeNull();
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnOkAndCallRepository_When_ValidGetRequest()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();
        var mockContext = new TestLambdaContext();
        
        var userId = Guid.NewGuid();
        var expectedUser = new UserDto
        {
            UserId = userId,
            Name = "Test User",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        
        mockRepository.GetUserByIdWithDetailsAsync(userId).Returns(expectedUser);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "userId", userId.ToString() }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection, mockRepository);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext);

        // Assert
        res.StatusCode.Should().Be(200, "because a successful retrieval should return HTTP 200 OK");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        res.Body.Should().Contain(expectedUser.UserId.ToString(), "because the response should contain the user ID");
        res.Body.Should().Contain(expectedUser.Name, "because the response should contain the user name");
        res.Body.Should().Contain(expectedUser.Email, "because the response should contain the user email");
        
        await mockRepository.Received(1).GetUserByIdWithDetailsAsync(userId);
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnNotFound_When_UserNotFound()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();
        var mockContext = new TestLambdaContext();
        
        var userId = Guid.NewGuid();
        
        mockRepository.GetUserByIdWithDetailsAsync(userId).Returns((UserDto?)null);

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "userId", userId.ToString() }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection, mockRepository);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext);

        // Assert
        res.StatusCode.Should().Be(404, "because a non-existent user should return HTTP 404 Not Found");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain error information");
        res.Body.Should().Contain("error", "because the response should indicate what went wrong");
        res.Body.Should().Contain("User not found", "because the error should specify that the user was not found");
        
        await mockRepository.Received(1).GetUserByIdWithDetailsAsync(userId);
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnOkWithCorsHeaders_When_OptionsRequest()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();
        var mockContext = new TestLambdaContext();
        
        Environment.SetEnvironmentVariable("ALLOWED_ORIGINS", "*");

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "OPTIONS";

        var function = new Function(mockConnection, mockRepository);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext);

        // Assert
        response.StatusCode.Should().Be(200, "because OPTIONS requests should return OK for CORS preflight");
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin", "because CORS headers must be present for preflight requests");
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods", "because allowed methods should be specified");
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers", "because allowed headers should be specified");
        response.Headers["Access-Control-Allow-Methods"].Should().Contain("GET", "because GET should be an allowed method");
        
        await mockRepository.DidNotReceive().GetUserByIdWithDetailsAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnMethodNotAllowed_When_InvalidHttpMethod()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();
        var mockContext = new TestLambdaContext();

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest();
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection, mockRepository);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext);

        // Assert
        response.StatusCode.Should().Be(405, "because only GET and OPTIONS methods should be allowed");
        response.Body.Should().Contain("Invalid request method", "because the error should specify the invalid method");
        
        await mockRepository.DidNotReceive().GetUserByIdWithDetailsAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnBadRequest_When_UserIdQueryParameterIsMissing()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();
        var mockContext = new TestLambdaContext();

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = null
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection, mockRepository);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without userId query parameter should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Missing userId query parameter", "because the error message should be specific");
        
        await mockRepository.DidNotReceive().GetUserByIdWithDetailsAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnBadRequest_When_UserIdIsEmpty()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();
        var mockContext = new TestLambdaContext();

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "userId", "" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection, mockRepository);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with empty userId should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Invalid or missing userId parameter", "because the error should specify the validation issue");
        
        await mockRepository.DidNotReceive().GetUserByIdWithDetailsAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task FunctionHandler_Should_ReturnBadRequest_When_UserIdIsNotValidGuid()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();
        var mockContext = new TestLambdaContext();

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "userId", "not-a-valid-guid" }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection, mockRepository);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with invalid GUID format should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("error", "because the response should indicate what went wrong");
        response.Body.Should().Contain("Invalid or missing userId parameter", "because the error should specify the validation issue");
        
        await mockRepository.DidNotReceive().GetUserByIdWithDetailsAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task FunctionHandler_Should_ThrowException_When_RepositoryThrowsException()
    {
        // Arrange
        var mockConnection = Substitute.For<IDbConnection>();
        var mockRepository = Substitute.For<IUserRepository>();
        var mockContext = new TestLambdaContext();
        
        var userId = Guid.NewGuid();
        
        mockRepository.GetUserByIdWithDetailsAsync(userId).Returns(Task.FromException<UserDto?>(new Exception("Database connection failed")));

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "userId", userId.ToString() }
            }
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "GET";

        var function = new Function(mockConnection, mockRepository);

        // Act & Assert
        // Note: The current implementation doesn't catch exceptions, so this will throw
        await Assert.ThrowsAsync<Exception>(async () =>
            await function.FunctionHandler(apiRequest, mockContext)
        );
        
        await mockRepository.Received(1).GetUserByIdWithDetailsAsync(userId);
    }
}
