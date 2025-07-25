namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Supported hash algorithms for computed hash columns.
/// </summary>
public enum HashMethod
{
    /// <summary>
    /// MD2 hashing algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Produces a 16-byte (128-bit) hash. The recommended SQL Server data type is <c>BINARY(16)</c>.
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> MD2 is cryptographically broken and considered insecure. It is slow and vulnerable to collision attacks.
    /// </para>
    /// </remarks>
    [Obsolete("Obsolete since SQL Server 2016 (13.x) and cryptographically insecure.")]
    MD2,

    /// <summary>
    /// MD4 hashing algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Produces a 16-byte (128-bit) hash. The recommended SQL Server data type is <c>BINARY(16)</c>.
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> MD4 has severe vulnerabilities and is completely insecure. Its use is strongly discouraged.
    /// </para>
    /// </remarks>
    [Obsolete("Obsolete since SQL Server 2016 (13.x) and cryptographically insecure.")]
    MD4,

    /// <summary>
    /// MD5 hashing algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Produces a 16-byte (128-bit) hash. The recommended SQL Server data type is <c>BINARY(16)</c>.
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> MD5 is vulnerable to practical collision attacks and should not be used for security purposes like password storage or digital signatures.
    /// </para>
    /// </remarks>
    [Obsolete("Obsolete since SQL Server 2016 (13.x) and cryptographically insecure.")]
    MD5,

    /// <summary>
    /// SHA-1 hashing algorithm, aliased as SHA in early SQL Server versions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Produces a 20-byte (160-bit) hash. The recommended SQL Server data type is <c>BINARY(20)</c>.
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> SHA-1 is no longer considered secure against well-funded attackers. Publicly demonstrated collision attacks exist. It has been deprecated by major vendors for security-critical functions.
    /// </para>
    /// </remarks>
    [Obsolete("Obsolete since SQL Server 2016 (13.x) and cryptographically insecure.")]
    SHA,

    /// <summary>
    /// SHA-1 hashing algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Produces a 20-byte (160-bit) hash. The recommended SQL Server data type is <c>BINARY(20)</c>.
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> SHA-1 is no longer considered secure against well-funded attackers. Publicly demonstrated collision attacks exist. It has been deprecated by major vendors for security-critical functions.
    /// </para>
    /// </remarks>
    [Obsolete("Obsolete since SQL Server 2016 (13.x) and cryptographically insecure.")]
    SHA1,

    /// <summary>
    /// SHA-2 256-bit hashing algorithm, available since SQL Server 2016 (13.x).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Produces a 32-byte (256-bit) hash. The recommended SQL Server data type is <c>BINARY(32)</c>.
    /// </para>
    /// <para>
    /// To ensure data integrity and avoid character set translation issues, the hash must be stored in a <c>BINARY(32)</c> column in SQL Server. The corresponding .NET type is <c>byte[32]</c>./// <para>
    /// </para>
    /// <para>
    /// This is a cryptographically secure hash function suitable for most applications.
    /// </para>
    /// </remarks>
    SHA2_256,

    /// <summary>
    /// SHA-2 512-bit hashing algorithm, available since SQL Server 2016 (13.x).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Produces a 64-byte (512-bit) hash. The recommended SQL Server data type is <c>BINARY(64)</c>.
    /// </para>
    /// <para>
    /// To ensure data integrity and avoid character set translation issues, the hash must be stored in a <c>BINARY(64)</c> column in SQL Server. The corresponding .NET type is <c>byte[64]</c>. This algorithm can be faster than SHA2_256 on 64-bit architectures.
    /// </para>
    /// <para>
    /// This is a cryptographically secure hash function with higher security than SHA2_256.
    /// </para>
    /// </remarks>
    SHA2_512
}