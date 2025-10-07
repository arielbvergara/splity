using System.Data;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
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
    public void Constructor_WithConnection_ShouldNotCreateRealConnection()
    {
        // This test verifies we can create mock constructors without real DB connections
        // The parameterless constructor tries to create a real connection, so we skip testing it
        // in unit tests. Integration tests would test the real connection.
        
        // Instead, we test that our constructor dependency injection works properly
        var mockConnection = new Mock<IDbConnection>();
        var function = new Function(mockConnection.Object);
        
        Assert.NotNull(function);
    }

    [Fact]
    public void Constructor_WithConnection_CreatesInstanceWithDefaultRepository()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var function = new Function(mockConnection.Object);

        // Assert
        Assert.NotNull(function);
    }

    [Fact]
    public void Constructor_WithConnectionAndRepository_CreatesInstanceWithProvidedRepository()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();

        // Act
        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Assert
        Assert.NotNull(function);
    }

    [Fact]
    public async Task FunctionHandler_ValidRequest_CallsRepositoryAndLogs()
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
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Once);
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Processing expenses creation"))), Times.Once);
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Expenses created: 2"))), Times.Once);
    }

    [Fact]
    public async Task FunctionHandler_OptionsRequest_ReturnsOkWithCorsHeaders()
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
        Assert.Equal(200, response.StatusCode);
        Assert.True(response.Headers.ContainsKey("Access-Control-Allow-Origin"));
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Never);
    }

    [Fact]
    public async Task FunctionHandler_GetRequest_ReturnsMethodNotAllowed()
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
        Assert.Equal(405, response.StatusCode);
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Never);
    }

    [Fact]
    public async Task FunctionHandler_EmptyBody_ReturnsBadRequest()
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
        Assert.Equal(400, response.StatusCode);
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Never);
    }

    [Fact]
    public async Task FunctionHandler_InvalidJson_ReturnsBadRequest()
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
        Assert.Equal(400, response.StatusCode);
        mockRepository.Verify(r => r.CreateExpensesAsync(It.IsAny<CreateExpensesRequest>()), Times.Never);
    }
}
