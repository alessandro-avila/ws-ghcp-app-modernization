#nullable disable
using System;
using System.Linq;
using System.Threading.Tasks;
using ContosoUniversity.Web.Data;
using ContosoUniversity.Web.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// rw-003 (F-004): DepartmentsController ported from
/// src/ContosoUniversity/Controllers/DepartmentsController.cs (legacy MVC 5).
///
/// Authorization (ADR-006):
///   - Class-level [Authorize(Roles = "Admin,Reader")] gates Index + Details so
///     anonymous requests redirect to /Account/SignIn (cookie auth challenge).
///   - Action-level [Authorize(Roles = "Admin")] on Create/Edit/Delete makes
///     reader-role POSTs return 403 Forbidden (read-only role).
///
/// Concurrency (RowVersion):
///   - Edit POST sets <c>OriginalValues["RowVersion"]</c> so a stale token
///     triggers <see cref="DbUpdateConcurrencyException"/>. The handler diffs
///     the posted entity against the database row and surfaces per-property
///     "Current value" model errors (parity with the legacy controller).
///
/// Notifications: legacy <c>SendEntityNotification</c> calls are intentionally
/// NOT ported in rw-003. NotificationService is not yet wired in the rewrite
/// Web project; surfacing it is queued for a later increment that ports the
/// notifications subsystem (see specs/frd-notification-system.md).
/// </summary>
[Authorize(Roles = "Admin,Reader")]
public class DepartmentsController : Controller
{
    private readonly SchoolContext _db;

    public DepartmentsController(SchoolContext db)
    {
        _db = db;
    }

    // GET: Departments
    public async Task<IActionResult> Index()
    {
        var departments = await _db.Departments
            .Include(d => d.Administrator)
            .AsNoTracking()
            .ToListAsync();
        return View(departments);
    }

    // GET: Departments/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var department = await _db.Departments
            .Include(d => d.Administrator)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepartmentID == id);

        if (department == null)
        {
            return NotFound();
        }

        return View(department);
    }

    // GET: Departments/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        PopulateAdministratorsDropDownList();
        return View();
    }

    // POST: Departments/Create
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Budget,StartDate,InstructorID")] Department department)
    {
        if (ModelState.IsValid)
        {
            _db.Departments.Add(department);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        PopulateAdministratorsDropDownList(department.InstructorID);
        return View(department);
    }

    // GET: Departments/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var department = await _db.Departments
            .Include(d => d.Administrator)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepartmentID == id);

        if (department == null)
        {
            return NotFound();
        }

        PopulateAdministratorsDropDownList(department.InstructorID);
        return View(department);
    }

    // POST: Departments/Edit/5
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [Bind("DepartmentID,Name,Budget,StartDate,InstructorID,RowVersion")] Department department)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _db.Entry(department).OriginalValues["RowVersion"] = department.RowVersion;
                _db.Entry(department).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();
                var clientValues = (Department)entry.Entity;
                var databaseEntry = await entry.GetDatabaseValuesAsync();

                if (databaseEntry == null)
                {
                    ModelState.AddModelError(string.Empty,
                        "Unable to save changes. The department was deleted by another user.");
                }
                else
                {
                    var databaseValues = (Department)databaseEntry.ToObject();

                    if (databaseValues.Name != clientValues.Name)
                    {
                        ModelState.AddModelError("Name", $"Current value: {databaseValues.Name}");
                    }
                    if (databaseValues.Budget != clientValues.Budget)
                    {
                        ModelState.AddModelError("Budget", $"Current value: {databaseValues.Budget:c}");
                    }
                    if (databaseValues.StartDate != clientValues.StartDate)
                    {
                        ModelState.AddModelError("StartDate", $"Current value: {databaseValues.StartDate:d}");
                    }
                    if (databaseValues.InstructorID != clientValues.InstructorID)
                    {
                        var instructor = await _db.Instructors
                            .AsNoTracking()
                            .FirstOrDefaultAsync(i => i.ID == databaseValues.InstructorID);
                        ModelState.AddModelError("InstructorID",
                            $"Current value: {instructor?.FullName}");
                    }

                    ModelState.AddModelError(string.Empty,
                        "The record you attempted to edit was modified by another user after "
                        + "you got the original value. The edit operation was canceled and the "
                        + "current values in the database have been displayed. If you still want "
                        + "to edit this record, click the Save button again. Otherwise click the "
                        + "Back to List hyperlink.");

                    department.RowVersion = databaseValues.RowVersion;
                    ModelState.Remove("RowVersion");
                }
            }
        }

        PopulateAdministratorsDropDownList(department.InstructorID);
        return View(department);
    }

    // GET: Departments/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id, bool? concurrencyError)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var department = await _db.Departments
            .Include(d => d.Administrator)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepartmentID == id);

        if (department == null)
        {
            if (concurrencyError.GetValueOrDefault())
            {
                return RedirectToAction(nameof(Index));
            }
            return NotFound();
        }

        if (concurrencyError.GetValueOrDefault())
        {
            ViewBag.ConcurrencyErrorMessage =
                "The record you attempted to delete was modified by another user after "
                + "you got the original values. The delete operation was canceled and the "
                + "current values in the database have been displayed. If you still want to "
                + "delete this record, click the Delete button again.";
        }

        return View(department);
    }

    // POST: Departments/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Department department)
    {
        try
        {
            if (await _db.Departments.AnyAsync(d => d.DepartmentID == department.DepartmentID))
            {
                _db.Entry(department).State = EntityState.Deleted;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            return RedirectToAction(nameof(Delete),
                new { id = department.DepartmentID, concurrencyError = true });
        }
    }

    private void PopulateAdministratorsDropDownList(object selectedInstructor = null)
    {
        var instructors = _db.Instructors
            .AsNoTracking()
            .OrderBy(i => i.LastName)
            .ToList();
        ViewBag.InstructorID = new SelectList(instructors, "ID", "FullName", selectedInstructor);
    }
}
