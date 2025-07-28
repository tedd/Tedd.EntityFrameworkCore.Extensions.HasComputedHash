//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata;
//using System;

//namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

///// <summary>
///// Simple test to verify the extension is working correctly.
///// </summary>
//public static class SimpleTest
//{
//    public class TestEntity
//    {
//        public int Id { get; set; }
//        public string Name { get; set; } = string.Empty;
//        public string Description { get; set; } = string.Empty;

//        [ComputedHash(SqlHashAlgorithm.SHA2_256, nameof(Name), nameof(Description))]
//        public byte[]? Hash { get; private set; }
//    }

//    public class TestDbContext : DbContext
//    {
//        public DbSet<TestEntity> Entities { get; set; } = null!;

//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            if (!optionsBuilder.IsConfigured)
//            {
//                optionsBuilder.UseInMemoryDatabase("TestDatabase")
//                    .UseComputedHashes();
//            }
//        }
//    }

//    public static void RunTest()
//    {
//        Console.WriteLine("=== ComputedHash Extension Test ===");
        
//        using var context = new TestDbContext();
        
//        // Check if the extension is registered
//        Console.WriteLine("1. Checking extension registration...");
//        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        
//        if (entityType == null)
//        {
//            Console.WriteLine("❌ Entity type not found!");
//            return;
//        }
        
//        Console.WriteLine("✅ Entity type found");
        
//        // Check if the hash property has the correct annotations
//        var hashProperty = entityType.FindProperty(nameof(TestEntity.Hash));
        
//        if (hashProperty == null)
//        {
//            Console.WriteLine("❌ Hash property not found!");
//            return;
//        }
        
//        Console.WriteLine("✅ Hash property found");
        
//        // Check annotations
//        var isComputedHash = hashProperty[AnnotationKeys.IsComputedHash] as bool?;
//        var algorithm = hashProperty[AnnotationKeys.ComputedHashAlgorithm] as string;
//        var sourceProperties = hashProperty[AnnotationKeys.ComputedHashSourceProperties] as string;
        
//        Console.WriteLine($"2. Checking annotations:");
//        Console.WriteLine($"   IsComputedHash: {isComputedHash}");
//        Console.WriteLine($"   Algorithm: {algorithm}");
//        Console.WriteLine($"   SourceProperties: {sourceProperties}");
        
//        if (isComputedHash == true && !string.IsNullOrEmpty(algorithm) && !string.IsNullOrEmpty(sourceProperties))
//        {
//            Console.WriteLine("✅ All annotations are correctly set");
//        }
//        else
//        {
//            Console.WriteLine("❌ Annotations are missing or incorrect");
//            return;
//        }
        
//        // Generate SQL to see what would be created
//        Console.WriteLine("3. Generating SQL...");
//        var sql = context.Database.GenerateCreateScript();
//        Console.WriteLine("Generated SQL:");
//        Console.WriteLine(sql);
        
//        if (sql.Contains("HASHBYTES") && sql.Contains("PERSISTED"))
//        {
//            Console.WriteLine("✅ SQL contains HASHBYTES and PERSISTED");
//        }
//        else
//        {
//            Console.WriteLine("❌ SQL does not contain expected computed column syntax");
//        }
        
//        Console.WriteLine("=== Test Complete ===");
//    }
//} 