#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.Web.Data;
using ContosoUniversity.Web.Domain;
using ContosoUniversity.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// rw-006 (F-003): InstructorsController ported from
/// <c>src/ContosoUniversity/Controllers/InstructorsController.cs</c> (legacy MVC 5).
///
/// Largest single controller rewrite per increment-plan.md rw-006. Three model classes touched:
///   - <see cref="Instructor"/> (TPH discriminator on Person table)
///   - <see cref="OfficeAssignment"/> (1:1 optional, PK == FK == InstructorID; shared-PK)
///   - <see cref="CourseAssignment"/> (composite PK on CourseID+InstructorID; M:M Instructor/Course)
///
/// Cascading drill-down (selected Instructor -&gt; their Courses -&gt; per-course Enrollments)
/// implemented server-side via the <see cref="InstructorIndexData"/> ViewModel and two
/// optional query-string parameters (id, courseID) — same UX as legacy.
///
/// Authorization (ADR-006):
///   - Class-level <c>[Authorize(Roles="Admin,Reader")]</c> gates Index + Details
///     so anonymous requests redirect to /Account/SignIn (cookie auth challenge).
///   - Action-level <c>[Authorize(Roles="Admin")]</c> on Create/Edit/Delete makes
///     reader-role POSTs return 403 Forbidden BEFORE the antiforgery filter (per ADR-006);
///     this is intentional and tested by the reader-create scenarios.
///
/// Notifications: legacy <c>NotificationService</c> calls intentionally NOT ported
/// in rw-006 (defer to rw-007 with the NotificationsController + Channel&lt;T&gt; infra).
/// </summary>
[Authorize(Roles = "Admin,Reader")]
public class InstructorsController : Controller
{
    private readonly SchoolContext _db;
    private readonly ILogger<InstructorsController> _logger;

    public InstructorsController(SchoolContext db, ILogger<InstructorsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET: Instructors
    // GET: Instructors?id=5
    // GET: Instructors?id=5&courseID=99001
    // Cascading drill-down: optional `id` selects an Instructor (loads their Courses);
    // optional `courseID` selects a Course (loads its Enrollments).
    public async Task<IActionResult> Index(int? id, int? courseID)
    {
        var viewModel = new InstructorIndexData
        {
            Instructors = await _db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)
                    .ThenInclude(ca => ca.Course)
                        .ThenInclude(c => c.Department)
                .AsNoTracking()
                .OrderBy(i => i.LastName)
                .ToListAsync()
                .ConfigureAwait(false)
        };

        if (id != null)
        {
            ViewBag.InstructorID = id.Value;
            var selected = viewModel.Instructors.FirstOrDefault(i => i.ID == id.Value);
            if (selected != null)
            {
                viewModel.Courses = selected.CourseAssignments.Select(ca => ca.Course);
            }
        }

        if (courseID != null && viewModel.Courses != null)
        {
            ViewBag.CourseID = courseID.Value;
            // Load Enrollments fresh (the Index query did not eager-load them).
            viewModel.Enrollments = await _db.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseID == courseID.Value)
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
        }

        return View(viewModel);
    }

    // GET: Instructors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var instructor = await _db.Instructors
            .Include(i => i.OfficeAssignment)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ID == id);

        if (instructor == null)
        {
            return NotFound();
        }

        return View(instructor);
    }

    // GET: Instructors/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var instructor = new Instructor
        {
            CourseAssignments = new List<CourseAssignment>()
        };
        await PopulateAssignedCourseDataAsync(instructor).ConfigureAwait(false);
        return View(instructor);
    }

    // POST: Instructors/Create
    // Reader-role POSTs return 403 BEFORE antiforgery validation (per ADR-006).
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("LastName,FirstMidName,HireDate,OfficeAssignment")] Instructor instructor,
        string[] selectedCourses,
        CancellationToken cancellationToken)
    {
        if (selectedCourses != null)
        {
            instructor.CourseAssignments = new List<CourseAssignment>();
            foreach (var course in selectedCourses)
            {
                if (int.TryParse(course, out var courseId))
                {
                    instructor.CourseAssignments.Add(new CourseAssignment
                    {
                        InstructorID = instructor.ID,
                        CourseID = courseId
                    });
                }
            }
        }

        // Drop OfficeAssignment if Location is blank (legacy parity: empty office means no row).
        if (instructor.OfficeAssignment != null
            && string.IsNullOrWhiteSpace(instructor.OfficeAssignment.Location))
        {
            instructor.OfficeAssignment = null;
        }

        if (ModelState.IsValid)
        {
            _db.Instructors.Add(instructor);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return RedirectToAction(nameof(Index));
        }

        await PopulateAssignedCourseDataAsync(instructor).ConfigureAwait(false);
        return View(instructor);
    }

    // GET: Instructors/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var instructor = await _db.Instructors
            .Include(i => i.OfficeAssignment)
            .Include(i => i.CourseAssignments)
                .ThenInclude(ca => ca.Course)
            .FirstOrDefaultAsync(i => i.ID == id);

        if (instructor == null)
        {
            return NotFound();
        }

        await PopulateAssignedCourseDataAsync(instructor).ConfigureAwait(false);
        return View(instructor);
    }

    // POST: Instructors/Edit/5
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        string[] selectedCourses,
        CancellationToken cancellationToken)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var instructorToUpdate = await _db.Instructors
            .Include(i => i.OfficeAssignment)
            .Include(i => i.CourseAssignments)
                .ThenInclude(ca => ca.Course)
            .FirstOrDefaultAsync(i => i.ID == id);

        if (instructorToUpdate == null)
        {
            return NotFound();
        }

        if (await TryUpdateModelAsync(
                instructorToUpdate,
                string.Empty,
                i => i.LastName, i => i.FirstMidName, i => i.HireDate, i => i.OfficeAssignment))
        {
            try
            {
                // Legacy parity: blank Location means delete the OfficeAssignment row.
                if (instructorToUpdate.OfficeAssignment != null
                    && string.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment.Location))
                {
                    instructorToUpdate.OfficeAssignment = null;
                }

                UpdateInstructorCourses(selectedCourses, instructorToUpdate);

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "rw-006: failed to save Instructor edit for ID={InstructorID}.",
                    id);
                ModelState.AddModelError(string.Empty,
                    "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
        }

        await PopulateAssignedCourseDataAsync(instructorToUpdate).ConfigureAwait(false);
        return View(instructorToUpdate);
    }

    // GET: Instructors/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var instructor = await _db.Instructors
            .Include(i => i.OfficeAssignment)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ID == id);

        if (instructor == null)
        {
            return NotFound();
        }

        return View(instructor);
    }

    // POST: Instructors/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var instructor = await _db.Instructors
            .Include(i => i.OfficeAssignment)
            .Include(i => i.CourseAssignments)
            .FirstOrDefaultAsync(i => i.ID == id);

        if (instructor == null)
        {
            return RedirectToAction(nameof(Index));
        }

        // Nullify Department.InstructorID for any administered department BEFORE removing
        // the Instructor (Department.InstructorID is a nullable FK to Person.ID).
        var administeredDepartments = await _db.Departments
            .Where(d => d.InstructorID == id)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var dept in administeredDepartments)
        {
            dept.InstructorID = null;
        }

        _db.Instructors.Remove(instructor);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// rw-006: Populate ViewBag.Courses with the per-row checkbox grid for Create/Edit.
    /// </summary>
    private async Task PopulateAssignedCourseDataAsync(Instructor instructor)
    {
        var allCourses = await _db.Courses
            .AsNoTracking()
            .OrderBy(c => c.CourseID)
            .ToListAsync()
            .ConfigureAwait(false);

        var instructorCourses = new HashSet<int>(
            instructor.CourseAssignments?.Select(ca => ca.CourseID) ?? Enumerable.Empty<int>());

        var viewModel = allCourses.Select(course => new AssignedCourseData
        {
            CourseID = course.CourseID,
            Title = course.Title,
            Assigned = instructorCourses.Contains(course.CourseID)
        }).ToList();

        ViewBag.Courses = viewModel;
    }

    /// <summary>
    /// rw-006: Symmetric-diff sync of CourseAssignment rows for the Edit POST.
    /// null selectedCourses == empty selection (clear all). Ported from legacy MVC 5.
    /// </summary>
    private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
    {
        if (selectedCourses == null)
        {
            instructorToUpdate.CourseAssignments = new List<CourseAssignment>();
            return;
        }

        var selectedCoursesHS = new HashSet<string>(selectedCourses);
        var instructorCourses = new HashSet<int>(
            instructorToUpdate.CourseAssignments.Select(ca => ca.CourseID));

        // Snapshot all courses once (avoid per-row round-trip).
        var allCourses = _db.Courses.AsNoTracking().ToList();

        foreach (var course in allCourses)
        {
            var courseIdString = course.CourseID.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (selectedCoursesHS.Contains(courseIdString))
            {
                if (!instructorCourses.Contains(course.CourseID))
                {
                    instructorToUpdate.CourseAssignments.Add(new CourseAssignment
                    {
                        InstructorID = instructorToUpdate.ID,
                        CourseID = course.CourseID
                    });
                }
            }
            else
            {
                if (instructorCourses.Contains(course.CourseID))
                {
                    var courseToRemove = instructorToUpdate.CourseAssignments
                        .SingleOrDefault(ca => ca.CourseID == course.CourseID);
                    if (courseToRemove != null)
                    {
                        _db.CourseAssignments.Remove(courseToRemove);
                    }
                }
            }
        }
    }
}
