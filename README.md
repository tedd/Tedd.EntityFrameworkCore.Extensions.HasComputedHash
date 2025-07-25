# Tedd.EntityFrameworkCore.Extensions.HasComputedHash

[![NuGet Version](https://img.shields.io/nuget/v/Tedd.EntityFrameworkCore.Extensions.HasComputedHash.svg)](https://www.nuget.org/packages/Tedd.EntityFrameworkCore.Extensions.HasComputedHash/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/your-github-username/your-repo/dotnet.yml?branch=main)](https://github.com/your-github-username/your-repo/actions)

An extension for Entity Framework Core 9 that adds first-class support for creating `PERSISTED` computed hash columns, powered by the native `HASHBYTES` function in SQL Server.

This allows you to automatically generate and maintain a SHA2 or other hash column based on the values of other properties in your entity, with all logic handled by the database.

---

## Prerequisites

* **.NET 9**
* **Entity Framework Core 9**
* **SQL Server Provider**: The current implementation is specific to `Microsoft.EntityFrameworkCore.SqlServer`.

---

## Installation

Install the package from the .NET CLI:

```bash
dotnet add package Tedd.EntityFrameworkCore.Extensions.HasComputedHash
```

Or from the Package Manager Console:

```powershell
Install-Package Tedd.EntityFrameworkCore.Extensions.HasComputedHash
```

---

## Setup

You must register the extension with your `DbContext`. This can be done with or without a dependency injection container.

### With Dependency Injection (Recommended)

In your `Program.cs` or service configuration, chain the `.UseComputedHashes()` method when calling `AddDbContext`.

```csharp
// Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer("...your connection string...");

    // Enable the computed hash functionality
    options.UseComputedHashes();
});
```

### Without Dependency Injection

If you are not using a DI container, you can configure the extension directly in your `DbContext`'s `OnConfiguring` method.

```csharp
public class ApplicationDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("...your connection string...");

            // Enable the computed hash functionality
            optionsBuilder.UseComputedHashes();
        }
    }
    // ...
}
```

---

## Usage

Once configured, you can define a computed hash column on your entities using either attributes or the fluent API.

### Method 1: Attribute-Based Configuration

Decorate a `byte[]` property in your entity with the `[ComputedHash]` attribute. The property must have a `private set` as its value is computed by the database.

You can specify the algorithm using either the `HashMethod` enum or a raw string.

```csharp
using Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

public class Document
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime LastModified { get; set; }

    // Example using the HashMethod enum (recommended)
    [ComputedHash(HashMethod.SHA2_512, nameof(Title), nameof(Content))]
    public byte[]? ContentHash { get; private set; }

    // Example using a raw string for the algorithm
    [ComputedHash("SHA2_256", nameof(Content), nameof(LastModified))]
    public byte[]? VersionHash { get; private set; }
}
```

### Method 2: Fluent API Configuration

Alternatively, you can configure the computed hash column in your `DbContext`'s `OnModelCreating` method using the `.HasComputedHash()` extension.

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Document> Documents { get; set; }

    // ... constructor and OnConfiguring ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure the ContentHash property
            entity.HasComputedHash(
                propertyName: nameof(Document.ContentHash),
                algorithm: HashMethod.SHA2_512, // You can use the enum here too
                sourcePropertyNames: [nameof(Document.Title), nameof(Document.Content)]);

            // Configure the VersionHash property
            entity.HasComputedHash(
                propertyName: nameof(Document.VersionHash),
                algorithm: "SHA2_256",
                sourcePropertyNames: [nameof(Document.Content), nameof(Document.LastModified)]);
        });
    }
}
```

After defining your model, simply add and apply migrations:

```bash
dotnet ef migrations add AddDocumentHashes
dotnet ef database update
```

EF Core will generate the correct SQL to create the `Document` table with `PERSISTED` computed columns.

---

## Storage Type Requirements

The extension automatically handles storage type optimization based on the chosen hash algorithm. Each algorithm produces a specific hash size that determines the optimal SQL Server data type:

### Supported Algorithms and Storage Types

| Algorithm | Hash Size | SQL Server Type | Security Status |
|-----------|-----------|-----------------|-----------------|
| MD2, MD4, MD5 | 16 bytes | `BINARY(16)` | ❌ **Insecure** |
| SHA, SHA1 | 20 bytes | `BINARY(20)` | ❌ **Insecure** |
| SHA2_256 | 32 bytes | `BINARY(32)` | ✅ **Secure** |
| SHA2_512 | 64 bytes | `BINARY(64)` | ✅ **Secure** |

### Automatic Storage Type Management

The extension automatically:

1. **Validates property types**: Only `byte[]` properties can be used for computed hash columns
2. **Sets appropriate storage size**: Automatically sets `HasMaxLength()` based on the algorithm
3. **Enforces BINARY storage**: Ensures the SQL Server column type is `BINARY(n)` where `n` matches the hash size
4. **Validates algorithm changes**: When changing algorithms, the storage size is automatically updated

### Property Type Validation

```csharp
// ✅ Correct - byte[] property
[ComputedHash(HashMethod.SHA2_256, nameof(Title))]
public byte[]? ContentHash { get; private set; }

// ❌ Incorrect - wrong property type
[ComputedHash(HashMethod.SHA2_256, nameof(Title))]
public string? ContentHash { get; private set; } // Will throw exception

// ❌ Incorrect - wrong property type
[ComputedHash(HashMethod.SHA2_256, nameof(Title))]
public int ContentHash { get; private set; } // Will throw exception
```

### Storage Size Examples

```csharp
public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    // SHA2_256 → 32 bytes → BINARY(32)
    [ComputedHash(HashMethod.SHA2_256, nameof(Title), nameof(Content))]
    public byte[]? ContentHash { get; private set; }

    // SHA2_512 → 64 bytes → BINARY(64)
    [ComputedHash(HashMethod.SHA2_512, nameof(Title), nameof(Content))]
    public byte[]? FullHash { get; private set; }

    // MD5 → 16 bytes → BINARY(16) (deprecated, insecure)
    [ComputedHash(HashMethod.MD5, nameof(Title))]
    public byte[]? LegacyHash { get; private set; }
}
```

### Security Recommendations

- **Use SHA2_256 or SHA2_512** for new applications
- **Avoid MD2, MD4, MD5, SHA, SHA1** as they are cryptographically broken
- **The extension marks insecure algorithms as obsolete** and will show compiler warnings

---

## Handling Changes and Removal

The extension fully supports modifying and removing computed hash columns through migrations. Here are the supported scenarios:

### 1. Modifying Computed Hash Properties

You can change the algorithm or source properties of a computed hash column:

```csharp
// Original
[ComputedHash(HashMethod.SHA2_512, nameof(Title), nameof(Content))]
public byte[]? ContentHash { get; private set; }

// Modified - algorithm changed
[ComputedHash(HashMethod.SHA2_256, nameof(Title), nameof(Content))]
public byte[]? ContentHash { get; private set; }

// Modified - source properties changed
[ComputedHash(HashMethod.SHA2_256, nameof(Title), nameof(Content), nameof(LastModified))]
public byte[]? ContentHash { get; private set; }
```

### 2. Removing Computed Hash Properties

You can remove the `[ComputedHash]` attribute or delete the property entirely:

```csharp
// Original
[ComputedHash(HashMethod.SHA2_512, nameof(Title), nameof(Content))]
public byte[]? ContentHash { get; private set; }

// Modified - attribute removed, property becomes regular column
public byte[]? ContentHash { get; set; }

// Or remove the property entirely
// public byte[]? ContentHash { get; private set; } // Property deleted
```

### 3. Converting Regular Columns to Computed Hash

You can convert an existing regular column to a computed hash column:

```csharp
// Original - regular column
public byte[]? ContentHash { get; set; }

// Modified - converted to computed hash
[ComputedHash(HashMethod.SHA2_512, nameof(Title), nameof(Content))]
public byte[]? ContentHash { get; private set; }
```

### 4. Using Fluent API for Changes

You can also use the fluent API to modify computed hash columns:

```csharp
// In OnModelCreating
modelBuilder.Entity<Document>(entity =>
{
    // Remove computed hash configuration
    entity.Property(e => e.ContentHash).HasAnnotation(AnnotationKeys.IsComputedHash, null);
    
    // Or modify the configuration
    entity.HasComputedHash(
        propertyName: nameof(Document.ContentHash),
        algorithm: HashMethod.SHA2_256, // Changed algorithm
        sourcePropertyNames: [nameof(Document.Title), nameof(Document.Content), nameof(Document.LastModified)]); // Added source property
});
```

When you make these changes, EF Core will generate the appropriate migration operations:

- **`AlterColumnOperation`**: For modifying existing computed hash columns
- **`DropColumnOperation`**: For removing computed hash columns entirely
- **`AddColumnOperation`**: For adding new computed hash columns

The extension automatically handles the SQL generation for all these scenarios, ensuring that the database schema stays in sync with your model changes.

---

## How It Works

This library hooks into the EF Core 9 migration pipeline.

1.  **`IConvention`**: The `ComputedHashConvention` discovers properties marked with the `[ComputedHash]` attribute or configured with the `.HasComputedHash()` fluent method. It adds custom annotations to the EF model and handles property removal and modification.
2.  **`IMigrationsSqlGenerator`**: A custom `CustomSqlServerMigrationsSqlGenerator` intercepts migration operations. When it sees operations with our custom annotations, it modifies them to include the appropriate `HASHBYTES('ALGORITHM', ...)` SQL in the `ComputedColumnSql` property.
3.  **`IDbContextOptionsExtension`**: The `.UseComputedHashes()` method registers these custom services with EF Core's dependency injection container, making the entire process seamless.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.