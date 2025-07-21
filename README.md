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

## How It Works

This library hooks into the EF Core 9 migration pipeline.

1.  **`IConvention`**: The `ComputedHashConvention` discovers properties marked with the `[ComputedHash]` attribute or configured with the `.HasComputedHash()` fluent method. It adds custom annotations to the EF model.
2.  **`IMigrationsSqlGenerator`**: A custom `CustomSqlServerMigrationsSqlGenerator` intercepts migration operations. When it sees an `AddColumnOperation` with our custom annotations, it modifies the operation to include the appropriate `HASHBYTES('ALGORITHM', ...)` SQL in the `ComputedColumnSql` property.
3.  **`IDbContextOptionsExtension`**: The `.UseComputedHashes()` method registers these custom services with EF Core's dependency injection container, making the entire process seamless.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.