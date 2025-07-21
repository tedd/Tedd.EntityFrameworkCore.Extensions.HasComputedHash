using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// The public, user-facing extension method to enable the functionality.
/// </summary>
public static class ComputedHashDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseComputedHashes(this DbContextOptionsBuilder optionsBuilder)
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
            .AddOrUpdateExtension(new ComputedHashOptionsExtension());

        return optionsBuilder;
    }
}