#nullable disable
using System;
using System.Linq;
using System.Threading.Tasks;
using ContosoUniversity.Web.Data;
using ContosoUniversity.Web.Domain;
using ContosoUniversity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// rw-004 (F-001): StudentsController ported from
/// src/ContosoUniversity/Controllers/StudentsController.cs (legacy MVC 5).
///
/// Authorization (ADR-006):
///   - Class-level [Authorize(Roles = "Admin,Reader")] gates Index + Details so
///     anonymous requests redirect to /Account/SignIn (cookie auth challenge),
///     and Reader requests succeed for read-only paths.
///   - Action-level [Authorize(Roles = "Admin")] on Create/Edit/Delete makes
///     reader-role requests return 403 Forbidden BEFORE antiforgery validation
///     (same filter ordering pattern verified by rw-003).
///
/// Page size DECISION (FRD §NFR-F-001-001):
///   - The FRD documents 3 students per page; the legacy controller uses 10
///     (drift between docs and code in the original app). The rewrite honors
///     the documented intent (pageSize = 3) so test scenarios on small seed
///     sets are deterministic. This is a conscious rewrite decision, not a
///     Track A green-baseline capture.
///
/// KL-F-001-001 FIX (Details path):
///   - Legacy <c>Details</c> uses <c>.Single()</c> which throws
///     <see cref="InvalidOperationException"/> -> HTTP 500 for missing
///     students. The rewrite uses <c>.SingleOrDefaultAsync()</c> -> HTTP 404
///     to honor the documented intent of US-F-001-002.
///
/// Notifications: legacy <c>SendEntityNotification</c> calls are intentionally
/// NOT ported in rw-004. NotificationService is not yet wired in the rewrite
/// Web project; surfacing it is queued for rw-007 (notifications subsystem).
///
/// Concurrency (RowVersion): the Student entity does NOT have a
/// <c>[Timestamp]</c> field in the legacy schema, so optimistic-concurrency
/// handling on Edit (the rw-003 pattern) does NOT apply here. Edit/Delete
/// simply mirror the rw-003 [Authorize] gating + CSRF wiring without the
/// concurrency-token diff logic.
/// </summary>
[Authorize(Roles = "Admin,Reader")]
public class StudentsController : Controller
{
    private const int PageSize = 3;

    private readonly SchoolContext _db;

    public StudentsController(SchoolContext db)
    {
        _db = db;
    }

    // GET: Students
    public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? page)
    {
        ViewBag.CurrentSort = sortOrder;
        ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

        if (searchString != null)
        {
            page = 1;
        }
        else
        {
            searchString = currentFilter;
        }

        ViewBag.CurrentFilter = searchString;

        IQueryable<Student> students = _db.Students.AsNoTracking();

        if (!string.IsNullOrEmpty(searchString))
        {
            students = students.Where(s =>
                s.LastName.Contains(searchString) ||
                s.FirstMidName.Contains(searchString));
        }

        students = sortOrder switch
        {
            "name_desc" => students.OrderByDescending(s => s.LastName),
            "Date" => students.OrderBy(s => s.EnrollmentDate),
            "date_desc" => students.OrderByDescending(s => s.EnrollmentDate),
            _ => students.OrderBy(s => s.LastName),
        };

        var pageNumber = page ?? 1;
        var paginated = await PaginatedList<Student>.CreateAsync(students, pageNumber, PageSize);
        return View(paginated);
    }

    // GET: Students/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        // KL-F-001-001 FIX: SingleOrDefaultAsync -> 404 (rewrite),
        // legacy used Single() -> 500 on missing rows.
        var student = await _db.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.ID == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // GET: Students/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        var student = new Student
        {
            EnrollmentDate = DateTime.Today
        };
        return View(student);
    }

    // POST: Students/Create
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("LastName,FirstMidName,EnrollmentDate")] Student student)
    {
        if (ModelState.IsValid)
        {
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(student);
    }

    // GET: Students/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var student = await _db.Students.FindAsync(id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // POST: Students/Edit/5
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [Bind("ID,LastName,FirstMidName,EnrollmentDate")] Student student)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(student).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(student);
    }

    // GET: Students/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var student = await _db.Students
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.ID == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // POST: Students/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var student = await _db.Students.FindAsync(id);
            if (student != null)
            {
                _db.Students.Remove(student);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "Unable to delete the student. Try again, and if the problem persists "
                + "see your system administrator.";
            return RedirectToAction(nameof(Index));
        }
    }
}
