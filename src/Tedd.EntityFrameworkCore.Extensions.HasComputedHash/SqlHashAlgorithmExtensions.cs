#pragma warning disable CS0618 // Type or member is obsolete
namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Extension methods for HashMethod enum.
/// </summary>
public static class SqlHashAlgorithmExtensions
{
    /// <summary>
    /// Gets the byte size of the hash produced by the specified algorithm.
    /// </summary>
    /// <param name="hashMethod">The hash method.</param>
    /// <returns>The size in bytes of the hash output.</returns>
    public static int GetHashSize(this SqlHashAlgorithm hashMethod)
    {
        return hashMethod switch
        {
            SqlHashAlgorithm.SHA2_256 => 32,
            SqlHashAlgorithm.SHA2_512 => 64,
            SqlHashAlgorithm.SHA1 => 20,
            SqlHashAlgorithm.SHA => 20,
            SqlHashAlgorithm.MD5 => 16,
            SqlHashAlgorithm.MD4 => 16,
            SqlHashAlgorithm.MD2 => 16,
            _ => throw new ArgumentException($"Unknown hash method: {hashMethod}", nameof(hashMethod))
        };
    }

    /// <summary>
    /// Gets the recommended SQL Server data type for the specified hash algorithm.
    /// </summary>
    /// <param name="hashMethod">The hash method.</param>
    /// <returns>The recommended SQL Server data type (e.g., "BINARY(32)").</returns>
    public static string GetRecommendedSqlType(this SqlHashAlgorithm hashMethod)
    {
        return $"BINARY({hashMethod.GetHashSize()})";
    }

    /// <summary>
    /// Determines if the hash method is considered cryptographically secure.
    /// </summary>
    /// <param name="hashMethod">The hash method.</param>
    /// <returns>True if the hash method is secure, false otherwise.</returns>
    public static bool IsCryptographicallySecure(this SqlHashAlgorithm hashMethod)
    {
        return hashMethod switch
        {
            SqlHashAlgorithm.SHA2_256 => true,
            SqlHashAlgorithm.SHA2_512 => true,
            SqlHashAlgorithm.SHA1 => false,
            SqlHashAlgorithm.SHA => false,
            SqlHashAlgorithm.MD5 => false,
            SqlHashAlgorithm.MD4 => false,
            SqlHashAlgorithm.MD2 => false,
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
            "SHA2_256" => 32,
            "SHA2_512" => 64,
            "SHA1" => 20,
            "SHA" => 20,
            "MD5" => 16,
            "MD4" => 16,
            "MD2" => 16,
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