namespace DbDiffChecker.Core.Models.Difference
{
    public class ColumnDifference : DifferenceBase
    {
        public string? SchemaName { get; set; }
        public string? TableName { get; set; }
        public string? ColumnName { get; set; }
        public string? SourceDataType { get; set; }
        public string? TargetDataType { get; set; }
        public bool? SourceIsNullable { get; set; }
        public bool? TargetIsNullable { get; set; }
        public string? SourceDefaultValue { get; set; }
        public string? TargetDefaultValue { get; set; }
        public string? SourceComment { get; set; }
        public string? TargetComment { get; set; }
    }
}
