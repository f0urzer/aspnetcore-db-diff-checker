using DbDiffChecker.Core.Models.Difference;
using DbDiffChecker.Core.Models.Info;
using DbDiffChecker.Core.Models.Response;
using DbDiffChecker.Core.Services.Interface;
using Npgsql;

namespace DbDiffChecker.Core.Services
{
    public class PostgresqlService : IDatabaseService
    {
        public async Task<List<SchemaInfo>> GetDatabaseSchemaInfo(DbConnectionInfo connectionInfo)
        {
            var result = new List<SchemaInfo>();

            using (var connection = new NpgsqlConnection(connectionInfo.ConnectionString))
            {
                await connection.OpenAsync();

                // Get schemas
                var schemas = await GetSchemas(connection);

                foreach (var schema in schemas)
                {
                    var schemaInfo = new SchemaInfo
                    {
                        Name = schema.Name,
                        Owner = schema.Owner
                    };

                    // Get tables for the schema
                    var tables = await GetTables(connection, schema.Name);
                    
                    // Get views for the schema
                    schemaInfo.Views = await GetViews(connection, schema.Name);

                    foreach (var table in tables)
                    {
                        var tableInfo = new TableInfo
                        {
                            SchemaName = schema.Name,
                            TableName = table
                        };

                        // Get columns for the table
                        tableInfo.Columns = await GetColumns(connection, schema.Name, table);

                        // Get constraints for the table
                        tableInfo.Constraints = await GetConstraints(connection, schema.Name, table);

                        // Get indexes for the table
                        tableInfo.Indexes = await GetIndexes(connection, schema.Name, table);

                        schemaInfo.Tables.Add(tableInfo);
                    }

                    result.Add(schemaInfo);
                }
            }

            return result;
        }

        public Task<ComparisonResult> CompareDatabases(List<SchemaInfo> sourceSchemas, List<SchemaInfo> targetSchemas)
        {
            var result = new ComparisonResult();

            // Compare schemas
            CompareSchemas(sourceSchemas, targetSchemas, result);

            // Compare tables
            CompareTables(sourceSchemas, targetSchemas, result);

            // Compare views
            CompareViews(sourceSchemas, targetSchemas, result);

            // Compare columns
            CompareColumns(sourceSchemas, targetSchemas, result);

            // Compare constraints
            CompareConstraints(sourceSchemas, targetSchemas, result);

            // Compare indexes
            CompareIndexes(sourceSchemas, targetSchemas, result);

            return Task.FromResult(result);
        }

        private async Task<List<(string Name, string Owner)>> GetSchemas(NpgsqlConnection connection)
        {
            var result = new List<(string Name, string Owner)>();

            using (var cmd = new NpgsqlCommand(@"
                SELECT schema_name, schema_owner
                FROM information_schema.schemata
                WHERE schema_name NOT IN ('pg_catalog', 'information_schema')
                ORDER BY schema_name", connection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add((reader.GetString(0), reader.GetString(1)));
                    }
                }
            }

            return result;
        }

        private async Task<List<string>> GetTables(NpgsqlConnection connection, string schemaName)
        {
            var result = new List<string>();

            using (var cmd = new NpgsqlCommand(@"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = @schema_name
                AND table_type = 'BASE TABLE'
                ORDER BY table_name", connection))
            {
                cmd.Parameters.AddWithValue("schema_name", schemaName);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(reader.GetString(0));
                    }
                }
            }

            return result;
        }

        private async Task<List<ViewInfo>> GetViews(NpgsqlConnection connection, string schemaName)
        {
            var result = new List<ViewInfo>();

            using (var cmd = new NpgsqlCommand(@"
                    SELECT n.nspname AS schema_name, c.relname AS view_name, pg_get_viewdef(c.oid, true) AS definition
                    FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE c.relkind = 'v' AND n.nspname = @schema_name;", connection))
            {
                cmd.Parameters.AddWithValue("schema_name", schemaName);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new ViewInfo
                        {
                            SchemaName = reader.GetString(0),
                            ViewName = reader.GetString(1),
                            Definition = reader.GetString(2)
                        });
                    }
                }
            }

            return result;
        }

        private async Task<List<ColumnInfo>> GetColumns(NpgsqlConnection connection, string schemaName, string tableName)
        {
            var result = new List<ColumnInfo>();

            using (var cmd = new NpgsqlCommand(@"
                SELECT c.column_name, 
                       c.data_type,
                       c.is_nullable,
                       c.column_default,
                       c.ordinal_position,
                       pgd.description
                FROM information_schema.columns c
                LEFT JOIN pg_catalog.pg_statio_all_tables st ON st.schemaname = c.table_schema AND st.relname = c.table_name
                LEFT JOIN pg_catalog.pg_description pgd ON pgd.objoid = st.relid AND pgd.objsubid = c.ordinal_position
                WHERE c.table_schema = @schema_name
                AND c.table_name = @table_name
                ORDER BY c.ordinal_position", connection))
            {
                cmd.Parameters.AddWithValue("schema_name", schemaName);
                cmd.Parameters.AddWithValue("table_name", tableName);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new ColumnInfo
                        {
                            Name = reader.GetString(0),
                            DataType = reader.GetString(1),
                            IsNullable = reader.GetString(2) == "YES",
                            DefaultValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Position = reader.GetInt32(4),
                            Comment = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                    }
                }
            }

            return result;
        }

        private async Task<List<ConstraintInfo>> GetConstraints(NpgsqlConnection connection, string schemaName, string tableName)
        {
            var result = new List<ConstraintInfo>();

            using (var cmd = new NpgsqlCommand(@"
                SELECT con.conname as constraint_name,
                       CASE con.contype
                           WHEN 'c' THEN 'CHECK'
                           WHEN 'f' THEN 'FOREIGN KEY'
                           WHEN 'p' THEN 'PRIMARY KEY'
                           WHEN 'u' THEN 'UNIQUE'
                           ELSE con.contype::text
                       END as constraint_type,
                       pg_get_constraintdef(con.oid) as constraint_definition
                FROM pg_catalog.pg_constraint con
                INNER JOIN pg_catalog.pg_class rel ON rel.oid = con.conrelid
                INNER JOIN pg_catalog.pg_namespace nsp ON nsp.oid = rel.relnamespace
                WHERE nsp.nspname = @schema_name
                AND rel.relname = @table_name
                ORDER BY con.conname", connection))
            {
                cmd.Parameters.AddWithValue("schema_name", schemaName);
                cmd.Parameters.AddWithValue("table_name", tableName);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new ConstraintInfo
                        {
                            Name = reader.GetString(0),
                            Type = reader.GetString(1),
                            Definition = reader.GetString(2)
                        });
                    }
                }
            }

            return result;
        }

        private async Task<List<IndexInfo>> GetIndexes(NpgsqlConnection connection, string schemaName, string tableName)
        {
            var result = new List<IndexInfo>();

            using (var cmd = new NpgsqlCommand(@"
                SELECT i.relname as index_name,
                       pg_get_indexdef(i.oid) as index_definition
                FROM pg_catalog.pg_class i
                JOIN pg_catalog.pg_index ix ON ix.indexrelid = i.oid
                JOIN pg_catalog.pg_class t ON t.oid = ix.indrelid
                JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace
                WHERE n.nspname = @schema_name
                AND t.relname = @table_name
                AND i.relkind = 'i'
                ORDER BY i.relname", connection))
            {
                cmd.Parameters.AddWithValue("schema_name", schemaName);
                cmd.Parameters.AddWithValue("table_name", tableName);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new IndexInfo
                        {
                            Name = reader.GetString(0),
                            Definition = reader.GetString(1)
                        });
                    }
                }
            }

            return result;
        }

        private void CompareSchemas(List<SchemaInfo> sourceSchemas, List<SchemaInfo> targetSchemas, ComparisonResult result)
        {
            var sourceSchemaNames = sourceSchemas.Select(s => s.Name).ToHashSet();
            var targetSchemaNames = targetSchemas.Select(s => s.Name).ToHashSet();

            // Find schemas that exist in source but not in target (removed)
            foreach (var schemaName in sourceSchemaNames.Except(targetSchemaNames))
            {
                result.SchemaDifferences.Add(new SchemaDifference
                {
                    Type = DifferenceType.Removed,
                    SchemaName = schemaName
                });
            }

            // Find schemas that exist in target but not in source (added)
            foreach (var schemaName in targetSchemaNames.Except(sourceSchemaNames))
            {
                result.SchemaDifferences.Add(new SchemaDifference
                {
                    Type = DifferenceType.Added,
                    SchemaName = schemaName
                });
            }
        }

        private void CompareTables(List<SchemaInfo> sourceSchemas, List<SchemaInfo> targetSchemas, ComparisonResult result)
        {
            var sourceDict = sourceSchemas.ToDictionary(s => s.Name, s => s);
            var targetDict = targetSchemas.ToDictionary(s => s.Name, s => s);

            // Check schemas that exist in both source and target
            foreach (var schemaName in sourceDict.Keys.Intersect(targetDict.Keys))
            {
                var sourceSchema = sourceDict[schemaName];
                var targetSchema = targetDict[schemaName];

                var sourceTableNames = sourceSchema.Tables.Select(t => t.TableName).ToHashSet();
                var targetTableNames = targetSchema.Tables.Select(t => t.TableName).ToHashSet();

                // Find tables that exist in source but not in target (removed)
                foreach (var tableName in sourceTableNames.Except(targetTableNames))
                {
                    result.TableDifferences.Add(new TableDifference
                    {
                        Type = DifferenceType.Removed,
                        SchemaName = schemaName,
                        TableName = tableName
                    });
                }

                // Find tables that exist in target but not in source (added)
                foreach (var tableName in targetTableNames.Except(sourceTableNames))
                {
                    result.TableDifferences.Add(new TableDifference
                    {
                        Type = DifferenceType.Added,
                        SchemaName = schemaName,
                        TableName = tableName
                    });
                }
            }
        }

        private void CompareViews(List<SchemaInfo> sourceSchemas, List<SchemaInfo> targetSchemas, ComparisonResult result)
        {
            var sourceViews = sourceSchemas
                .SelectMany(s => s.Views)
                .ToDictionary(v => $"{v.SchemaName}.{v.ViewName}");

            var targetViews = targetSchemas
                .SelectMany(s => s.Views)
                .ToDictionary(v => $"{v.SchemaName}.{v.ViewName}");

            // Removed
            foreach (var key in sourceViews.Keys.Except(targetViews.Keys))
            {
                var v = sourceViews[key];
                result.ViewDifferences.Add(new ViewDifference
                {
                    Type = DifferenceType.Removed,
                    SchemaName = v.SchemaName,
                    ViewName = v.ViewName,
                    SourceDefinition = v.Definition
                });
            }

            // Added
            foreach (var key in targetViews.Keys.Except(sourceViews.Keys))
            {
                var v = targetViews[key];
                result.ViewDifferences.Add(new ViewDifference
                {
                    Type = DifferenceType.Added,
                    SchemaName = v.SchemaName,
                    ViewName = v.ViewName,
                    TargetDefinition = v.Definition
                });
            }

            // Modified
            foreach (var key in sourceViews.Keys.Intersect(targetViews.Keys))
            {
                var src = sourceViews[key];
                var tgt = targetViews[key];

                if (src.Definition!.Trim() != tgt.Definition!.Trim())
                {
                    result.ViewDifferences.Add(new ViewDifference
                    {
                        Type = DifferenceType.Modified,
                        SchemaName = src.SchemaName,
                        ViewName = src.ViewName,
                        SourceDefinition = src.Definition,
                        TargetDefinition = tgt.Definition
                    });
                }
            }
        }

        private void CompareColumns(List<SchemaInfo> sourceSchemas, List<SchemaInfo> targetSchemas, ComparisonResult result)
        {
            var sourceDict = sourceSchemas
                .SelectMany(s => s.Tables)
                .ToDictionary(t => $"{t.SchemaName}.{t.TableName}", t => t);

            var targetDict = targetSchemas
                .SelectMany(s => s.Tables)
                .ToDictionary(t => $"{t.SchemaName}.{t.TableName}", t => t);

            // Check tables that exist in both source and target
            foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
            {
                var sourceTable = sourceDict[key];
                var targetTable = targetDict[key];

                var sourceColumns = sourceTable.Columns.ToDictionary(c => c.Name, c => c);
                var targetColumns = targetTable.Columns.ToDictionary(c => c.Name, c => c);

                // Find columns that exist in source but not in target (removed)
                foreach (var columnName in sourceColumns.Keys.Except(targetColumns.Keys))
                {
                    var sourceColumn = sourceColumns[columnName];

                    result.ColumnDifferences.Add(new ColumnDifference
                    {
                        Type = DifferenceType.Removed,
                        SchemaName = sourceTable.SchemaName,
                        TableName = sourceTable.TableName,
                        ColumnName = columnName,
                        SourceDataType = sourceColumn.DataType,
                        SourceIsNullable = sourceColumn.IsNullable,
                        SourceDefaultValue = sourceColumn.DefaultValue,
                        SourceComment = sourceColumn.Comment
                    });
                }

                // Find columns that exist in target but not in source (added)
                foreach (var columnName in targetColumns.Keys.Except(sourceColumns.Keys))
                {
                    var targetColumn = targetColumns[columnName];

                    result.ColumnDifferences.Add(new ColumnDifference
                    {
                        Type = DifferenceType.Added,
                        SchemaName = targetTable.SchemaName,
                        TableName = targetTable.TableName,
                        ColumnName = columnName,
                        TargetDataType = targetColumn.DataType,
                        TargetIsNullable = targetColumn.IsNullable,
                        TargetDefaultValue = targetColumn.DefaultValue,
                        TargetComment = targetColumn.Comment
                    });
                }

                // Compare columns that exist in both source and target
                foreach (var columnName in sourceColumns.Keys.Intersect(targetColumns.Keys))
                {
                    var sourceColumn = sourceColumns[columnName];
                    var targetColumn = targetColumns[columnName];

                    bool isDifferent = false;
                    var difference = new ColumnDifference
                    {
                        Type = DifferenceType.Modified,
                        SchemaName = sourceTable.SchemaName,
                        TableName = sourceTable.TableName,
                        ColumnName = columnName,
                        SourceDataType = sourceColumn.DataType,
                        TargetDataType = targetColumn.DataType,
                        SourceIsNullable = sourceColumn.IsNullable,
                        TargetIsNullable = targetColumn.IsNullable,
                        SourceDefaultValue = sourceColumn.DefaultValue,
                        TargetDefaultValue = targetColumn.DefaultValue,
                        SourceComment = sourceColumn.Comment,
                        TargetComment = targetColumn.Comment
                    };

                    // Check if data type is different
                    if (sourceColumn.DataType != targetColumn.DataType)
                    {
                        isDifferent = true;
                    }

                    // Check if nullable is different
                    if (sourceColumn.IsNullable != targetColumn.IsNullable)
                    {
                        isDifferent = true;
                    }

                    // Check if default value is different
                    if (sourceColumn.DefaultValue != targetColumn.DefaultValue)
                    {
                        isDifferent = true;
                    }

                    // Check if comment is different
                    if (sourceColumn.Comment != targetColumn.Comment)
                    {
                        isDifferent = true;
                    }

                    if (isDifferent)
                    {
                        result.ColumnDifferences.Add(difference);
                    }
                }
            }
        }

        private void CompareConstraints(List<SchemaInfo> sourceSchemas, List<SchemaInfo> targetSchemas, ComparisonResult result)
        {
            var sourceDict = sourceSchemas
                .SelectMany(s => s.Tables)
                .ToDictionary(t => $"{t.SchemaName}.{t.TableName}", t => t);

            var targetDict = targetSchemas
                .SelectMany(s => s.Tables)
                .ToDictionary(t => $"{t.SchemaName}.{t.TableName}", t => t);

            // Check tables that exist in both source and target
            foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
            {
                var sourceTable = sourceDict[key];
                var targetTable = targetDict[key];

                var sourceConstraints = sourceTable.Constraints.ToDictionary(c => c.Name, c => c);
                var targetConstraints = targetTable.Constraints.ToDictionary(c => c.Name, c => c);

                // Find constraints that exist in source but not in target (removed)
                foreach (var constraintName in sourceConstraints.Keys.Except(targetConstraints.Keys))
                {
                    var sourceConstraint = sourceConstraints[constraintName];

                    result.ConstraintDifferences.Add(new ConstraintDifference
                    {
                        Type = DifferenceType.Removed,
                        SchemaName = sourceTable.SchemaName,
                        TableName = sourceTable.TableName,
                        ConstraintName = constraintName,
                        SourceDefinition = sourceConstraint.Definition
                    });
                }

                // Find constraints that exist in target but not in source (added)
                foreach (var constraintName in targetConstraints.Keys.Except(sourceConstraints.Keys))
                {
                    var targetConstraint = targetConstraints[constraintName];

                    result.ConstraintDifferences.Add(new ConstraintDifference
                    {
                        Type = DifferenceType.Added,
                        SchemaName = targetTable.SchemaName,
                        TableName = targetTable.TableName,
                        ConstraintName = constraintName,
                        TargetDefinition = targetConstraint.Definition
                    });
                }

                // Compare constraints that exist in both source and target
                foreach (var constraintName in sourceConstraints.Keys.Intersect(targetConstraints.Keys))
                {
                    var sourceConstraint = sourceConstraints[constraintName];
                    var targetConstraint = targetConstraints[constraintName];

                    // Check if definition is different
                    if (sourceConstraint.Definition != targetConstraint.Definition)
                    {
                        result.ConstraintDifferences.Add(new ConstraintDifference
                        {
                            Type = DifferenceType.Modified,
                            SchemaName = sourceTable.SchemaName,
                            TableName = sourceTable.TableName,
                            ConstraintName = constraintName,
                            SourceDefinition = sourceConstraint.Definition,
                            TargetDefinition = targetConstraint.Definition
                        });
                    }
                }
            }
        }

        private void CompareIndexes(List<SchemaInfo> sourceSchemas, List<SchemaInfo> targetSchemas, ComparisonResult result)
        {
            var sourceDict = sourceSchemas
                .SelectMany(s => s.Tables)
                .ToDictionary(t => $"{t.SchemaName}.{t.TableName}", t => t);

            var targetDict = targetSchemas
                .SelectMany(s => s.Tables)
                .ToDictionary(t => $"{t.SchemaName}.{t.TableName}", t => t);

            // Check tables that exist in both source and target
            foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
            {
                var sourceTable = sourceDict[key];
                var targetTable = targetDict[key];

                var sourceIndexes = sourceTable.Indexes.ToDictionary(i => i.Name, i => i);
                var targetIndexes = targetTable.Indexes.ToDictionary(i => i.Name, i => i);

                // Find indexes that exist in source but not in target (removed)
                foreach (var indexName in sourceIndexes.Keys.Except(targetIndexes.Keys))
                {
                    var sourceIndex = sourceIndexes[indexName];

                    result.IndexDifferences.Add(new IndexDifference
                    {
                        Type = DifferenceType.Removed,
                        SchemaName = sourceTable.SchemaName,
                        TableName = sourceTable.TableName,
                        IndexName = indexName,
                        SourceDefinition = sourceIndex.Definition
                    });
                }

                // Find indexes that exist in target but not in source (added)
                foreach (var indexName in targetIndexes.Keys.Except(sourceIndexes.Keys))
                {
                    var targetIndex = targetIndexes[indexName];

                    result.IndexDifferences.Add(new IndexDifference
                    {
                        Type = DifferenceType.Added,
                        SchemaName = targetTable.SchemaName,
                        TableName = targetTable.TableName,
                        IndexName = indexName,
                        TargetDefinition = targetIndex.Definition
                    });
                }

                // Compare indexes that exist in both source and target
                foreach (var indexName in sourceIndexes.Keys.Intersect(targetIndexes.Keys))
                {
                    var sourceIndex = sourceIndexes[indexName];
                    var targetIndex = targetIndexes[indexName];

                    // Check if definition is different
                    if (sourceIndex.Definition != targetIndex.Definition)
                    {
                        result.IndexDifferences.Add(new IndexDifference
                        {
                            Type = DifferenceType.Modified,
                            SchemaName = sourceTable.SchemaName,
                            TableName = sourceTable.TableName,
                            IndexName = indexName,
                            SourceDefinition = sourceIndex.Definition,
                            TargetDefinition = targetIndex.Definition
                        });
                    }
                }
            }
        }
    }
}
