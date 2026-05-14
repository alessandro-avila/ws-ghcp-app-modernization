#nullable disable
using System;
using System.Linq;
using System.Threading.Tasks;
using ContosoUniversity.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Web.Data;

/// <summary>
/// rw-003 (F-004) + rw-004 (F-001): seeds deterministic dev-only domain rows so
/// the rewrite controllers have non-trivial data to render in fresh dev/test
/// databases.
///
/// Seeds:
///   - 1 Instructor ("Seed, Rewrite") - rw-003: Administrator dropdown source
///     for Departments/Create + Departments/Edit.
///   - 2 Students ("Alpha, Reader-Sortable", "Zulu, Reader-Sortable") - rw-004:
///     deterministic data for the Students Index sort/filter/paging scenarios.
///     Both share the FirstMidName substring "Reader-Sortable" so the search
///     scenario can use a substring that matches both, and the sort scenario
///     relies on the deterministic LastName order (Alpha < Zulu).
///
/// Idempotent - checks for existing well-known seed markers before inserting,
/// so SeedAsync is safe to call on every host start.
///
/// Dev-only: <see cref="Program"/> guards the call with
/// <c>app.Environment.IsDevelopment()</c>, mirroring <see cref="SeedAuthData"/>.
/// Production data is owned by the legacy app's seeding pipeline; the rewrite
/// reads/writes the same physical schema during co-existence.
/// </summary>
public static class SeedSchoolData
{
    public const string SeedInstructorLastName = "Seed";
    public const string SeedInstructorFirstName = "Rewrite";

    // rw-004 (F-001): deterministic Student rows for the Students Index
    // sort/filter/paging cucumber scenarios.
    public const string SeedStudentFirstNameMarker = "Reader-Sortable";
    public const string SeedStudentAlphaLastName = "Alpha";
    public const string SeedStudentZuluLastName = "Zulu";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<SchoolContext>>();
        var db = services.GetRequiredService<SchoolContext>();

        await SeedInstructorAsync(db, logger);
        await SeedStudentsAsync(db, logger);
    }

    private static async Task SeedInstructorAsync(SchoolContext db, ILogger logger)
    {
        try
        {
            var alreadySeeded = await db.Instructors
                .AsNoTracking()
                .AnyAsync(i => i.LastName == SeedInstructorLastName
                            && i.FirstMidName == SeedInstructorFirstName);

            if (alreadySeeded)
            {
                return;
            }

            db.Instructors.Add(new Instructor
            {
                LastName = SeedInstructorLastName,
                FirstMidName = SeedInstructorFirstName,
                HireDate = new DateTime(2024, 1, 1)
            });

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to seed deterministic Instructor row for the Administrator dropdown. "
                + "Departments/Create + Departments/Edit will fall back to the existing instructor list.");
        }
    }

    private static async Task SeedStudentsAsync(SchoolContext db, ILogger logger)
    {
        try
        {
            // EF Core filters db.Students by the TPH discriminator, so this
            // only counts Student rows (Instructors are not double-counted).
            var alreadySeeded = await db.Students
                .AsNoTracking()
                .AnyAsync(s => s.FirstMidName == SeedStudentFirstNameMarker);

            if (alreadySeeded)
            {
                return;
            }

            db.Students.Add(new Student
            {
                LastName = SeedStudentAlphaLastName,
                FirstMidName = SeedStudentFirstNameMarker,
                EnrollmentDate = new DateTime(2024, 1, 1)
            });
            db.Students.Add(new Student
            {
                LastName = SeedStudentZuluLastName,
                FirstMidName = SeedStudentFirstNameMarker,
                EnrollmentDate = new DateTime(2024, 6, 1)
            });

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to seed deterministic Student rows for the Students Index sort/filter/paging "
                + "scenarios. The rw-004 cucumber scenarios will fail until the seed succeeds.");
        }
    }
}
