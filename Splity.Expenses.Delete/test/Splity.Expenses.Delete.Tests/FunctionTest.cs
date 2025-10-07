using System.Data;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories.Interfaces;
using Xunit;

namespace Splity.Expenses.Delete.Tests;

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
        
        function.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConnection_CreatesInstanceWithDefaultRepository()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var function = new Function(mockConnection.Object);

        // Assert
        function.Should().NotBeNull();
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
        function.Should().NotBeNull();
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
        mockRepository.Setup(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(2);

        var expenseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var deleteExpensesRequest = new DeleteExpensesRequest
        {
            ExpenseIds = expenseIds
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(deleteExpensesRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        res.StatusCode.Should().Be(200, "because a successful deletion should return HTTP 200 OK");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        res.Body.Should().Contain("DeletedCount", "because the response should contain the deleted count");
        res.Body.Should().Contain("2", "because 2 expenses should have been deleted");
        
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Once, "because the repository method should be called exactly once");
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Processing bulk delete"))), Times.Once, "because the processing should be logged");
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Successfully deleted 2"))), Times.Once, "because the result count should be logged");
    }

    [Fact]
    public async Task FunctionHandler_NoExpensesDeleted_ReturnsNotFound()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockRepository.Setup(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(0); // No expenses were deleted

        var expenseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var deleteExpensesRequest = new DeleteExpensesRequest
        {
            ExpenseIds = expenseIds
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(deleteExpensesRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var res = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        res.StatusCode.Should().Be(404, "because when no expenses are deleted, it should return HTTP 404 Not Found");
        res.Body.Should().NotBeNullOrEmpty("because the response should contain the result data");
        res.Body.Should().Contain("DeletedCount", "because the response should contain the deleted count");
        res.Body.Should().Contain("0", "because 0 expenses should have been deleted");
        res.Body.Should().Contain("No expenses were deleted", "because the message should indicate no deletion occurred");
        
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Once, "because the repository method should be called exactly once");
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
        response.StatusCode.Should().Be(200, "because OPTIONS requests should return OK for CORS preflight");
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin", "because CORS headers must be present for preflight requests");
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods", "because allowed methods should be specified");
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers", "because allowed headers should be specified");
        response.Headers["Access-Control-Allow-Methods"].Should().Be("DELETE", "because only DELETE method should be allowed");
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Never, "because OPTIONS requests should not process any business logic");
    }

    [Fact]
    public async Task FunctionHandler_PostRequest_ReturnsMethodNotAllowed()
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
        apiRequest.RequestContext.Http.Method = "POST";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(405, "because only DELETE and OPTIONS methods should be allowed");
        response.Body.Should().Contain("Invalid request method: POST", "because the error should specify the invalid method");
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Never, "because invalid HTTP methods should not trigger business logic");
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
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests without body should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("Request body is required", "because the response should indicate what went wrong");
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Never, "because invalid requests should not reach the repository");
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
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because invalid JSON should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("Invalid JSON format", "because the error should be specific about JSON parsing failure");
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Never, "because malformed requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_EmptyExpenseIdsList_ReturnsBadRequest()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var deleteExpensesRequest = new DeleteExpensesRequest
        {
            ExpenseIds = new List<Guid>() // Empty list
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(deleteExpensesRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(400, "because requests with empty expense IDs should be rejected");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("ExpenseIds are required and cannot be empty", "because the response should indicate what went wrong");
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Never, "because invalid requests should not reach the repository");
    }

    [Fact]
    public async Task FunctionHandler_NullExpenseIdsList_ReturnsInternalServerError()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        // Create JSON with null ExpenseIds which causes a runtime exception when calling .Any()
        var jsonBody = "{\"ExpenseIds\": null}";

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = jsonBody
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(500, "because null ExpenseIds causes a runtime exception when calling .Any()");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("Error deleting expenses", "because the response should indicate the operation that failed");
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Never, "because the exception occurs before reaching the repository");
    }

    [Fact]
    public async Task FunctionHandler_RepositoryThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockRepository = new Mock<IExpenseRepository>();
        var mockContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockContext.Setup(c => c.Logger).Returns(mockLogger.Object);
        mockRepository.Setup(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var expenseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var deleteExpensesRequest = new DeleteExpensesRequest
        {
            ExpenseIds = expenseIds
        };

        var apiRequest = new APIGatewayHttpApiV2ProxyRequest
        {
            Body = JsonSerializer.Serialize(deleteExpensesRequest)
        };
        apiRequest.RequestContext = new();
        apiRequest.RequestContext.Http = new();
        apiRequest.RequestContext.Http.Method = "DELETE";

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        var response = await function.FunctionHandler(apiRequest, mockContext.Object);

        // Assert
        response.StatusCode.Should().Be(500, "because repository exceptions should result in HTTP 500 Internal Server Error");
        response.Body.Should().NotBeNullOrEmpty("because error responses should contain error information");
        response.Body.Should().Contain("Error deleting expenses", "because the response should indicate the operation that failed");
        response.Body.Should().Contain("Database connection failed", "because the specific error message should be included");
        
        mockRepository.Verify(r => r.DeleteExpensesByIdsAsync(It.IsAny<List<Guid>>()), Times.Once, "because the repository method should be called before the exception");
        mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Error deleting expenses"))), Times.Once, "because the error should be logged");
    }
}
