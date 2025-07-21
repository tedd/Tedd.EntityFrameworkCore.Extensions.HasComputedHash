using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

/// <summary>
/// Custom SQL generator for SQL Server that translates computed hash annotations into T-SQL.
/// </summary>
public class CustomSqlServerMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
{
    public CustomSqlServerMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        Microsoft.EntityFrameworkCore.Update.ICommandBatchPreparer commandBatchPreparer)
        : base(dependencies, commandBatchPreparer)
    {
    }

    // The override of the Generate method remains the same as the last correction.
    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        IModel? model = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
        foreach (var operation in operations)
        {
            if (operation is AddColumnOperation addColumnOperation &&
                addColumnOperation[AnnotationKeys.IsComputedHash] as bool? == true)
            {
                var algorithm = addColumnOperation[AnnotationKeys.ComputedHashAlgorithm] as string;
                var sourcePropertiesRaw = addColumnOperation[AnnotationKeys.ComputedHashSourceProperties] as string;

                if (!string.IsNullOrEmpty(algorithm) && !string.IsNullOrEmpty(sourcePropertiesRaw))
                {
                    var sourceColumns = sourcePropertiesRaw.Split(',').Select(c => Dependencies.SqlGenerationHelper.DelimitIdentifier(c));
                    var concatExpression = string.Join(" + '|' + ", sourceColumns.Select(c => $"ISNULL(CONVERT(NVARCHAR(MAX), {c}), N'')"));

                    addColumnOperation.ComputedColumnSql = $"HASHBYTES('{algorithm}', {concatExpression}) PERSISTED";
                }
            }
        }

        return base.Generate(operations, model, options);
    }
}