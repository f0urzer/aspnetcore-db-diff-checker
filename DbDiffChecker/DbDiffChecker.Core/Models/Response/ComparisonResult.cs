using DbDiffChecker.Core.Models.Difference;

namespace DbDiffChecker.Core.Models.Response
{
    public class ComparisonResult
    {
        public List<SchemaDifference> SchemaDifferences { get; set; } = new List<SchemaDifference>();
        public List<TableDifference> TableDifferences { get; set; } = new List<TableDifference>();
        public List<ViewDifference> ViewDifferences { get; set; } = new List<ViewDifference>();
        public List<ColumnDifference> ColumnDifferences { get; set; } = new List<ColumnDifference>();
        public List<ConstraintDifference> ConstraintDifferences { get; set; } = new List<ConstraintDifference>();
        public List<IndexDifference> IndexDifferences { get; set; } = new List<IndexDifference>();
    }
}
