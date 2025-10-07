using System.Data;
using FluentAssertions;
using Moq;
using Splity.Shared.Database.Repositories;
using Xunit;

namespace Splity.Expenses.Delete.Tests;

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
    public async Task DeleteExpensesByIdsAsync_ShouldReturnZero_WhenExpenseIdsListIsEmpty()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var repository = new ExpenseRepository(mockConnection.Object);

        var emptyExpenseIds = new List<Guid>();

        // Act
        var result = await repository.DeleteExpensesByIdsAsync(emptyExpenseIds);

        // Assert
        result.Should().Be(0, "because deleting zero expenses should return zero as the count of deleted records");
    }
}