using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// Liveness endpoint for the rewrite app (rw-001a AC #6).
/// Returns plain-text "Healthy" so downstream tooling (Cucumber, future App Service health probes)
/// can verify the process is responsive without exercising the database.
/// </summary>
[Route("Health")]
public class HealthController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return Content("Healthy", "text/plain");
    }
}
