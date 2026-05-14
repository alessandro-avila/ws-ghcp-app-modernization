#nullable disable
using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// Protected endpoint exercised by rw-001b Cucumber scenarios. The global
/// <c>FallbackPolicy</c> requires an authenticated user, so anonymous requests
/// receive a 302 redirect to <c>/Account/SignIn</c> from the cookie auth scheme.
/// </summary>
[Route("Dashboard")]
public class DashboardController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["UserName"] = User?.Identity?.Name ?? string.Empty;
        return View();
    }
}
