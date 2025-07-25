using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reflection;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// An EF Core convention that discovers properties with the [ComputedHash] attribute
/// and applies the corresponding annotations to the model.
/// This implementation is compatible with EF Core 9.
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
        if (attribute != null)
        {
            if (attribute.SourcePropertyNames is null || attribute.SourcePropertyNames.Count == 0)
            {
                var property = propertyBuilder.Metadata;
                throw new InvalidOperationException($"The [ComputedHash] attribute on {property.DeclaringEntityType.DisplayName()}.{property.Name} must have at least one source property.");
            }

            // Validate that the property is of type byte[]
            if (propertyBuilder.Metadata.ClrType != typeof(byte[]))
            {
                var property = propertyBuilder.Metadata;
                throw new InvalidOperationException($"The [ComputedHash] attribute on {property.DeclaringEntityType.DisplayName()}.{property.Name} can only be applied to properties of type byte[]. Found type: {property.ClrType.Name}");
            }

            // Set the computed hash annotations
            propertyBuilder.HasAnnotation(AnnotationKeys.IsComputedHash, true);
            propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashAlgorithm, attribute.Algorithm);
            propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashSourceProperties, string.Join(",", attribute.SourcePropertyNames));

            // Set the appropriate storage type based on the hash algorithm
            var hashSize = HashMethodExtensions.GetHashSize(attribute.Algorithm);
            propertyBuilder.HasMaxLength(hashSize);
        }
    }

    // Corrected signature for EF Core 9
    public void ProcessPropertyRemoved(
        IConventionTypeBaseBuilder typeBaseBuilder,
        IConventionProperty property,
        IConventionContext<IConventionProperty> context)
    {
        // When a property is removed, we don't need to do anything special.
        // The annotations will be automatically cleaned up by EF Core.
    }

    // Corrected signature for EF Core 9
    public void ProcessPropertyAnnotationChanged(
        IConventionPropertyBuilder propertyBuilder,
        string name,
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        // Handle changes to computed hash annotations
        if (name == AnnotationKeys.IsComputedHash)
        {
            // The annotation value is now accessed via the 'Value' property.
            var isComputedHash = annotation?.Value as bool?;
            var wasComputedHash = oldAnnotation?.Value as bool?;

            // If a property is being converted from a computed hash to a regular property
            if (wasComputedHash == true && isComputedHash != true)
            {
                // Remove the computed hash annotations
                propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashAlgorithm, null);
                propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashSourceProperties, null);

                // Remove the max length constraint since it's no longer a computed hash
                propertyBuilder.HasMaxLength(null);
            }
            // If a property is being converted from a regular property to a computed hash,
            // the logic is handled by ProcessPropertyAdded or fluent API calls,
            // so no action is needed here.
        }
        // Handle changes to the algorithm annotation
        else if (name == AnnotationKeys.ComputedHashAlgorithm)
        {
            var algorithm = annotation?.Value as string;
            if (!string.IsNullOrEmpty(algorithm))
            {
                // Update the max length based on the new algorithm
                var hashSize = HashMethodExtensions.GetHashSize(algorithm);
                propertyBuilder.HasMaxLength(hashSize);
            }
        }
    }

    public void ProcessEntityTypeRemoved(
        IConventionModelBuilder modelBuilder,
        IConventionEntityType entityType,
        IConventionContext<IConventionEntityType> context)
    {
        // When an entity type is removed, we don't need to do anything special.
        // The annotations will be automatically cleaned up by EF Core.
    }
}