using System.Diagnostics;
using ContosoUniversity.Web.Data;
using ContosoUniversity.Web.Models;
using ContosoUniversity.Web.Models.SchoolViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Web.Controllers;

// Public landing page: anonymous so the rewrite app's home and error pages
// remain reachable even when the global FallbackPolicy demands authentication
// for everything else (rw-001b). Per ADR-006, [AllowAnonymous] is applied at
// the class level for HomeController only (Index/About/Contact/Privacy/Error).
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SchoolContext _db;

    public HomeController(ILogger<HomeController> logger, SchoolContext db)
    {
        _logger = logger;
        _db = db;
    }

    public IActionResult Index()
    {
        return View();
    }

    // F-005 / rw-002: ports legacy Controllers/HomeController.About -- projects
    // Students grouped by EnrollmentDate into the EnrollmentDateGroup view model.
    // When the Students table is empty the query returns an empty list and the
    // view renders a header-only table with HTTP 200.
    public async Task<IActionResult> About()
    {
        var data = await _db.Students
            .GroupBy(s => s.EnrollmentDate)
            .Select(g => new EnrollmentDateGroup
            {
                EnrollmentDate = g.Key,
                StudentCount = g.Count()
            })
            .ToListAsync();

        return View(data);
    }

    // F-008 / rw-002: ports legacy Controllers/HomeController.Contact -- static
    // contact card. Sets ViewData["Message"] for parity with legacy ViewBag.Message.
    public IActionResult Contact()
    {
        ViewData["Message"] = "Your contact page.";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
