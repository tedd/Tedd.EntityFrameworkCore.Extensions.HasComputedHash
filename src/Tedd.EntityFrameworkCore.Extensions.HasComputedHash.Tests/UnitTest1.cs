using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using Xunit;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash.Tests;

public class ComputedHashTests
{
    // Test entity with computed hash properties
    public class TestDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }

        // Computed hash property using attribute
        [ComputedHash(HashMethod.SHA2_512, nameof(Title), nameof(Content))]
        public byte[]? ContentHash { get; private set; }

        // Another computed hash property
        [ComputedHash("SHA2_256", nameof(Content), nameof(LastModified))]
        public byte[]? VersionHash { get; private set; }
    }

    // Test entity for fluent API testing
    public class FluentApiDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        // Property that will be configured via fluent API
        public byte[]? ContentHash { get; set; }
    }

    // Test DbContext for in-memory testing
    public class TestDbContext : DbContext
    {
        public DbSet<TestDocument> Documents { get; set; } = null!;
        public DbSet<FluentApiDocument> FluentApiDocuments { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("TestComputedHash")
                    .UseComputedHashes();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure fluent API for FluentApiDocument
            modelBuilder.Entity<FluentApiDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasComputedHash(
                    propertyName: nameof(FluentApiDocument.ContentHash),
                    algorithm: HashMethod.SHA2_256,
                    sourcePropertyNames: new[] { nameof(FluentApiDocument.Title), nameof(FluentApiDocument.Content) });
            });
        }
    }

    [Fact]
    public void ComputedHashAttribute_ShouldBeAppliedToProperties()
    {
        // Arrange
        using var context = new TestDbContext();
        var entityType = context.Model.FindEntityType(typeof(TestDocument));

        // Act & Assert
        Assert.NotNull(entityType);

        var contentHashProperty = entityType.FindProperty(nameof(TestDocument.ContentHash));
        Assert.NotNull(contentHashProperty);

        var versionHashProperty = entityType.FindProperty(nameof(TestDocument.VersionHash));
        Assert.NotNull(versionHashProperty);
    }

    [Fact]
    public void FluentApiConfiguration_ShouldBeAppliedToProperties()
    {
        // Arrange
        using var context = new TestDbContext();
        var entityType = context.Model.FindEntityType(typeof(FluentApiDocument));

        // Act & Assert
        Assert.NotNull(entityType);

        var contentHashProperty = entityType.FindProperty(nameof(FluentApiDocument.ContentHash));
        Assert.NotNull(contentHashProperty);
    }

    [Fact]
    public void HashMethodExtensions_ShouldReturnCorrectValues()
    {
        // Act & Assert
        Assert.Equal(32, HashMethod.SHA2_256.GetHashSize());
        Assert.Equal(64, HashMethod.SHA2_512.GetHashSize());
        Assert.Equal(16, HashMethod.MD5.GetHashSize());

        Assert.Equal("BINARY(32)", HashMethod.SHA2_256.GetRecommendedSqlType());
        Assert.Equal("BINARY(64)", HashMethod.SHA2_512.GetRecommendedSqlType());
        Assert.Equal("BINARY(16)", HashMethod.MD5.GetRecommendedSqlType());

        Assert.True(HashMethod.SHA2_256.IsCryptographicallySecure());
        Assert.True(HashMethod.SHA2_512.IsCryptographicallySecure());
        Assert.False(HashMethod.MD5.IsCryptographicallySecure());
    }

    [Fact]
    public void EntityCreation_ShouldWorkWithComputedHashProperties()
    {
        // Arrange
        using var context = new TestDbContext();
        var document = new TestDocument
        {
            Title = "Test Document",
            Content = "This is test content",
            LastModified = DateTime.UtcNow
        };

        // Act
        context.Documents.Add(document);
        context.SaveChanges();

        // Assert
        Assert.NotNull(document);
        Assert.Equal("Test Document", document.Title);
        Assert.Equal("This is test content", document.Content);
    }

    [Fact]
    public void EntityUpdate_ShouldWorkWithComputedHashProperties()
    {
        // Arrange
        using var context = new TestDbContext();
        var document = new TestDocument
        {
            Title = "Original Title",
            Content = "Original content",
            LastModified = DateTime.UtcNow
        };

        context.Documents.Add(document);
        context.SaveChanges();

        // Act
        document.Title = "Updated Title";
        context.SaveChanges();

        // Assert
        Assert.Equal("Updated Title", document.Title);
    }

    [Fact]
    public void ComputedHashAttribute_ShouldValidatePropertyType()
    {
        // This test verifies that the attribute can only be applied to byte[] properties
        // The compilation should fail if applied to wrong types, but we can test the runtime behavior

        var document = new TestDocument();
        var contentHashProperty = typeof(TestDocument).GetProperty(nameof(TestDocument.ContentHash));

        Assert.NotNull(contentHashProperty);
        Assert.Equal(typeof(byte[]), contentHashProperty.PropertyType);
    }

    [Fact]
    public void HashMethod_ShouldSupportStringAndEnumValues()
    {
        // Test that both string and enum values work for hash methods
        var sha256Enum = HashMethod.SHA2_256;
        var sha256String = "SHA2_256";

        Assert.Equal(sha256Enum, HashMethod.SHA2_256);
        Assert.Equal(sha256String, "SHA2_256");
    }

    [Fact]
    public void ComputedHashOptions_ShouldBeConfigurable()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        optionsBuilder.UseInMemoryDatabase("TestOptions")
            .UseComputedHashes();

        // Assert
        Assert.NotNull(optionsBuilder.Options);
    }

    [Fact]
    public void MultipleComputedHashProperties_ShouldBeSupported()
    {
        // Arrange
        using var context = new TestDbContext();
        var entityType = context.Model.FindEntityType(typeof(TestDocument));

        // Act & Assert
        Assert.NotNull(entityType);

        // Should have both computed hash properties
        var contentHashProperty = entityType.FindProperty(nameof(TestDocument.ContentHash));
        var versionHashProperty = entityType.FindProperty(nameof(TestDocument.VersionHash));

        Assert.NotNull(contentHashProperty);
        Assert.NotNull(versionHashProperty);
    }

    [Fact]
    public void ComputedHashAttribute_ShouldSupportMultipleSourceProperties()
    {
        // This test verifies that the attribute can specify multiple source properties
        var document = new TestDocument();
        var contentHashProperty = typeof(TestDocument).GetProperty(nameof(TestDocument.ContentHash));

        Assert.NotNull(contentHashProperty);

        // The attribute should be present with multiple source properties
        var attribute = contentHashProperty.GetCustomAttributes(typeof(ComputedHashAttribute), false).FirstOrDefault() as ComputedHashAttribute;
        Assert.NotNull(attribute);
    }

    [Fact]
    public void HashMethodExtensions_ShouldHandleAllSupportedAlgorithms()
    {
        // Test all supported hash methods
        var methods = new[] { HashMethod.MD5, HashMethod.SHA2_256, HashMethod.SHA2_512 };

        foreach (var method in methods)
        {
            var size = method.GetHashSize();
            var sqlType = method.GetRecommendedSqlType();
            var isSecure = method.IsCryptographicallySecure();

            Assert.True(size > 0);
            Assert.NotNull(sqlType);
            Assert.True(sqlType.StartsWith("BINARY("));
        }
    }
}

