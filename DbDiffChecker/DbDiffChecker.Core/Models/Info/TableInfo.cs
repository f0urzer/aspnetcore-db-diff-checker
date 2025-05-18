namespace DbDiffChecker.Core.Models.Info
{
    public class TableInfo
    {
        public string? SchemaName { get; set; }
        public string? TableName { get; set; }
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
        public List<ConstraintInfo> Constraints { get; set; } = new List<ConstraintInfo>();
        public List<IndexInfo> Indexes { get; set; } = new List<IndexInfo>();
    }
}
