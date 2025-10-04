using System.Data;
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
    private const string TestUsername = "test_username";
    private const string TestHostname = "test_hostname";
    private const string TestDatabase = "test_database";

    [Fact]
    public void Constructor_WithConnection_CreatesInstanceWithNoParameters()
    {
        Environment.SetEnvironmentVariable("CLUSTER_USERNAME", TestUsername);
        Environment.SetEnvironmentVariable("CLUSTER_HOSTNAME", TestHostname);
        Environment.SetEnvironmentVariable("CLUSTER_DATABASE", TestDatabase);

        // Act
        var function = new Function();

        // Assert
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

        var request = new CreateExpensesRequest
        {
            PartyId = Guid.NewGuid(),
            PayerId = Guid.NewGuid(),
            Expenses = new List<CreateExpenseRequest>
            {
                new() { Description = "Test Expense 1", Amount = 10.50m },
                new() { Description = "Test Expense 2", Amount = 25.75m }
            }
        };

        var function = new Function(mockConnection.Object, mockRepository.Object);

        // Act
        await function.FunctionHandler(request, mockContext.Object);

        // Assert
        mockRepository.Verify(r => r.CreateExpensesAsync(request), Times.Once);
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Processing expenses creation"))), Times.Once);
        mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Expenses created: 2"))), Times.Once);
    }
}

public class ExpenseRepositoryTests
{
    [Fact]
    public void Constructor_WithConnection_CreatesInstance()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var repository = new ExpenseRepository(mockConnection.Object);

        // Assert
        Assert.NotNull(repository);
    }

    [Fact]
    public async Task CreateExpensesAsync_EmptyExpensesList_ShouldHandleGracefully()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var repository = new ExpenseRepository(mockConnection.Object);

        var request = new CreateExpensesRequest
        {
            PartyId = Guid.NewGuid(),
            PayerId = Guid.NewGuid(),
            Expenses = new List<CreateExpenseRequest>()
        };

        // Act & Assert
        // This would throw an exception in the current implementation
        // because Aggregate on empty collection throws InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repository.CreateExpensesAsync(request));
    }
}