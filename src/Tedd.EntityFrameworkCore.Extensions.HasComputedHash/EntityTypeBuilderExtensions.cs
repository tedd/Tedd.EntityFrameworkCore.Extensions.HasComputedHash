using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Provides a fluent API for configuring a computed hash column.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configures a byte[] property as a computed hash column.
    /// </summary>
    /// <param name="entityTypeBuilder">The entity builder.</param>
    /// <param name="propertyName">The name of the hash property being configured.</param>
    /// <param name="algorithm">The hashing algorithm (e.g., 'SHA2_256').</param>
    /// <param name="sourcePropertyNames">The source properties to use for the hash.</param>
    /// <returns>A PropertyBuilder for further configuration.</returns>
    public static PropertyBuilder<byte[]> HasComputedHash(
        this EntityTypeBuilder entityTypeBuilder,
        string propertyName,
        string algorithm,
        params string[] sourcePropertyNames)
    {
        // A basic check to ensure source properties are provided.
        if (sourcePropertyNames is null || sourcePropertyNames.Length == 0)
        {
            throw new ArgumentException("At least one source property must be provided for the computed hash.", nameof(sourcePropertyNames));
        }

        // Validate the algorithm
        var hashSize = HashMethodExtensions.GetHashSize(algorithm);

        return entityTypeBuilder.Property<byte[]>(propertyName)
            .HasAnnotation(AnnotationKeys.IsComputedHash, true)
            .HasAnnotation(AnnotationKeys.ComputedHashAlgorithm, algorithm)
            .HasAnnotation(AnnotationKeys.ComputedHashSourceProperties, string.Join(",", sourcePropertyNames))
            .HasMaxLength(hashSize); // Set the appropriate storage size
    }

    /// <summary>
    /// Configures a byte[] property as a computed hash column using the HashMethod enum.
    /// </summary>
    /// <param name="entityTypeBuilder">The entity builder.</param>
    /// <param name="propertyName">The name of the hash property being configured.</param>
    /// <param name="algorithm">The hashing algorithm from the HashMethod enum.</param>
    /// <param name="sourcePropertyNames">The source properties to use for the hash.</param>
    /// <returns>A PropertyBuilder for further configuration.</returns>
    public static PropertyBuilder<byte[]> HasComputedHash(
        this EntityTypeBuilder entityTypeBuilder,
        string propertyName,
        HashMethod algorithm,
        params string[] sourcePropertyNames)
    {
        return HasComputedHash(entityTypeBuilder, propertyName, algorithm.ToString(), sourcePropertyNames);
    }
}