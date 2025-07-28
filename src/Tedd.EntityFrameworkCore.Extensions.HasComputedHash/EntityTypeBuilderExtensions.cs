using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Provides a fluent API for configuring a computed hash column.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configures a property to be a computed hash based on other properties of the entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity.</param>
    /// <param name="propertyName">The name of the hash property (e.g., e => e.MyHash).</param>
    /// <param name="algorithm">The hashing algorithm to use (e.g., "SHA2_256").</param>
    /// <param name="sourcePropertyNames">The source properties to be used in the algorithm calculation.</param>
    /// <returns>A builder for the configured property.</returns>
    public static PropertyBuilder<byte[]> HasComputedHash<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        string propertyName,
        string algorithm,
        params string[] sourcePropertyNames)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entityTypeBuilder, nameof(entityTypeBuilder));
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm, nameof(algorithm));
        ArgumentNullException.ThrowIfNull(sourcePropertyNames, nameof(sourcePropertyNames));
        if (sourcePropertyNames.Length == 0)
            throw new ArgumentException("At least one property must be provided for the computed hash.", nameof(sourcePropertyNames));

        var hashSize = SqlHashAlgorithmExtensions.GetHashSize(algorithm);

        return entityTypeBuilder.Property<byte[]>(propertyName)
            .HasAnnotation(AnnotationKeys.IsComputedHash, true)
            .HasAnnotation(AnnotationKeys.ComputedHashAlgorithm, algorithm)
            .HasAnnotation(AnnotationKeys.ComputedHashSourceProperties, string.Join(",", sourcePropertyNames))
            .HasMaxLength(hashSize);
    }

    /// <summary>
    /// Configures a property to be a computed hash based on other properties of the entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity.</param>
    /// <param name="propertyExpression">An expression to identify the hash property (e.g., e => e.MyHash).</param>
    /// <param name="algorithm">The hashing algorithm to use (e.g., "SHA2_256").</param>
    /// <param name="sourcePropertiesExpression">An expression that defines the source properties via an anonymous type (e.g., e => new { e.Prop1, e.Prop2 }).</param>
    /// <returns>A builder for the configured property.</returns>
    public static PropertyBuilder<byte[]> HasComputedHash<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, byte[]>> propertyExpression,
        string algorithm,
        Expression<Func<TEntity, object>> sourcePropertiesExpression)
        where TEntity : class
    {
        // Extract the target property name from its expression
        var propertyName = GetPropertyName(propertyExpression);

        // Extract the source property names from the anonymous type expression
        var sourcePropertyNames = GetPropertyNamesFromExpression(sourcePropertiesExpression);

        // Call the core implementation
        return entityTypeBuilder.HasComputedHash(
            propertyName,
            algorithm,
            sourcePropertyNames.ToArray());
    }

    /// <summary>
    /// Configures a property to be a computed hash based on other properties of the entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity.</param>
    /// <param name="propertyExpression">An expression to identify the hash property (e.g., e => e.MyHash).</param>
    /// <param name="algorithm">The hashing algorithm to use (e.g., "SHA2_256").</param>
    /// <param name="sourcePropertiesExpression">An expression that defines the source properties via an anonymous type (e.g., e => new { e.Prop1, e.Prop2 }).</param>
    /// <returns>A builder for the configured property.</returns>
    public static PropertyBuilder<byte[]> HasComputedHash<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, byte[]>> propertyExpression,
        SqlHashAlgorithm algorithm,
        Expression<Func<TEntity, object>> sourcePropertiesExpression)
        where TEntity : class
    {
        // Extract the target property name from its expression
        var propertyName = GetPropertyName(propertyExpression);

        // Extract the source property names from the anonymous type expression
        var sourcePropertyNames = GetPropertyNamesFromExpression(sourcePropertiesExpression);

        // Call the core implementation
        return entityTypeBuilder.HasComputedHash(
            propertyName,
            algorithm.ToString(),
            sourcePropertyNames.ToArray());
    }


    /// <summary>
    /// Extracts property names from an expression.
    /// Supports single property access (e => e.Prop) and anonymous types (e => new { ... }).
    /// </summary>
    private static IEnumerable<string> GetPropertyNamesFromExpression<TEntity>(Expression<Func<TEntity, object>> expression)
    {
        var body = expression.Body;
        // Unwrap the Convert expression if the property's return type is not 'object'.
        if (body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
        {
            body = unaryExpression.Operand;
        }

        // Handle single property access, e.g., e => e.FirstName
        if (body is MemberExpression memberExpression)
        {
            return new[] { memberExpression.Member.Name };
        }

        // Handle anonymous type, e.g., e => new { e.FirstName, e.LastName }
        if (body is NewExpression newExpression)
        {
            return newExpression.Members?.Select(m => m.Name) ?? Enumerable.Empty<string>();
        }

        throw new ArgumentException(
            "The source properties expression must be a simple property access (e.g., `e => e.Property`) or an anonymous type initializer (e.g., `e => new { e.Prop1, e.Prop2 }`).",
            nameof(expression));
    }

    /// <summary>
    /// Extracts a single property name from a MemberExpression.
    /// </summary>
    private static string GetPropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException(
                $"Expression '{expression}' refers to a method, not a property.",
                nameof(expression));
        }

        if (memberExpression.Member is not PropertyInfo propertyInfo)
        {
            throw new ArgumentException(
                $"Expression '{expression}' refers to a field, not a property.",
                nameof(expression));
        }

        return propertyInfo.Name;
    }
}