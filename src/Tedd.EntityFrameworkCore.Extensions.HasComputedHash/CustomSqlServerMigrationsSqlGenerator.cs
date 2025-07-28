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

    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        IModel? model = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
        foreach (var operation in operations)
        {
            // Handle adding computed hash columns
            if (operation is AddColumnOperation addColumnOperation &&
                addColumnOperation[AnnotationKeys.IsComputedHash] as bool? == true)
            {
                var algorithm = addColumnOperation[AnnotationKeys.ComputedHashAlgorithm] as string;
                var sourcePropertiesRaw = addColumnOperation[AnnotationKeys.ComputedHashSourceProperties] as string;

                if (!string.IsNullOrEmpty(algorithm) && !string.IsNullOrEmpty(sourcePropertiesRaw))
                {
                    // Validate that the column type is appropriate for a computed hash
                    if (addColumnOperation.ColumnType != null && 
                        !addColumnOperation.ColumnType.StartsWith("BINARY", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Computed hash columns must use BINARY storage type. Found: {addColumnOperation.ColumnType}. " +
                            $"For algorithm '{algorithm}', use {SqlHashAlgorithmExtensions.GetRecommendedSqlType(algorithm)}");
                    }

                    var sourceColumns = sourcePropertiesRaw.Split(',').Select(c => Dependencies.SqlGenerationHelper.DelimitIdentifier(c));
                    var concatExpression = string.Join(" + '|' + ", sourceColumns.Select(c => $"ISNULL(CONVERT(NVARCHAR(MAX), {c}), N'')"));

                    addColumnOperation.ComputedColumnSql = $"HASHBYTES('{algorithm}', {concatExpression}) PERSISTED";
                    
                    // Ensure the column type is set to the recommended type if not already set
                    if (addColumnOperation.ColumnType == null)
                    {
                        addColumnOperation.ColumnType = SqlHashAlgorithmExtensions.GetRecommendedSqlType(algorithm);
                    }
                }
            }
            
            // Handle dropping computed hash columns
            else if (operation is DropColumnOperation dropColumnOperation)
            {
                // Check if this is a computed hash column by looking at the model
                if (model != null)
                {
                    var entityType = model.FindEntityType(dropColumnOperation.Table);
                    if (entityType != null)
                    {
                        var property = entityType.FindProperty(dropColumnOperation.Name);
                        if (property != null && property[AnnotationKeys.IsComputedHash] as bool? == true)
                        {
                            // For computed hash columns, we need to ensure the column is dropped properly
                            // The base implementation should handle this, but we can add custom logic if needed
                        }
                    }
                }
            }
            
            // Handle altering computed hash columns
            else if (operation is AlterColumnOperation alterColumnOperation)
            {
                // Check if this is a computed hash column being modified
                if (alterColumnOperation[AnnotationKeys.IsComputedHash] as bool? == true)
                {
                    var algorithm = alterColumnOperation[AnnotationKeys.ComputedHashAlgorithm] as string;
                    var sourcePropertiesRaw = alterColumnOperation[AnnotationKeys.ComputedHashSourceProperties] as string;

                    if (!string.IsNullOrEmpty(algorithm) && !string.IsNullOrEmpty(sourcePropertiesRaw))
                    {
                        // Validate that the column type is appropriate for a computed hash
                        if (alterColumnOperation.ColumnType != null && 
                            !alterColumnOperation.ColumnType.StartsWith("BINARY", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Computed hash columns must use BINARY storage type. Found: {alterColumnOperation.ColumnType}. " +
                                $"For algorithm '{algorithm}', use {SqlHashAlgorithmExtensions.GetRecommendedSqlType(algorithm)}");
                        }

                        var sourceColumns = sourcePropertiesRaw.Split(',').Select(c => Dependencies.SqlGenerationHelper.DelimitIdentifier(c));
                        var concatExpression = string.Join(" + '|' + ", sourceColumns.Select(c => $"ISNULL(CONVERT(NVARCHAR(MAX), {c}), N'')"));

                        alterColumnOperation.ComputedColumnSql = $"HASHBYTES('{algorithm}', {concatExpression}) PERSISTED";
                        
                        // Ensure the column type is set to the recommended type if not already set
                        if (alterColumnOperation.ColumnType == null)
                        {
                            alterColumnOperation.ColumnType = SqlHashAlgorithmExtensions.GetRecommendedSqlType(algorithm);
                        }
                    }
                }
                // Check if this column is being converted from a computed hash column to a regular column
                else if (alterColumnOperation.OldColumn[AnnotationKeys.IsComputedHash] as bool? == true &&
                         alterColumnOperation[AnnotationKeys.IsComputedHash] as bool? != true)
                {
                    // Remove the computed column SQL to convert it to a regular column
                    alterColumnOperation.ComputedColumnSql = null;
                }
            }
        }

        return base.Generate(operations, model, options);
    }
}