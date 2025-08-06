namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Defines constant keys for EF Core annotations to avoid magic strings.
/// These persist for internal validation but are supplementary to relational annotations.
/// </summary>
internal static class AnnotationKeys
{
    public const string Prefix = "Tedd.Extensions:";
    public const string IsComputedHash = Prefix + "IsComputedHash";
    public const string ComputedHashAlgorithm = Prefix + "ComputedHashAlgorithm";
    public const string ComputedHashSourceProperties = Prefix + "ComputedHashSourceProperties";
}