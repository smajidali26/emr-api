using EMR.Domain.Common;
using FluentAssertions;

namespace EMR.UnitTests.Domain.Common;

/// <summary>
/// Unit tests for BaseEntity
/// Tests cover: audit fields, soft delete, concurrency, and timestamp management
/// </summary>
public class BaseEntityTests
{
    // Concrete test implementation of BaseEntity for testing
    private class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity(string createdBy)
        {
            CreatedBy = createdBy;
        }

        // Parameterless constructor for testing
        public TestEntity() { }
    }

    #region Constructor and Initialization Tests

    [Fact]
    public void NewEntity_ShouldHaveDefaultValues()
    {
        // Act
        var entity = new TestEntity("test-user");

        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.CreatedBy.Should().Be("test-user");
        entity.UpdatedAt.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.RowVersion.Should().BeEmpty();
    }

    [Fact]
    public void NewEntity_ShouldHaveUniqueIds()
    {
        // Act
        var entity1 = new TestEntity("user1");
        var entity2 = new TestEntity("user2");

        // Assert
        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void CreatedAt_ShouldBeInUtc()
    {
        // Act
        var entity = new TestEntity("test-user");

        // Assert
        entity.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region MarkAsUpdated Tests

    [Fact]
    public void MarkAsUpdated_ShouldSetUpdatedFields()
    {
        // Arrange
        var entity = new TestEntity("creator");
        var beforeUpdate = DateTime.UtcNow;

        // Act
        entity.MarkAsUpdated("updater");

        // Assert
        entity.UpdatedBy.Should().Be("updater");
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void MarkAsUpdated_ShouldNotChangeCreatedFields()
    {
        // Arrange
        var entity = new TestEntity("creator");
        var originalCreatedBy = entity.CreatedBy;
        var originalCreatedAt = entity.CreatedAt;

        // Act
        entity.MarkAsUpdated("updater");

        // Assert
        entity.CreatedBy.Should().Be(originalCreatedBy);
        entity.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void MarkAsUpdated_CalledMultipleTimes_ShouldUpdateTimestamp()
    {
        // Arrange
        var entity = new TestEntity("creator");
        entity.MarkAsUpdated("first-updater");
        var firstUpdate = entity.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure different timestamp
        entity.MarkAsUpdated("second-updater");

        // Assert
        entity.UpdatedBy.Should().Be("second-updater");
        entity.UpdatedAt.Should().BeAfter(firstUpdate!.Value);
    }

    [Fact]
    public void MarkAsUpdated_UpdatedAtShouldBeInUtc()
    {
        // Arrange
        var entity = new TestEntity("creator");

        // Act
        entity.MarkAsUpdated("updater");

        // Assert
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region MarkAsDeleted Tests (Soft Delete)

    [Fact]
    public void MarkAsDeleted_ShouldSetDeletedFields()
    {
        // Arrange
        var entity = new TestEntity("creator");
        var beforeDelete = DateTime.UtcNow;

        // Act
        entity.MarkAsDeleted("deleter");

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be("deleter");
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeOnOrAfter(beforeDelete);
        entity.DeletedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void MarkAsDeleted_ShouldNotChangeCreatedFields()
    {
        // Arrange
        var entity = new TestEntity("creator");
        var originalCreatedBy = entity.CreatedBy;
        var originalCreatedAt = entity.CreatedAt;

        // Act
        entity.MarkAsDeleted("deleter");

        // Assert
        entity.CreatedBy.Should().Be(originalCreatedBy);
        entity.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void MarkAsDeleted_DeletedAtShouldBeInUtc()
    {
        // Arrange
        var entity = new TestEntity("creator");

        // Act
        entity.MarkAsDeleted("deleter");

        // Assert
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void MarkAsDeleted_CanBeCalledMultipleTimes()
    {
        // Arrange
        var entity = new TestEntity("creator");
        entity.MarkAsDeleted("first-deleter");
        var firstDeletedAt = entity.DeletedAt;

        // Act
        Thread.Sleep(10); // Ensure different timestamp
        entity.MarkAsDeleted("second-deleter");

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be("second-deleter");
        entity.DeletedAt.Should().BeAfter(firstDeletedAt!.Value);
    }

    #endregion

    #region Restore Tests

    [Fact]
    public void Restore_ShouldClearDeletedFields()
    {
        // Arrange
        var entity = new TestEntity("creator");
        entity.MarkAsDeleted("deleter");

        // Act
        entity.Restore();

        // Assert
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Restore_ShouldNotChangeOtherFields()
    {
        // Arrange
        var entity = new TestEntity("creator");
        entity.MarkAsUpdated("updater");
        var originalUpdatedBy = entity.UpdatedBy;
        var originalUpdatedAt = entity.UpdatedAt;
        entity.MarkAsDeleted("deleter");

        // Act
        entity.Restore();

        // Assert
        entity.CreatedBy.Should().Be("creator");
        entity.UpdatedBy.Should().Be(originalUpdatedBy);
        entity.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldHaveNoEffect()
    {
        // Arrange
        var entity = new TestEntity("creator");

        // Act
        entity.Restore();

        // Assert
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Restore_CalledMultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var entity = new TestEntity("creator");
        entity.MarkAsDeleted("deleter");

        // Act
        entity.Restore();
        entity.Restore();

        // Assert
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    #endregion

    #region Soft Delete Workflow Tests

    [Fact]
    public void SoftDeleteWorkflow_DeleteAndRestore_ShouldWork()
    {
        // Arrange
        var entity = new TestEntity("creator");

        // Act & Assert - Initial state
        entity.IsDeleted.Should().BeFalse();

        // Act & Assert - Delete
        entity.MarkAsDeleted("deleter");
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be("deleter");

        // Act & Assert - Restore
        entity.Restore();
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedBy.Should().BeNull();

        // Act & Assert - Delete again
        entity.MarkAsDeleted("another-deleter");
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be("another-deleter");
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public void RowVersion_ShouldBeInitializedAsEmpty()
    {
        // Act
        var entity = new TestEntity("creator");

        // Assert
        entity.RowVersion.Should().NotBeNull();
        entity.RowVersion.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Entity_CanBeUpdatedAfterDeletion()
    {
        // Arrange
        var entity = new TestEntity("creator");
        entity.MarkAsDeleted("deleter");

        // Act - Update after deletion
        entity.MarkAsUpdated("updater");

        // Assert
        entity.IsDeleted.Should().BeTrue(); // Still deleted
        entity.UpdatedBy.Should().Be("updater");
        entity.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MultipleOperations_ShouldMaintainAuditTrail()
    {
        // Arrange
        var entity = new TestEntity("creator");
        var createdAt = entity.CreatedAt;

        // Act
        entity.MarkAsUpdated("updater-1");
        var firstUpdate = entity.UpdatedAt;

        Thread.Sleep(10);
        entity.MarkAsUpdated("updater-2");
        var secondUpdate = entity.UpdatedAt;

        entity.MarkAsDeleted("deleter");

        // Assert
        entity.CreatedAt.Should().Be(createdAt);
        entity.CreatedBy.Should().Be("creator");
        entity.UpdatedAt.Should().Be(secondUpdate);
        entity.UpdatedBy.Should().Be("updater-2");
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be("deleter");

        // Verify chronological order
        createdAt.Should().BeBefore(firstUpdate!.Value);
        firstUpdate.Should().BeBefore(secondUpdate!.Value);
        secondUpdate.Should().BeOnOrBefore(entity.DeletedAt!.Value);
    }

    [Fact]
    public void CreatedBy_CanBeEmptyString()
    {
        // Act
        var entity = new TestEntity("");

        // Assert
        entity.CreatedBy.Should().BeEmpty();
    }

    [Fact]
    public void UpdatedBy_CanBeEmptyString()
    {
        // Arrange
        var entity = new TestEntity("creator");

        // Act
        entity.MarkAsUpdated("");

        // Assert
        entity.UpdatedBy.Should().BeEmpty();
    }

    [Fact]
    public void DeletedBy_CanBeEmptyString()
    {
        // Arrange
        var entity = new TestEntity("creator");

        // Act
        entity.MarkAsDeleted("");

        // Assert
        entity.DeletedBy.Should().BeEmpty();
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public void CompleteLifecycle_CreateUpdateDeleteRestore_ShouldWork()
    {
        // Arrange & Act - Create
        var entity = new TestEntity("initial-creator");
        var createdAt = entity.CreatedAt;

        // Assert - Initial state
        entity.CreatedBy.Should().Be("initial-creator");
        entity.UpdatedAt.Should().BeNull();
        entity.IsDeleted.Should().BeFalse();

        // Act - Update
        entity.MarkAsUpdated("first-updater");

        // Assert - After first update
        entity.UpdatedBy.Should().Be("first-updater");
        entity.UpdatedAt.Should().NotBeNull();
        entity.IsDeleted.Should().BeFalse();

        // Act - Delete
        entity.MarkAsDeleted("deleter");

        // Assert - After deletion
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be("deleter");
        entity.DeletedAt.Should().NotBeNull();

        // Act - Restore
        entity.Restore();

        // Assert - After restoration
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedBy.Should().BeNull();
        entity.DeletedAt.Should().BeNull();
        entity.CreatedBy.Should().Be("initial-creator"); // Original creator preserved
        entity.CreatedAt.Should().Be(createdAt); // Original timestamp preserved
    }

    #endregion
}
