using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Internal options extension for service registration.
/// </summary>
public class ComputedHashOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        // Register convention implementations
        services.AddSingleton<IPropertyAddedConvention, ComputedHashConvention>();
        services.AddSingleton<IPropertyRemovedConvention, ComputedHashConvention>();
        services.AddSingleton<IPropertyAnnotationChangedConvention, ComputedHashConvention>();
        services.AddSingleton<IEntityTypeRemovedConvention, ComputedHashConvention>();
    }

    public void Validate(IDbContextOptions options)
    {
        // Optional: Validate relational provider presence if requisite.
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["Tedd.Extensions:ComputedHash"] = "1";
        public override string LogFragment => "using ComputedHashes ";
    }
}