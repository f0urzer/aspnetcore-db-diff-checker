namespace DbDiffChecker.Core.Models.Difference
{
    public class IndexDifference : DifferenceBase
    {
        public string? SchemaName { get; set; }
        public string? TableName { get; set; }
        public string? IndexName { get; set; }
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }
    }
}
