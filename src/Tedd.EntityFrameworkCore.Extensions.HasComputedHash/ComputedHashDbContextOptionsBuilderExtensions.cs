using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Public extension method to enable computed hash functionality.
/// </summary>
public static class ComputedHashDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseComputedHashes(this DbContextOptionsBuilder optionsBuilder)
    {
        Debugger.Launch();
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
            .AddOrUpdateExtension(new ComputedHashOptionsExtension());
        return optionsBuilder;
    }
}