namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Extension methods for HashMethod enum.
/// </summary>
public static class HashMethodExtensions
{
    /// <summary>
    /// Gets the byte size of the hash produced by the specified algorithm.
    /// </summary>
    /// <param name="hashMethod">The hash method.</param>
    /// <returns>The size in bytes of the hash output.</returns>
    public static int GetHashSize(this HashMethod hashMethod)
    {
        return hashMethod switch
        {
            HashMethod.MD2 => 16,
            HashMethod.MD4 => 16,
            HashMethod.MD5 => 16,
            HashMethod.SHA => 20,
            HashMethod.SHA1 => 20,
            HashMethod.SHA2_256 => 32,
            HashMethod.SHA2_512 => 64,
            _ => throw new ArgumentException($"Unknown hash method: {hashMethod}", nameof(hashMethod))
        };
    }

    /// <summary>
    /// Gets the recommended SQL Server data type for the specified hash algorithm.
    /// </summary>
    /// <param name="hashMethod">The hash method.</param>
    /// <returns>The recommended SQL Server data type (e.g., "BINARY(32)").</returns>
    public static string GetRecommendedSqlType(this HashMethod hashMethod)
    {
        return $"BINARY({hashMethod.GetHashSize()})";
    }

    /// <summary>
    /// Determines if the hash method is considered cryptographically secure.
    /// </summary>
    /// <param name="hashMethod">The hash method.</param>
    /// <returns>True if the hash method is secure, false otherwise.</returns>
    public static bool IsCryptographicallySecure(this HashMethod hashMethod)
    {
        return hashMethod switch
        {
            HashMethod.MD2 => false,
            HashMethod.MD4 => false,
            HashMethod.MD5 => false,
            HashMethod.SHA => false,
            HashMethod.SHA1 => false,
            HashMethod.SHA2_256 => true,
            HashMethod.SHA2_512 => true,
            _ => throw new ArgumentException($"Unknown hash method: {hashMethod}", nameof(hashMethod))
        };
    }

    /// <summary>
    /// Gets the hash size for a string algorithm name.
    /// </summary>
    /// <param name="algorithm">The algorithm name (e.g., "SHA2_256").</param>
    /// <returns>The size in bytes of the hash output.</returns>
    public static int GetHashSize(string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "MD2" => 16,
            "MD4" => 16,
            "MD5" => 16,
            "SHA" => 20,
            "SHA1" => 20,
            "SHA2_256" => 32,
            "SHA2_512" => 64,
            _ => throw new ArgumentException($"Unknown or unsupported hash algorithm: {algorithm}", nameof(algorithm))
        };
    }

    /// <summary>
    /// Gets the recommended SQL Server data type for a string algorithm name.
    /// </summary>
    /// <param name="algorithm">The algorithm name (e.g., "SHA2_256").</param>
    /// <returns>The recommended SQL Server data type (e.g., "BINARY(32)").</returns>
    public static string GetRecommendedSqlType(string algorithm)
    {
        return $"BINARY({GetHashSize(algorithm)})";
    }
}