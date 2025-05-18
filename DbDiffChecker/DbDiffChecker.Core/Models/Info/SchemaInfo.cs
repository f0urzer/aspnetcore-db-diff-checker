namespace DbDiffChecker.Core.Models.Info
{
    public class SchemaInfo
    {
        public string? Name { get; set; }
        public string? Owner { get; set; }
        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();
        public List<ViewInfo> Views { get; set; } = new List<ViewInfo>();
    }
}
