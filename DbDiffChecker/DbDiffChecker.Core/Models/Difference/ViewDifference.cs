namespace DbDiffChecker.Core.Models.Difference
{
    public class ViewDifference : DifferenceBase
    {
        public string? SchemaName { get; set; }
        public string? ViewName { get; set; }
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }
    }
}
