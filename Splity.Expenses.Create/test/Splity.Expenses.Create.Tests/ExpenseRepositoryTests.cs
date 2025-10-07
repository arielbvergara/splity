using System.Data;
using FluentAssertions;
using Moq;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Xunit;

namespace Splity.Expenses.Create.Tests;

public class ExpenseRepositoryTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WhenCalledWithValidConnection()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var repository = new ExpenseRepository(mockConnection.Object);

        // Assert
        repository.Should().NotBeNull("because the constructor should successfully create an instance with a valid connection");
    }

    [Fact]
    public async Task CreateExpensesAsync_ShouldReturnZero_WhenExpensesListIsEmpty()
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
        result.Should().Be(0, "because creating zero expenses should return zero as the count of created records");
    }
}