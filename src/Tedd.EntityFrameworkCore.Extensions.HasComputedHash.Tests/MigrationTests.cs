using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using System;
using System.Linq;

using Xunit;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash.Tests;

public class MigrationTests
{
    // Test entity with computed hash properties
    public class MigrationTestDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }

        // Computed hash property using attribute
        [ComputedHash(SqlHashAlgorithm.SHA2_512, nameof(Title), nameof(Content))]
        public byte[]? ContentHash { get; private set; }

        // Another computed hash property
        [ComputedHash("SHA2_256", nameof(Content), nameof(LastModified))]
        public byte[]? VersionHash { get; private set; }
    }

    // Test DbContext for migration testing
    public class MigrationTestDbContext : DbContext
    {
        public DbSet<MigrationTestDocument> Documents { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Use in-memory database for testing model configuration
                optionsBuilder.UseInMemoryDatabase("TestComputedHash")
                    .UseComputedHashes();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MigrationTestDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });
        }
    }

    [Fact]
    public void ComputedHashAttribute_ShouldBeAppliedToProperties()
    {
        // Arrange
        using var context = new MigrationTestDbContext();
        var entityType = context.Model.FindEntityType(typeof(MigrationTestDocument));

        // Act & Assert
        Assert.NotNull(entityType);

        var contentHashProperty = entityType.FindProperty(nameof(MigrationTestDocument.ContentHash));
        var versionHashProperty = entityType.FindProperty(nameof(MigrationTestDocument.VersionHash));

        Assert.NotNull(contentHashProperty);
        Assert.NotNull(versionHashProperty);

        // Verify the properties exist and have the correct types
        Assert.Equal(typeof(byte[]), contentHashProperty.ClrType);
        Assert.Equal(typeof(byte[]), versionHashProperty.ClrType);
    }

    [Fact]
    public void Model_ShouldHaveCorrectPropertyTypes()
    {
        // Arrange
        using var context = new MigrationTestDbContext();
        var entityType = context.Model.FindEntityType(typeof(MigrationTestDocument));

        // Act & Assert
        Assert.NotNull(entityType);

        var contentHashProperty = entityType.FindProperty(nameof(MigrationTestDocument.ContentHash));
        var versionHashProperty = entityType.FindProperty(nameof(MigrationTestDocument.VersionHash));

        Assert.NotNull(contentHashProperty);
        Assert.NotNull(versionHashProperty);

        // Verify property types are byte[]
        Assert.Equal(typeof(byte[]), contentHashProperty.ClrType);
        Assert.Equal(typeof(byte[]), versionHashProperty.ClrType);

        // Verify they are nullable
        Assert.True(contentHashProperty.IsNullable);
        Assert.True(versionHashProperty.IsNullable);
    }

    [Fact]
    public void Extension_ShouldBeConfigurable()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<MigrationTestDbContext>();

        // Act
        optionsBuilder.UseInMemoryDatabase("TestExtension")
            .UseComputedHashes();

        // Assert
        Assert.NotNull(optionsBuilder.Options);
    }

    [Fact]
    public void SqlHashAlgorithmExtensions_ShouldWorkWithStringValues()
    {
        // Test that the extensions work with string algorithm names
        Assert.Equal(64, SqlHashAlgorithmExtensions.GetHashSize("SHA2_512"));
        Assert.Equal(32, SqlHashAlgorithmExtensions.GetHashSize("SHA2_256"));
        Assert.Equal(16, SqlHashAlgorithmExtensions.GetHashSize("MD5"));

        Assert.Equal("BINARY(64)", SqlHashAlgorithmExtensions.GetRecommendedSqlType("SHA2_512"));
        Assert.Equal("BINARY(32)", SqlHashAlgorithmExtensions.GetRecommendedSqlType("SHA2_256"));
        Assert.Equal("BINARY(16)", SqlHashAlgorithmExtensions.GetRecommendedSqlType("MD5"));
    }

    [Fact]
    public void SqlHashAlgorithmExtensions_ShouldWorkWithEnumValues()
    {
        // Test that the extensions work with enum values
        Assert.Equal(64, SqlHashAlgorithm.SHA2_512.GetHashSize());
        Assert.Equal(32, SqlHashAlgorithm.SHA2_256.GetHashSize());
        Assert.Equal(16, SqlHashAlgorithm.MD5.GetHashSize());

        Assert.Equal("BINARY(64)", SqlHashAlgorithm.SHA2_512.GetRecommendedSqlType());
        Assert.Equal("BINARY(32)", SqlHashAlgorithm.SHA2_256.GetRecommendedSqlType());
        Assert.Equal("BINARY(16)", SqlHashAlgorithm.MD5.GetRecommendedSqlType());
    }

    [Fact]
    public void Model_ShouldSupportMultipleComputedHashProperties()
    {
        // Arrange
        using var context = new MigrationTestDbContext();
        var entityType = context.Model.FindEntityType(typeof(MigrationTestDocument));

        // Act & Assert
        Assert.NotNull(entityType);

        // Verify both computed hash properties exist
        var contentHashProperty = entityType.FindProperty(nameof(MigrationTestDocument.ContentHash));
        var versionHashProperty = entityType.FindProperty(nameof(MigrationTestDocument.VersionHash));

        Assert.NotNull(contentHashProperty);
        Assert.NotNull(versionHashProperty);

        // Verify they have different types (this is a basic test that they exist)
        Assert.Equal(typeof(byte[]), contentHashProperty.ClrType);
        Assert.Equal(typeof(byte[]), versionHashProperty.ClrType);
    }

    [Fact]
    public void ComputedHashAttribute_ShouldSupportMultipleSourceProperties()
    {
        // This test verifies that the attribute can specify multiple source properties
        var document = new MigrationTestDocument();
        var contentHashProperty = typeof(MigrationTestDocument).GetProperty(nameof(MigrationTestDocument.ContentHash));

        Assert.NotNull(contentHashProperty);

        // The attribute should be present with multiple source properties
        var attribute = contentHashProperty.GetCustomAttributes(typeof(ComputedHashAttribute), false).FirstOrDefault() as ComputedHashAttribute;
        Assert.NotNull(attribute);
        Assert.NotNull(attribute.SourcePropertyNames);
        Assert.Equal(2, attribute.SourcePropertyNames.Count);
    }

    [Fact]
    public void SqlHashAlgorithmExtensions_ShouldHandleAllSupportedAlgorithms()
    {
        // Test all supported hash methods
        var methods = new[] { SqlHashAlgorithm.MD5, SqlHashAlgorithm.SHA2_256, SqlHashAlgorithm.SHA2_512 };

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