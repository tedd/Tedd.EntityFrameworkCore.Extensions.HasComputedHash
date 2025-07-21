//using Microsoft.EntityFrameworkCore.Metadata.Conventions;
//using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

//namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

///// <summary>
///// Helper class to inject the custom convention into the EF Core pipeline.
///// </summary>
//public class CustomConventionSetBuilder(RelationalConventionSetBuilderDependencies dependencies)
//    : RelationalConventionSetBuilder(dependencies)
//{
//    public override ConventionSet CreateConventionSet()
//    {
//        var conventionSet = base.CreateConventionSet();
//        conventionSet.PropertyAddedConventions.Add(new ComputedHashConvention());
//        return conventionSet;
//    }
//}