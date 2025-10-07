using System.Data;
using Moq;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Xunit;

namespace Splity.Expenses.Create.Tests;

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
    public async Task CreateExpensesAsync_EmptyExpensesList_ShouldReturnZero()
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

        // Act
        var result = await repository.CreateExpensesAsync(request);

        // Assert
        Assert.Equal(0, result);
    }
}