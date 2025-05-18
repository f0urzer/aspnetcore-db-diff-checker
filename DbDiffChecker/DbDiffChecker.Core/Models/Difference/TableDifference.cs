namespace DbDiffChecker.Core.Models.Difference
{
    public class TableDifference : DifferenceBase
    {
        public string? SchemaName { get; set; }
        public string? TableName { get; set; }
    }
}
