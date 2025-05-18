using DbDiffChecker.Core.Models.Info;
using DbDiffChecker.Core.Models.Response;

namespace DbDiffChecker.Core.Services.Interface
{
    public interface IDatabaseService
    {
        Task<List<SchemaInfo>> GetDatabaseSchemaInfo(DbConnectionInfo connectionInfo);
        Task<ComparisonResult> CompareDatabases(List<SchemaInfo> sourceSchemas, List<SchemaInfo> targetSchemas);
    }
}
