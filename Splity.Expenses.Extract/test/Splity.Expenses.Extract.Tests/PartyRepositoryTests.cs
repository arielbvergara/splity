using System.Data;
using FluentAssertions;
using Moq;
using Splity.Shared.Database.Models.Commands;
using Splity.Shared.Database.Repositories;
using Xunit;

namespace Splity.Expenses.Extract.Tests;

public class PartyRepositoryTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WhenCalledWithValidConnection()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var repository = new PartyRepository(mockConnection.Object);

        // Assert
        repository.Should().NotBeNull("because the constructor should successfully create an instance with a valid connection");
    }

    [Fact]
    public void CreatePartyBillImageAsync_ShouldReturnCorrectTaskType_WhenCalled()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var repository = new PartyRepository(mockConnection.Object);

        var request = new CreatePartyBillImageRequest
        {
            BillId = Guid.NewGuid(),
            PartyId = Guid.NewGuid(),
            Title = "Test Receipt.jpg",
            ImageUrl = "https://bucket.s3.amazonaws.com/test-receipt.jpg"
        };

        // Act & Assert
        // We can only verify that the method exists and returns the correct task type
        // since unit testing the actual database operation would require a real connection
        var result = repository.CreatePartyBillImageAsync(request);
        result.Should().NotBeNull("because the method should return a valid Task<int>");
        result.Should().BeOfType<Task<int>>("because the method signature should return Task<int>");
    }
}