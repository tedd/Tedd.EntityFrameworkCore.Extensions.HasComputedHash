//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata;
//using System;
//using System.Linq;

//namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

///// <summary>
///// Example demonstrating the computed hash functionality including removal and modification.
///// This is for documentation purposes and shows how the extension handles various scenarios.
///// </summary>
//public static class TestExample
//{
//    // Example entity with computed hash properties
//    public class Document
//    {
//        public int Id { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Content { get; set; } = string.Empty;
//        public DateTime LastModified { get; set; }

//        // Computed hash property using attribute
//        [ComputedHash(SqlHashAlgorithm.SHA2_512, nameof(Title), nameof(Content))]
//        public byte[]? ContentHash { get; private set; }

//        // Another computed hash property
//        [ComputedHash("SHA2_256", nameof(Content), nameof(LastModified))]
//        public byte[]? VersionHash { get; private set; }
//    }

//    // Example DbContext
//    public class TestDbContext : DbContext
//    {
//        public DbSet<Document> Documents { get; set; } = null!;

//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            if (!optionsBuilder.IsConfigured)
//            {
//                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestComputedHash;Trusted_Connection=true;")
//                    .UseComputedHashes();
//            }
//        }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            // Example of fluent API configuration
//            modelBuilder.Entity<Document>(entity =>
//            {
//                entity.HasKey(e => e.Id);

//                // This demonstrates how the fluent API can be used
//                // entity.HasComputedHash(
//                //     propertyName: nameof(Document.ContentHash),
//                //     algorithm: HashMethod.SHA2_512,
//                //     sourcePropertyNames: [nameof(Document.Title), nameof(Document.Content)]);
//            });
//        }
//    }

//    /// <summary>
//    /// Demonstrates the various scenarios the extension handles:
//    /// 1. Adding computed hash columns
//    /// 2. Modifying computed hash columns (changing algorithm or source properties)
//    /// 3. Removing computed hash columns
//    /// 4. Converting regular columns to computed hash columns
//    /// 5. Converting computed hash columns to regular columns
//    /// </summary>
//    public static void DemonstrateFunctionality()
//    {
//        using var context = new TestDbContext();

//        // Debug: Check if the extension is properly registered
//        Console.WriteLine("Checking extension registration...");
//        var entityType = context.Model.FindEntityType(typeof(Document));
//        if (entityType != null)
//        {
//            var contentHashProperty = entityType.FindProperty(nameof(Document.ContentHash));
//            var versionHashProperty = entityType.FindProperty(nameof(Document.VersionHash));

//            if (contentHashProperty != null)
//            {
//                var isComputedHash = contentHashProperty[AnnotationKeys.IsComputedHash] as bool?;
//                var algorithm = contentHashProperty[AnnotationKeys.ComputedHashAlgorithm] as string;
//                var sourceProperties = contentHashProperty[AnnotationKeys.ComputedHashSourceProperties] as string;

//                Console.WriteLine($"ContentHash property:");
//                Console.WriteLine($"  IsComputedHash: {isComputedHash}");
//                Console.WriteLine($"  Algorithm: {algorithm}");
//                Console.WriteLine($"  SourceProperties: {sourceProperties}");
//            }

//            if (versionHashProperty != null)
//            {
//                var isComputedHash = versionHashProperty[AnnotationKeys.IsComputedHash] as bool?;
//                var algorithm = versionHashProperty[AnnotationKeys.ComputedHashAlgorithm] as string;
//                var sourceProperties = versionHashProperty[AnnotationKeys.ComputedHashSourceProperties] as string;

//                Console.WriteLine($"VersionHash property:");
//                Console.WriteLine($"  IsComputedHash: {isComputedHash}");
//                Console.WriteLine($"  Algorithm: {algorithm}");
//                Console.WriteLine($"  SourceProperties: {sourceProperties}");
//            }
//        }

//        // Generate the migration SQL to see what would be created
//        var createScript = context.Database.GenerateCreateScript();
//        Console.WriteLine("\nGenerated SQL:");
//        Console.WriteLine(createScript);

//        // Scenario 1: Add a new document with computed hash properties
//        var document = new Document
//        {
//            Title = "Test Document",
//            Content = "This is test content",
//            LastModified = DateTime.UtcNow
//        };

//        context.Documents.Add(document);
//        context.SaveChanges();

//        // The ContentHash and VersionHash will be automatically computed by SQL Server
//        // based on the HASHBYTES function

//        Console.WriteLine("Document created with computed hash properties");
//        Console.WriteLine($"ContentHash: {Convert.ToBase64String(document.ContentHash ?? Array.Empty<byte>())}");
//        Console.WriteLine($"VersionHash: {Convert.ToBase64String(document.VersionHash ?? Array.Empty<byte>())}");

//        // Scenario 2: Update the document - the computed hash will be recalculated
//        document.Title = "Updated Test Document";
//        context.SaveChanges();

//        Console.WriteLine("Document updated - computed hash recalculated");
//        Console.WriteLine($"ContentHash: {Convert.ToBase64String(document.ContentHash ?? Array.Empty<byte>())}");
//    }
//}

//// Example of how to modify the model to demonstrate removal/modification scenarios
//public class DocumentModificationExample
//{
//    // Original entity with computed hash
//    public class DocumentV1
//    {
//        public int Id { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Content { get; set; } = string.Empty;

//        [ComputedHash(SqlHashAlgorithm.SHA2_512, nameof(Title), nameof(Content))]
//        public byte[]? ContentHash { get; private set; }
//    }

//    // Modified entity - algorithm changed
//    public class DocumentV2
//    {
//        public int Id { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Content { get; set; } = string.Empty;

//        [ComputedHash(SqlHashAlgorithm.SHA2_256, nameof(Title), nameof(Content))] // Changed from SHA2_512 to SHA2_256
//        public byte[]? ContentHash { get; private set; }
//    }

//    // Modified entity - source properties changed
//    public class DocumentV3
//    {
//        public int Id { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Content { get; set; } = string.Empty;
//        public DateTime LastModified { get; set; }

//        [ComputedHash(SqlHashAlgorithm.SHA2_256, nameof(Title), nameof(Content), nameof(LastModified))] // Added LastModified
//        public byte[]? ContentHash { get; private set; }
//    }

//    // Modified entity - computed hash removed
//    public class DocumentV4
//    {
//        public int Id { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Content { get; set; } = string.Empty;
//        public DateTime LastModified { get; set; }

//        // ContentHash property removed entirely
//    }

//    // Modified entity - regular column converted to computed hash
//    public class DocumentV5
//    {
//        public int Id { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Content { get; set; } = string.Empty;
//        public DateTime LastModified { get; set; }

//        // Regular property that will be converted to computed hash
//        public byte[]? ContentHash { get; set; } // Note: no private set, will be converted via fluent API
//    }
//}

//// Examples demonstrating storage type validation and different hash algorithms
//public class StorageTypeExamples
//{
//    // Example showing different hash algorithms and their storage requirements
//    public class HashAlgorithmExamples
//    {
//        public int Id { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Content { get; set; } = string.Empty;

//        // SHA2_256 → 32 bytes → BINARY(32)
//        [ComputedHash(SqlHashAlgorithm.SHA2_256, nameof(Title), nameof(Content))]
//        public byte[]? Sha256Hash { get; private set; }

//        // SHA2_512 → 64 bytes → BINARY(64)
//        [ComputedHash(SqlHashAlgorithm.SHA2_512, nameof(Title), nameof(Content))]
//        public byte[]? Sha512Hash { get; private set; }

//        // MD5 → 16 bytes → BINARY(16) (deprecated, insecure)
//        [ComputedHash(SqlHashAlgorithm.MD5, nameof(Title))]
//        public byte[]? Md5Hash { get; private set; }
//    }

//    // Example showing property type validation
//    public class PropertyTypeValidationExamples
//    {
//        public int Id { get; set; }
//        public string Title { get; set; } = string.Empty;

//        // ✅ Correct - byte[] property
//        [ComputedHash(SqlHashAlgorithm.SHA2_256, nameof(Title))]
//        public byte[]? ValidHash { get; private set; }

//        // ❌ These would cause compilation errors or runtime exceptions:

//        // [ComputedHash(HashMethod.SHA2_256, nameof(Title))]
//        // public string? InvalidHash { get; private set; } // Wrong type

//        // [ComputedHash(HashMethod.SHA2_256, nameof(Title))]
//        // public int InvalidHash { get; private set; } // Wrong type

//        // [ComputedHash(HashMethod.SHA2_256, nameof(Title))]
//        // public byte[]? InvalidHash { get; set; } // No private set
//    }

//    /// <summary>
//    /// Demonstrates how to use the HashMethodExtensions to get storage information.
//    /// </summary>
//    public static void DemonstrateStorageTypeInfo()
//    {
//        Console.WriteLine("Hash Algorithm Storage Requirements:");
//        Console.WriteLine($"SHA2_256: {SqlHashAlgorithm.SHA2_256.GetHashSize()} bytes → {SqlHashAlgorithm.SHA2_256.GetRecommendedSqlType()}");
//        Console.WriteLine($"SHA2_512: {SqlHashAlgorithm.SHA2_512.GetHashSize()} bytes → {SqlHashAlgorithm.SHA2_512.GetRecommendedSqlType()}");
//        Console.WriteLine($"MD5: {SqlHashAlgorithm.MD5.GetHashSize()} bytes → {SqlHashAlgorithm.MD5.GetRecommendedSqlType()}");
        
//        Console.WriteLine("\nSecurity Status:");
//        Console.WriteLine($"SHA2_256: {(SqlHashAlgorithm.SHA2_256.IsCryptographicallySecure() ? "✅ Secure" : "❌ Insecure")}");
//        Console.WriteLine($"SHA2_512: {(SqlHashAlgorithm.SHA2_512.IsCryptographicallySecure() ? "✅ Secure" : "❌ Insecure")}");
//        Console.WriteLine($"MD5: {(SqlHashAlgorithm.MD5.IsCryptographicallySecure() ? "✅ Secure" : "❌ Insecure")}");
//    }
//} 