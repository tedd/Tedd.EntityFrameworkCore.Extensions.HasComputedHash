using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// An EF Core convention that discovers properties with the [ComputedHash] attribute
/// and applies the corresponding annotations to the model.
/// </summary>
public class ComputedHashConvention : IPropertyAddedConvention
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

            propertyBuilder.HasAnnotation(AnnotationKeys.IsComputedHash, true);
            propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashAlgorithm, attribute.Algorithm);
            propertyBuilder.HasAnnotation(AnnotationKeys.ComputedHashSourceProperties, string.Join(",", attribute.SourcePropertyNames));
        }
    }
}