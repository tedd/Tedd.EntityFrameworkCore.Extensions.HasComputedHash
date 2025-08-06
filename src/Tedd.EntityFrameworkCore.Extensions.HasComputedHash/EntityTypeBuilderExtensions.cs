using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Fluent API extensions for configuring computed hash properties.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    public static PropertyBuilder<byte[]> HasComputedHash<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        string propertyName,
        string algorithm,
        params string[] sourcePropertyNames)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entityTypeBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
        ArgumentNullException.ThrowIfNull(sourcePropertyNames);
        if (sourcePropertyNames.Length == 0)
            throw new ArgumentException("At least one source property required.", nameof(sourcePropertyNames));

        var propertyBuilder = entityTypeBuilder.Property<byte[]>(propertyName);

        // Apply custom annotations
        propertyBuilder.HasAnnotation(AnnotationKeys.IsComputedHash, true);
        propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashAlgorithm, algorithm);
        propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashSourceProperties, string.Join(",", sourcePropertyNames));

        // Configure as persisted computed column
        var sourceColumns = sourcePropertyNames.Select(c => $"[{c}]");
        var concatExpression = string.Join(" + '|' + ", sourceColumns.Select(c => $"ISNULL(CONVERT(NVARCHAR(MAX), {c}), N'')"));
        var computedSql = $"HASHBYTES('{algorithm}', {concatExpression})";

        return propertyBuilder
            //.ValueGenerated(ValueGenerated.OnAddOrUpdate)
            .ValueGeneratedOnAddOrUpdate()
            .HasColumnType(SqlHashAlgorithmExtensions.GetRecommendedSqlType(algorithm))
            .HasAnnotation(RelationalAnnotationNames.ComputedColumnSql, computedSql)
            .HasAnnotation(RelationalAnnotationNames.IsStored, true);
    }

    public static PropertyBuilder<byte[]> HasComputedHash<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, byte[]>> propertyExpression,
        string algorithm,
        Expression<Func<TEntity, object>> sourcePropertiesExpression)
        where TEntity : class
    {
        var propertyName = GetPropertyName(propertyExpression);
        var sourcePropertyNames = GetPropertyNamesFromExpression(sourcePropertiesExpression).ToArray();
        return entityTypeBuilder.HasComputedHash(propertyName, algorithm, sourcePropertyNames);
    }

    public static PropertyBuilder<byte[]> HasComputedHash<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, byte[]>> propertyExpression,
        SqlHashAlgorithm algorithm,
        Expression<Func<TEntity, object>> sourcePropertiesExpression)
        where TEntity : class
    {
        var propertyName = GetPropertyName(propertyExpression);
        var sourcePropertyNames = GetPropertyNamesFromExpression(sourcePropertiesExpression).ToArray();
        return entityTypeBuilder.HasComputedHash(propertyName, algorithm.ToString(), sourcePropertyNames);
    }

    private static string GetPropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        if (expression.Body is not MemberExpression member)
            throw new ArgumentException("Expression must reference a property.", nameof(expression));
        if (member.Member is not PropertyInfo prop)
            throw new ArgumentException("Expression must reference a property, not a field or method.", nameof(expression));
        return prop.Name;
    }

    private static IEnumerable<string> GetPropertyNamesFromExpression<TEntity>(Expression<Func<TEntity, object>> expression)
    {
        var body = expression.Body is UnaryExpression unary ? unary.Operand : expression.Body;

        if (body is MemberExpression member)
            return [member.Member.Name];

        if (body is NewExpression newExp)
            return newExp.Members?.Select(m => m.Name) ?? Enumerable.Empty<string>();

        throw new ArgumentException("Expression must be property access or anonymous type initializer.", nameof(expression));
    }
}

// Enum and extensions for SqlHashAlgorithm remain unaltered from your provision.
// Omit test/example classes unless requisite for validation.
