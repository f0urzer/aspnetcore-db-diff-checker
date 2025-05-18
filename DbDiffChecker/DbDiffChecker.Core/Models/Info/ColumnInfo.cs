namespace DbDiffChecker.Core.Models.Info
{
    public class ColumnInfo
    {
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public bool IsNullable { get; set; }
        public string? DefaultValue { get; set; }
        public string? Comment { get; set; }
        public int Position { get; set; }
    }
}
