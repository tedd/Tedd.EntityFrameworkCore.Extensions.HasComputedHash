using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash.Tests;

/// <summary>
/// Tests that use SQL Server LocalDB to verify actual computed hash functionality.
/// These tests verify SQL generation, database schema, and runtime behavior.
/// </summary>
public class RealMigrationTests : IDisposable
{
    private readonly string _databaseName;
    private readonly string _connectionString;
    private readonly string _mdfPath;
    private readonly string _ldfPath;

    public RealMigrationTests()
    {
        _databaseName = $"TestComputedHash_{Guid.NewGuid():N}";
        _mdfPath = Path.Combine(Path.GetTempPath(), $"{_databaseName}.mdf");
        _ldfPath = Path.Combine(Path.GetTempPath(), $"{_databaseName}.ldf");
        _connectionString = $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Integrated Security=true;";
    }

    public void Dispose()
    {
        CleanupDatabase();
    }

    private void CleanupDatabase()
    {
        try
        {
            // Connect to master database to drop our test database
            var masterConnectionString = _connectionString.Replace($"Database={_databaseName};", "Database=master;");
            
            using var connection = new SqlConnection(masterConnectionString);
            connection.Open();

            // Force close all connections and drop the database
            using var dropCommand = connection.CreateCommand();
            dropCommand.CommandText = $@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = '{_databaseName}')
                BEGIN
                    -- Set database to single user mode to force close connections
                    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    
                    -- Drop the database
                    DROP DATABASE [{_databaseName}];
                END";

            dropCommand.ExecuteNonQuery();
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Clean up the physical files
        try
        {
            if (File.Exists(_mdfPath))
                File.Delete(_mdfPath);
            if (File.Exists(_ldfPath))
                File.Delete(_ldfPath);
        }
        catch
        {
            // Ignore file deletion errors
        }
    }

    // Test entity with computed hash properties
    public class RealMigrationTestDocument
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

    // Test DbContext for real migration testing
    public class RealMigrationTestDbContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<RealMigrationTestDocument> Documents { get; set; } = null!;

        public RealMigrationTestDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString)
                    .UseComputedHashes();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RealMigrationTestDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });
        }
    }

    [Fact]
    public async Task Migration_ShouldCreateComputedHashColumns()
    {
        // Arrange
        using var context = new RealMigrationTestDbContext(_connectionString);
        
        // Create the database and apply migrations
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Act - Verify the database schema
        var sql = @"
            SELECT 
                c.name AS ColumnName,
                t.name AS DataType,
                c.max_length AS MaxLength,
                cc.definition AS ComputedDefinition
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            LEFT JOIN sys.computed_columns cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
            WHERE c.object_id = OBJECT_ID('RealMigrationTestDocuments')
            ORDER BY c.column_id";

        var results = await context.Database.SqlQueryRaw<ColumnInfo>(sql).ToListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.True(results.Count > 0);

        // Verify computed hash columns exist
        var contentHashColumn = results.FirstOrDefault(r => r.ColumnName == "ContentHash");
        var versionHashColumn = results.FirstOrDefault(r => r.ColumnName == "VersionHash");

        Assert.NotNull(contentHashColumn);
        Assert.NotNull(versionHashColumn);

        // Verify they are computed columns
        Assert.NotNull(contentHashColumn.ComputedDefinition);
        Assert.NotNull(versionHashColumn.ComputedDefinition);

        // Verify they use HASHBYTES function
        Assert.Contains("HASHBYTES", contentHashColumn.ComputedDefinition);
        Assert.Contains("HASHBYTES", versionHashColumn.ComputedDefinition);

        // Verify correct data types
        Assert.Equal("binary", contentHashColumn.DataType.ToLower());
        Assert.Equal("binary", versionHashColumn.DataType.ToLower());
    }

    [Fact]
    public async Task Migration_ShouldInsertDataWithComputedHashes()
    {
        // Arrange
        using var context = new RealMigrationTestDbContext(_connectionString);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var document = new RealMigrationTestDocument
        {
            Title = "Test Document",
            Content = "This is test content",
            LastModified = DateTime.UtcNow
        };

        // Act
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotNull(document.ContentHash);
        Assert.NotNull(document.VersionHash);
        Assert.Equal(64, document.ContentHash.Length); // SHA2_512 = 64 bytes
        Assert.Equal(32, document.VersionHash.Length); // SHA2_256 = 32 bytes

        // Verify the hashes are not empty
        Assert.NotEqual(new byte[64], document.ContentHash);
        Assert.NotEqual(new byte[32], document.VersionHash);
    }

    [Fact]
    public async Task Migration_ShouldUpdateComputedHashesWhenDataChanges()
    {
        // Arrange
        using var context = new RealMigrationTestDbContext(_connectionString);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var document = new RealMigrationTestDocument
        {
            Title = "Original Title",
            Content = "Original content",
            LastModified = DateTime.UtcNow
        };

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var originalContentHash = document.ContentHash;
        var originalVersionHash = document.VersionHash;

        // Act - Update the document
        document.Title = "Updated Title";
        document.Content = "Updated content";
        await context.SaveChangesAsync();

        // Assert
        Assert.NotNull(document.ContentHash);
        Assert.NotNull(document.VersionHash);
        Assert.NotEqual(originalContentHash, document.ContentHash);
        Assert.NotEqual(originalVersionHash, document.VersionHash);
    }

    [Fact]
    public async Task Migration_ShouldGenerateCorrectSqlForComputedHashColumns()
    {
        // Arrange
        using var context = new RealMigrationTestDbContext(_connectionString);
        await context.Database.EnsureDeletedAsync();

        // Act - Generate the migration SQL
        var sql = context.Database.GenerateCreateScript();

        // Assert
        Assert.NotNull(sql);
        Assert.Contains("ContentHash", sql);
        Assert.Contains("VersionHash", sql);
        Assert.Contains("HASHBYTES", sql);
        Assert.Contains("PERSISTED", sql);
        Assert.Contains("SHA2_512", sql);
        Assert.Contains("SHA2_256", sql);
    }

    [Fact]
    public async Task Migration_ShouldHandleMultipleComputedHashColumns()
    {
        // Arrange
        using var context = new RealMigrationTestDbContext(_connectionString);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Act - Insert multiple documents
        var documents = new[]
        {
            new RealMigrationTestDocument { Title = "Doc 1", Content = "Content 1", LastModified = DateTime.UtcNow },
            new RealMigrationTestDocument { Title = "Doc 2", Content = "Content 2", LastModified = DateTime.UtcNow },
            new RealMigrationTestDocument { Title = "Doc 3", Content = "Content 3", LastModified = DateTime.UtcNow }
        };

        context.Documents.AddRange(documents);
        await context.SaveChangesAsync();

        // Assert
        foreach (var doc in documents)
        {
            Assert.NotNull(doc.ContentHash);
            Assert.NotNull(doc.VersionHash);
            Assert.Equal(64, doc.ContentHash.Length);
            Assert.Equal(32, doc.VersionHash.Length);
        }

        // Verify all hashes are unique for different content
        var contentHashes = documents.Select(d => d.ContentHash).ToList();
        var versionHashes = documents.Select(d => d.VersionHash).ToList();

        Assert.Equal(contentHashes.Count, contentHashes.Distinct().Count());
        Assert.Equal(versionHashes.Count, versionHashes.Distinct().Count());
    }

    [Fact]
    public async Task Migration_ShouldVerifyComputedHashAnnotations()
    {
        // Arrange
        using var context = new RealMigrationTestDbContext(_connectionString);
        var entityType = context.Model.FindEntityType(typeof(RealMigrationTestDocument));

        // Act & Assert
        Assert.NotNull(entityType);

        var contentHashProperty = entityType.FindProperty(nameof(RealMigrationTestDocument.ContentHash));
        var versionHashProperty = entityType.FindProperty(nameof(RealMigrationTestDocument.VersionHash));

        Assert.NotNull(contentHashProperty);
        Assert.NotNull(versionHashProperty);

        // Verify the computed hash annotations are present
        Assert.True(contentHashProperty[AnnotationKeys.IsComputedHash] as bool? == true);
        Assert.True(versionHashProperty[AnnotationKeys.IsComputedHash] as bool? == true);

        // Verify algorithm annotations
        Assert.Equal("SHA2_512", contentHashProperty[AnnotationKeys.ComputedHashAlgorithm] as string);
        Assert.Equal("SHA2_256", versionHashProperty[AnnotationKeys.ComputedHashAlgorithm] as string);

        // Verify source properties annotations
        var contentHashSources = contentHashProperty[AnnotationKeys.ComputedHashSourceProperties] as string;
        var versionHashSources = versionHashProperty[AnnotationKeys.ComputedHashSourceProperties] as string;

        Assert.NotNull(contentHashSources);
        Assert.NotNull(versionHashSources);
        Assert.Contains("Title", contentHashSources);
        Assert.Contains("Content", contentHashSources);
        Assert.Contains("Content", versionHashSources);
        Assert.Contains("LastModified", versionHashSources);
    }
}

// Helper class for SQL query results
public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public string ComputedDefinition { get; set; } = string.Empty;
} 