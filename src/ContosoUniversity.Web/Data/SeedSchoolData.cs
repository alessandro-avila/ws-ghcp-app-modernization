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
/// rw-003 (F-004): seeds a deterministic minimal Instructor row so the
/// Departments/Create + Departments/Edit Administrator dropdown is non-trivial
/// in fresh dev/test databases.
///
/// Idempotent — checks for an existing well-known seed marker (last name
/// "Seed") before inserting. Safe to call on every host start.
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

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<SchoolContext>>();
        var db = services.GetRequiredService<SchoolContext>();

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
}
