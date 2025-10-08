using System.Data;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using Splity.Shared.Database.Models;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Models.Queries;
using Splity.Shared.Database.Repositories;
using Splity.Shared.Database.Repositories.Interfaces;
using Xunit;

namespace Splity.Expenses.Create.Tests;

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
        var mockRepository = new Mock<IExpenseRepository>();

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
        var mockRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockRepository.Setup(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()))
            .ReturnsAsync(2);

        var createExpensesRequest = new CreateExpensesRequest
        {
            PartyId = Guid.NewGuid(),
            PayerId = Guid.NewGuid(),
            Expenses = new List<CreateExpenseRequest>
            {
                new() { Description = "Test Expense 1", Amount = 10.50m },
                new() { Description = "Test Expense 2", Amount = 25.75m }
            }
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(createExpensesRequest)
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
        res.Body.Should().Contain("expenses", "because the response should contain the expenses count");
        res.Body.Should().Contain("2", "because 2 expenses should have been created");
        
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Once, "because the repository method should be called exactly once");
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Processing expenses creation"))), Times.Once, "because the processing should be logged");
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Expenses created: 2"))), Times.Once, "because the result count should be logged");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnOkWithCorsHeaders_WhenOptionsRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();
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
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Never, "because OPTIONS requests should not process any business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnMethodNotAllowed_WhenInvalidHttpMethod()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();
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
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenRequestBodyIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();
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
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenJsonIsInvalid()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();
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
        response.Body.Should().Contain("Invalid JSON", "because the error should be specific about JSON parsing failure");
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Never, "because malformed requests should not reach the repository");
    }
}
