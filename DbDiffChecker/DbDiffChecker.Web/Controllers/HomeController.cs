using DbDiffChecker.Core.Models.Info;
using DbDiffChecker.Core.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DbDiffChecker.Web.Controllers
{
    public class HomeController(IDatabaseService service) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Compare(DbConnectionInfo sourceConnection, DbConnectionInfo targetConnection)
        {
            if (string.IsNullOrEmpty(sourceConnection.ConnectionString) || string.IsNullOrEmpty(targetConnection.ConnectionString))
            {
                ModelState.AddModelError("", "Connection strings are required");
                return View("Index");
            }

            try
            {
                // Get schema info from both databases
                var sourceSchemas = await service.GetDatabaseSchemaInfo(sourceConnection);
                var targetSchemas = await service.GetDatabaseSchemaInfo(targetConnection);

                // Compare the schemas
                var result = await service.CompareDatabases(sourceSchemas, targetSchemas);

                // Store the result in ViewData
                ViewData["SourceName"] = sourceConnection.Name ?? "Source Database";
                ViewData["TargetName"] = targetConnection.Name ?? "Target Database";
                ViewData["ComparisonResult"] = result;

                return View("ComparisonResult");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error comparing databases: {ex.Message}");
                return View("Index");
            }
        }
    }
}