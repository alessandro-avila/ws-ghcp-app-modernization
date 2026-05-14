using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// Renders the friendly error page for both unhandled exceptions
/// (via <c>app.UseExceptionHandler("/Error/500")</c>) and HTTP error status codes
/// (via <c>app.UseStatusCodePagesWithReExecute("/Error/{0}")</c>).
///
/// Anonymous so that even pre-auth exceptions surface the friendly page rather
/// than triggering an authentication challenge that would mask the original error.
/// (rw-001d, SEC-MEDIUM-001.)
/// </summary>
[AllowAnonymous]
[Route("Error")]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    // Accept any HTTP verb. UseStatusCodePagesWithReExecute preserves the
    // original request method when re-executing the error path, so a failed
    // POST /Departments/Create (e.g. role-check 403 or anti-forgery 400) is
    // re-executed as POST /Error/403 or POST /Error/400. Constraining to GET
    // would cause those re-executions to fall through to MapFallback and
    // surface as 404 instead of the original status (rw-003).
    [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", Route = "")]
    [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", Route = "{statusCode:int?}")]
    public IActionResult Index(int? statusCode)
    {
        // Capture the original exception (set by UseExceptionHandler) for structured logging.
        // Never surface its details to the response body.
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature is not null)
        {
            _logger.LogError(
                exceptionFeature.Error,
                "Unhandled exception while processing {Path}",
                exceptionFeature.Path);
        }

        var resolvedStatus = statusCode ?? (exceptionFeature is not null ? 500 : 400);
        Response.StatusCode = resolvedStatus;

        ViewData["StatusCode"] = resolvedStatus;
        return View("Index");
    }
}
