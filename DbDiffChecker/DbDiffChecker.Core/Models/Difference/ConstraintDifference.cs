namespace DbDiffChecker.Core.Models.Difference
{
    public class ConstraintDifference : DifferenceBase
    {
        public string? SchemaName { get; set; }
        public string? TableName { get; set; }
        public string? ConstraintName { get; set; }
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }
    }
}
