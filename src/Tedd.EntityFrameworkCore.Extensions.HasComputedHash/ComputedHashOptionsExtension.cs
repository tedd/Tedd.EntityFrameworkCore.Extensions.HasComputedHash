using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// The internal options extension that carries configuration and registers services.
/// </summary>
public class ComputedHashOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        // Replace the default services with our custom implementations.
        services.AddSingleton<IMigrationsSqlGenerator, CustomSqlServerMigrationsSqlGenerator>();

        // Register the convention for each interface it implements
        services.AddSingleton<IPropertyAddedConvention, ComputedHashConvention>();
        services.AddSingleton<IPropertyRemovedConvention, ComputedHashConvention>();
        services.AddSingleton<IPropertyAnnotationChangedConvention, ComputedHashConvention>();
        services.AddSingleton<IEntityTypeRemovedConvention, ComputedHashConvention>();
    }

    public void Validate(IDbContextOptions options)
    {
        // Can be used to validate that the extension is used correctly,
        // e.g., that a relational provider is also configured.
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["Tedd.Extensions:ComputedHash"] = "1";
        public override string LogFragment => "using ComputedHashes";
    }
}