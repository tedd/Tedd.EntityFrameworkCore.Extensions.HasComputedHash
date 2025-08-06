using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// An EF Core convention that discovers properties with the [ComputedHash] attribute
/// and configures them as persisted computed columns using standard relational annotations.
/// Compatible with EF Core 9.0.
/// </summary>
internal class ComputedHashConvention :
    IPropertyAddedConvention,
    IPropertyRemovedConvention,
    IPropertyAnnotationChangedConvention,
    IEntityTypeRemovedConvention
{
    public void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        IConventionContext<IConventionPropertyBuilder> context)
    {
        var memberInfo = propertyBuilder.Metadata.GetIdentifyingMemberInfo();
        if (memberInfo is null) return;

        var attribute = memberInfo.GetCustomAttribute<ComputedHashAttribute>();
        if (attribute == null) return;

        if (attribute.SourcePropertyNames == null || attribute.SourcePropertyNames.Count == 0)
        {
            var property = propertyBuilder.Metadata;
            throw new InvalidOperationException($"The [ComputedHash] attribute on {property.DeclaringEntityType.DisplayName()}.{property.Name} must specify at least one source property.");
        }

        if (propertyBuilder.Metadata.ClrType != typeof(byte[]))
        {
            var property = propertyBuilder.Metadata;
            throw new InvalidOperationException($"The [ComputedHash] attribute on {property.DeclaringEntityType.DisplayName()}.{property.Name} applies exclusively to byte[] properties. Encountered type: {property.ClrType.Name}.");
        }

        // Apply custom annotations for validation
        propertyBuilder.HasAnnotation(AnnotationKeys.IsComputedHash, true);
        propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashAlgorithm, attribute.Algorithm);
        propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashSourceProperties, string.Join(",", attribute.SourcePropertyNames));

        // Configure as persisted computed column using standard relational annotations
        var sourceColumns = attribute.SourcePropertyNames.Select(c => $"[{c}]");
        var concatExpression = string.Join(" + '|' + ", sourceColumns.Select(c => $"ISNULL(CONVERT(NVARCHAR(MAX), {c}), N'')"));
        var computedSql = $"HASHBYTES('{attribute.Algorithm}', {concatExpression})";

        propertyBuilder.ValueGenerated(ValueGenerated.OnAddOrUpdate);
        propertyBuilder.HasColumnType(SqlHashAlgorithmExtensions.GetRecommendedSqlType(attribute.Algorithm));
        propertyBuilder.HasAnnotation(RelationalAnnotationNames.ComputedColumnSql, computedSql);
        propertyBuilder.HasAnnotation(RelationalAnnotationNames.IsStored, true);
    }

    public void ProcessPropertyRemoved(
        IConventionTypeBaseBuilder typeBaseBuilder,
        IConventionProperty property,
        IConventionContext<IConventionProperty> context)
    {
        // Annotations auto-cleanse upon removal; no intervention required.
    }

    public void ProcessPropertyAnnotationChanged(
        IConventionPropertyBuilder propertyBuilder,
        string name,
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        if (name == AnnotationKeys.IsComputedHash)
        {
            var isComputedHash = annotation?.Value as bool?;
            var wasComputedHash = oldAnnotation?.Value as bool?;

            if (wasComputedHash == true && isComputedHash != true)
            {
                // Demote to conventional property
                propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashAlgorithm, null);
                propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashSourceProperties, null);
                propertyBuilder.HasAnnotation(RelationalAnnotationNames.ComputedColumnSql, null);
                propertyBuilder.HasAnnotation(RelationalAnnotationNames.IsStored, null);
                propertyBuilder.ValueGenerated(ValueGenerated.Never);
                propertyBuilder.HasColumnType(null); // Revert to default
            }
        }
        else if (name == AnnotationKeys.ComputedHashAlgorithm || name == AnnotationKeys.ComputedHashSourceProperties)
        {
            if ((propertyBuilder.Metadata[AnnotationKeys.IsComputedHash] as bool?) != true) return;

            var algorithm = propertyBuilder.Metadata[AnnotationKeys.ComputedHashAlgorithm] as string;
            var sourcesRaw = propertyBuilder.Metadata[AnnotationKeys.ComputedHashSourceProperties] as string;

            if (string.IsNullOrEmpty(algorithm) || string.IsNullOrEmpty(sourcesRaw)) return;

            var sourceColumns = sourcesRaw.Split(',').Select(c => $"[{c.Trim()}]");
            var concatExpression = string.Join(" + '|' + ", sourceColumns.Select(c => $"ISNULL(CONVERT(NVARCHAR(MAX), {c}), N'')"));
            var computedSql = $"HASHBYTES('{algorithm}', {concatExpression})";

            propertyBuilder.ValueGenerated(ValueGenerated.OnAddOrUpdate);
            propertyBuilder.HasColumnType(SqlHashAlgorithmExtensions.GetRecommendedSqlType(algorithm));
            propertyBuilder.HasAnnotation(RelationalAnnotationNames.ComputedColumnSql, computedSql);
            propertyBuilder.HasAnnotation(RelationalAnnotationNames.IsStored, true);
        }
    }

    public void ProcessEntityTypeRemoved(
        IConventionModelBuilder modelBuilder,
        IConventionEntityType entityType,
        IConventionContext<IConventionEntityType> context)
    {
        // Annotations auto-cleanse upon entity removal; no intervention required.
    }
}