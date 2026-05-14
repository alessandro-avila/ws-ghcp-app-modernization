using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// Test-only diagnostic endpoints used to exercise the production-hardening
/// middleware (rw-001d) end-to-end. Every action checks
/// <see cref="IWebHostEnvironment.IsDevelopment"/> on entry and returns 404 in
/// non-dev environments so the endpoints are unreachable in production even if
/// the assembly is deployed. The actions are also marked <see cref="AllowAnonymousAttribute"/>
/// so the Cucumber harness can hit them without first sign-in.
/// </summary>
[AllowAnonymous]
[Route("Diag")]
public class DiagController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public DiagController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Throws an unhandled <see cref="InvalidOperationException"/> so the
    /// configured exception handler middleware can be exercised by the
    /// rw-001d red-baseline scenario "Server-side exceptions render a friendly
    /// page with no internal details".
    /// </summary>
    [HttpGet("Throw")]
    public IActionResult Throw()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        throw new InvalidOperationException(
            "Diagnostic throw triggered by GET /Diag/Throw — this endpoint is gated " +
            "to the Development environment and exists only to exercise app.UseExceptionHandler.");
    }
}
