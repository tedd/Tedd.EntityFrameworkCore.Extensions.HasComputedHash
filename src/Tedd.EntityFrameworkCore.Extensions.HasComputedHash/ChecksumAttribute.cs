namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Marks a property as a computed hash column, generated from other properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ComputedHashAttribute : Attribute
{
    public ComputedHashAttribute(string algorithm, params string[] sourcePropertyNames)
    {
        Algorithm = algorithm;
        SourcePropertyNames = sourcePropertyNames;
    }
    public ComputedHashAttribute(HashMethod algorithm, params string[] sourcePropertyNames)
    {
        Algorithm = algorithm.ToString();
        SourcePropertyNames = sourcePropertyNames;
    }

    public string Algorithm { get; }
    public IReadOnlyList<string> SourcePropertyNames { get; }
}