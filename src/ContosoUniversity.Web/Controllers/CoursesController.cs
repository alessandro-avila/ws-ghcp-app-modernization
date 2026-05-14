#nullable disable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.Web.Data;
using ContosoUniversity.Web.Domain;
using ContosoUniversity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// rw-005 (F-002): CoursesController ported from
/// <c>src/ContosoUniversity/Controllers/CourseController.cs</c> (legacy MVC 5),
/// closing SEC-HIGH-002 (shallow file-upload validation) across four vectors:
///   1. Framework-level 5 MB request-size cap via <c>[RequestSizeLimit(5_242_880)]</c>
///      on <see cref="Create(Course, IFormFile, CancellationToken)"/> +
///      <see cref="Edit(Course, IFormFile, CancellationToken)"/>.
///   2. Extension allowlist via <see cref="IUploadValidator"/>.
///   3. Magic-byte check via <see cref="IUploadValidator"/> (SixLabors.ImageSharp).
///   4. Sanitized server-generated filename via <see cref="IUploadValidator.GenerateSanitizedFileName"/>.
///   5. Files are persisted under <c>App_Data/uploads/teaching-materials/</c> (a non-web-rooted
///      folder so static-file middleware never serves them) and the dedicated
///      <see cref="Image(int, CancellationToken)"/> action streams the bytes back with
///      <c>Content-Disposition: attachment</c> to defeat inline-render attacks.
///
/// Authorization (ADR-006):
///   - Class-level <c>[Authorize(Roles="Admin,Reader")]</c> gates Index + Details + Image
///     so anonymous requests redirect to /Account/SignIn (cookie auth challenge).
///   - Action-level <c>[Authorize(Roles="Admin")]</c> on Create/Edit/Delete makes
///     reader-role POSTs return 403 Forbidden BEFORE the antiforgery filter (per ADR-006);
///     this is intentional and tested by the reader-create scenarios.
///
/// Notifications (rw-007 / SEC-CRITICAL-002): every successful CUD action
/// publishes a <see cref="NotificationEnvelope"/> via
/// <see cref="INotificationService.PublishAsync"/>; <c>CreatedBy</c> captures
/// <c>User.Identity?.Name</c> so the audit row attributes the change to the
/// authenticated principal instead of the legacy hardcoded "System" sentinel.
/// </summary>
[Authorize(Roles = "Admin,Reader")]
public class CoursesController : Controller
{
    /// <summary>rw-005: framework-level request-size cap (5 MB) per SEC-HIGH-002 vector 1.</summary>
    public const long MaxUploadBytes = 5_242_880;

    /// <summary>
    /// rw-005: server-side upload directory, relative to <see cref="IWebHostEnvironment.ContentRootPath"/>.
    /// Lives under <c>App_Data/</c> precisely because that path is NOT served by the static-file
    /// middleware — uploaded files are only reachable via the authenticated <see cref="Image"/> action.
    /// Kept in sync with <c>SeedSchoolData.SeedCourseImageRelativePath</c>.
    /// </summary>
    public const string UploadDirectoryRelativePath = "App_Data/uploads/teaching-materials";

    private readonly SchoolContext _db;
    private readonly IUploadValidator _uploadValidator;
    private readonly IWebHostEnvironment _env;
    private readonly INotificationService _notifications;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(
        SchoolContext db,
        IUploadValidator uploadValidator,
        IWebHostEnvironment env,
        INotificationService notifications,
        ILogger<CoursesController> logger)
    {
        _db = db;
        _uploadValidator = uploadValidator;
        _env = env;
        _notifications = notifications;
        _logger = logger;
    }

    // GET: Courses
    public async Task<IActionResult> Index()
    {
        var courses = await _db.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .OrderBy(c => c.CourseID)
            .ToListAsync();
        return View(courses);
    }

    // GET: Courses/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var course = await _db.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CourseID == id);

        if (course == null)
        {
            return NotFound();
        }

        return View(course);
    }

    // GET: Courses/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        PopulateDepartmentsDropDownList();
        return View();
    }

    // POST: Courses/Create
    // SEC-HIGH-002 vector 1: 5 MB framework-level cap. The early Content-Length
    // middleware in Program.cs short-circuits oversize requests with HTTP 413
    // BEFORE the MVC pipeline runs antiforgery validation (cucumber AC#7);
    // [RequestSizeLimit] is the belt-and-braces Kestrel-level cap for clients
    // that omit the Content-Length header.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Create(
        [Bind("CourseID,Title,Credits,DepartmentID")] Course course,
        IFormFile teachingMaterialImage,
        CancellationToken cancellationToken)
    {
        // SEC-HIGH-002 upload validation BEFORE ModelState so a malicious file
        // surfaces 415/400 with an empty body (cucumber expects status-code only,
        // not a re-rendered form).
        if (teachingMaterialImage is { Length: > 0 })
        {
            UploadValidationOutcome outcome;
            await using (var stream = teachingMaterialImage.OpenReadStream())
            {
                outcome = await _uploadValidator
                    .ValidateAsync(stream, teachingMaterialImage.FileName, cancellationToken)
                    .ConfigureAwait(false);
            }

            switch (outcome)
            {
                case UploadValidationOutcome.PathTraversalRejected:
                    _logger.LogWarning(
                        "rw-005: rejected upload for Course CourseID={CourseID} due to path-traversal filename '{FileName}'.",
                        course.CourseID, teachingMaterialImage.FileName);
                    return BadRequest();
                case UploadValidationOutcome.ExtensionRejected:
                case UploadValidationOutcome.MagicByteRejected:
                    _logger.LogWarning(
                        "rw-005: rejected upload for Course CourseID={CourseID} (outcome={Outcome}, filename='{FileName}').",
                        course.CourseID, outcome, teachingMaterialImage.FileName);
                    return new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);
                case UploadValidationOutcome.EmptyFile:
                    // Treat as "no file uploaded" — fall through to ModelState validation.
                    teachingMaterialImage = null;
                    break;
            }
        }

        if (ModelState.IsValid)
        {
            if (teachingMaterialImage is { Length: > 0 })
            {
                course.TeachingMaterialImagePath = await PersistUploadAsync(
                    course.CourseID, teachingMaterialImage, cancellationToken)
                    .ConfigureAwait(false);
            }

            _db.Courses.Add(course);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _notifications.PublishAsync(
                entityType: nameof(Course),
                entityId: course.CourseID.ToString(System.Globalization.CultureInfo.InvariantCulture),
                displayName: course.Title,
                operation: EntityOperation.CREATE,
                createdBy: User.Identity?.Name,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return RedirectToAction(nameof(Index));
        }

        PopulateDepartmentsDropDownList(course.DepartmentID);
        return View(course);
    }

    // GET: Courses/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CourseID == id);

        if (course == null)
        {
            return NotFound();
        }

        PopulateDepartmentsDropDownList(course.DepartmentID);
        return View(course);
    }

    // POST: Courses/Edit/5
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Edit(
        [Bind("CourseID,Title,Credits,DepartmentID,TeachingMaterialImagePath")] Course course,
        IFormFile teachingMaterialImage,
        CancellationToken cancellationToken)
    {
        if (teachingMaterialImage is { Length: > 0 })
        {
            UploadValidationOutcome outcome;
            await using (var stream = teachingMaterialImage.OpenReadStream())
            {
                outcome = await _uploadValidator
                    .ValidateAsync(stream, teachingMaterialImage.FileName, cancellationToken)
                    .ConfigureAwait(false);
            }

            switch (outcome)
            {
                case UploadValidationOutcome.PathTraversalRejected:
                    return BadRequest();
                case UploadValidationOutcome.ExtensionRejected:
                case UploadValidationOutcome.MagicByteRejected:
                    return new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);
                case UploadValidationOutcome.EmptyFile:
                    teachingMaterialImage = null;
                    break;
            }
        }

        if (ModelState.IsValid)
        {
            if (teachingMaterialImage is { Length: > 0 })
            {
                course.TeachingMaterialImagePath = await PersistUploadAsync(
                    course.CourseID, teachingMaterialImage, cancellationToken)
                    .ConfigureAwait(false);
            }

            _db.Entry(course).State = EntityState.Modified;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _notifications.PublishAsync(
                entityType: nameof(Course),
                entityId: course.CourseID.ToString(System.Globalization.CultureInfo.InvariantCulture),
                displayName: course.Title,
                operation: EntityOperation.UPDATE,
                createdBy: User.Identity?.Name,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return RedirectToAction(nameof(Index));
        }

        PopulateDepartmentsDropDownList(course.DepartmentID);
        return View(course);
    }

    // GET: Courses/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var course = await _db.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CourseID == id);

        if (course == null)
        {
            return NotFound();
        }

        return View(course);
    }

    // POST: Courses/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var course = await _db.Courses.FindAsync(new object[] { id }, cancellationToken)
            .ConfigureAwait(false);
        if (course != null)
        {
            _db.Courses.Remove(course);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _notifications.PublishAsync(
                entityType: nameof(Course),
                entityId: course.CourseID.ToString(System.Globalization.CultureInfo.InvariantCulture),
                displayName: course.Title,
                operation: EntityOperation.DELETE,
                createdBy: User.Identity?.Name,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: Courses/Image/5
    // SEC-HIGH-002 vector 5: serve uploaded teaching-material images via an
    // authenticated action with Content-Disposition: attachment so the browser
    // never renders user-supplied content inline (defeats SVG-with-script and
    // similar inline-render attacks).
    public async Task<IActionResult> Image(int id, CancellationToken cancellationToken)
    {
        var course = await _db.Courses
            .AsNoTracking()
            .Where(c => c.CourseID == id)
            .Select(c => new { c.CourseID, c.TeachingMaterialImagePath })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (course == null || string.IsNullOrWhiteSpace(course.TeachingMaterialImagePath))
        {
            return NotFound();
        }

        // Resolve the absolute path under the content root, then sanity-check that the
        // resolved path is still inside the upload directory (defense-in-depth in case
        // the persisted TeachingMaterialImagePath was tampered with via direct DB write).
        var contentRoot = _env.ContentRootPath;
        var uploadRoot = Path.GetFullPath(Path.Combine(contentRoot,
            UploadDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        var absolutePath = Path.GetFullPath(Path.Combine(contentRoot,
            course.TeachingMaterialImagePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!absolutePath.StartsWith(uploadRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(absolutePath, uploadRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "rw-005: refused to serve TeachingMaterialImagePath outside upload root for CourseID={CourseID}.",
                course.CourseID);
            return NotFound();
        }

        if (!System.IO.File.Exists(absolutePath))
        {
            return NotFound();
        }

        var downloadFileName = Path.GetFileName(absolutePath);
        var contentType = ResolveContentType(downloadFileName);

        // PhysicalFile + fileDownloadName == "x" yields Content-Disposition: attachment; filename=x
        return PhysicalFile(absolutePath, contentType, downloadFileName);
    }

    private static string ResolveContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    private async Task<string> PersistUploadAsync(
        int courseId, IFormFile file, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var sanitizedName = _uploadValidator.GenerateSanitizedFileName(courseId, ext);

        var contentRoot = _env.ContentRootPath;
        var uploadRoot = Path.Combine(contentRoot,
            UploadDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(uploadRoot);

        var absolutePath = Path.Combine(uploadRoot, sanitizedName);
        await using (var dest = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(dest, cancellationToken).ConfigureAwait(false);
        }

        // Persist the path RELATIVE to the content root so different deployments
        // (different ContentRootPath values) keep working.
        return Path.Combine(UploadDirectoryRelativePath, sanitizedName)
            .Replace(Path.DirectorySeparatorChar, '/');
    }

    private void PopulateDepartmentsDropDownList(object selectedDepartment = null)
    {
        var departments = _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToList();
        ViewBag.DepartmentID = new SelectList(departments, "DepartmentID", "Name", selectedDepartment);
    }
}
