#nullable disable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ContosoUniversity.Web.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Web.Data;

/// <summary>
/// rw-003 (F-004) + rw-004 (F-001) + rw-005 (F-002) + rw-006 (F-003): seeds
/// deterministic dev-only domain rows so the rewrite controllers have non-trivial
/// data to render in fresh dev/test databases.
///
/// Seeds:
///   - 1 Instructor ("Seed, Rewrite") - rw-003: Administrator dropdown source
///     for Departments/Create + Departments/Edit, AND the Administrator FK for
///     the rw-005 seed Department.
///   - 2 Students ("Alpha, Reader-Sortable", "Zulu, Reader-Sortable") - rw-004:
///     deterministic data for the Students Index sort/filter/paging scenarios.
///     Both share the FirstMidName substring "Reader-Sortable" so the search
///     scenario can use a substring that matches both, and the sort scenario
///     relies on the deterministic LastName order (Alpha &lt; Zulu).
///   - 1 Department ("Rewrite-Seed-Dept-rw005") - rw-005: DepartmentID FK source
///     for the Course Create form's department dropdown. Cucumber resolves the
///     auto-generated DepartmentID at runtime by parsing the dropdown.
///   - 1 Course (CourseID=99001, Title="Reader-Visible Seed Course",
///     Credits=3, TeachingMaterialImagePath set to a deterministic path) -
///     rw-005: provides the row backing AC#2 (Reader Index visibility), AC#3
///     (Reader Details), and AC#10 (Reader Image download). The companion
///     on-disk image file is written next to the row insert so AC#10 has
///     bytes to stream back.
///   - 1 Instructor ("Rewrite-Seed-Instructor-rw006, Rew") - rw-006: backs
///     F-003 master/detail/sub-detail Index scenarios. Linked 1:1 to a seeded
///     OfficeAssignment (Location="Office 99-001") and joined via a single
///     CourseAssignment to the rw-005 seed Course (CourseID=99001). One
///     additional Enrollment row (rw-005 Alpha student against CourseID=99001)
///     gives AC#5 (sub-detail enrollments pane) a non-empty list to render.
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

    // rw-005 (F-002): deterministic Department + Course row for the Courses
    // Index/Details/Image cucumber scenarios. Course.CourseID is
    // [DatabaseGenerated(DatabaseGeneratedOption.None)] so we pin a known
    // integer (99001) in the 90000+ range to avoid colliding with the legacy
    // hand-seeded course IDs (1050..4022).
    public const string SeedDepartmentName = "Rewrite-Seed-Dept-rw005";
    public const decimal SeedDepartmentBudget = 100000m;
    public const int SeedReaderVisibleCourseId = 99001;
    public const string SeedReaderVisibleCourseTitle = "Reader-Visible Seed Course";
    public const int SeedReaderVisibleCourseCredits = 3;

    // The seed teaching-material image lives under App_Data/uploads/... — a
    // NON-web-rooted directory, served only by the [Authorize] Image controller
    // action. The relative path is stored in the Course row; SeedSchoolData
    // also writes a 3-byte placeholder file at the absolute path so the AC#10
    // green-baseline scenario can stream bytes back.
    public const string SeedCourseImageRelativePath =
        "App_Data/uploads/teaching-materials/course_99001_seed.jpg";

    // rw-006 (F-003): deterministic Instructor + OfficeAssignment + CourseAssignment
    // + Enrollment for the master/detail/sub-detail Index cucumber scenarios.
    public const string SeedRw006InstructorLastName = "Rewrite-Seed-Instructor-rw006";
    public const string SeedRw006InstructorFirstName = "Rew";
    public const string SeedRw006OfficeLocation = "Office 99-001";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<SchoolContext>>();
        var db = services.GetRequiredService<SchoolContext>();
        var env = services.GetRequiredService<IWebHostEnvironment>();

        await SeedInstructorAsync(db, logger);
        await SeedStudentsAsync(db, logger);
        await SeedDepartmentAndCourseAsync(db, logger, env);
        await SeedInstructorRw006Async(db, logger);
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

    private static async Task SeedDepartmentAndCourseAsync(
        SchoolContext db, ILogger logger, IWebHostEnvironment env)
    {
        try
        {
            var alreadySeeded = await db.Departments
                .AsNoTracking()
                .AnyAsync(d => d.Name == SeedDepartmentName);

            if (alreadySeeded)
            {
                // Even if the Department row exists, make sure the on-disk
                // teaching-material file is present for AC#10 — the file may
                // have been deleted between runs (e.g. App_Data wiped during
                // a clean checkout). EnsureSeedImageFileExists is idempotent.
                EnsureSeedImageFileExists(env, logger);
                return;
            }

            // Look up the seed Instructor (rw-003) for the Administrator FK.
            // The Person base class exposes the PK as `ID`.
            var instructor = await db.Instructors
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.LastName == SeedInstructorLastName
                                       && i.FirstMidName == SeedInstructorFirstName);

            if (instructor == null)
            {
                logger.LogWarning(
                    "Cannot seed rw-005 Department/Course - the seed Instructor (LastName={LastName}, "
                    + "FirstMidName={FirstMidName}) is missing. SeedInstructorAsync should have run first; "
                    + "the rw-005 cucumber scenarios will fail until SeedInstructorAsync succeeds.",
                    SeedInstructorLastName, SeedInstructorFirstName);
                return;
            }

            var department = new Department
            {
                Name = SeedDepartmentName,
                Budget = SeedDepartmentBudget,
                StartDate = new DateTime(2024, 1, 1),
                InstructorID = instructor.ID
            };
            db.Departments.Add(department);
            await db.SaveChangesAsync();

            // Write the on-disk image file BEFORE inserting the Course row so
            // the AC#10 scenario never sees a row pointing at a missing file.
            EnsureSeedImageFileExists(env, logger);

            db.Courses.Add(new Course
            {
                CourseID = SeedReaderVisibleCourseId,
                Title = SeedReaderVisibleCourseTitle,
                Credits = SeedReaderVisibleCourseCredits,
                DepartmentID = department.DepartmentID,
                TeachingMaterialImagePath = SeedCourseImageRelativePath
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to seed deterministic Department + Course rows for the rw-005 Courses scenarios. "
                + "The rw-005 cucumber scenarios will fail until the seed succeeds.");
        }
    }

    /// <summary>
    /// rw-006 (F-003): seeds an Instructor row plus its 1:1 OfficeAssignment and
    /// a single CourseAssignment to the rw-005 seed Course (CourseID=99001), then
    /// adds one Enrollment row so the Index sub-detail (enrollments) pane has
    /// content to render. Idempotent via a LastName check.
    ///
    /// Depends on SeedDepartmentAndCourseAsync having created the seed Course;
    /// if it didn't, the CourseAssignment + Enrollment inserts are skipped and
    /// only the Instructor + OfficeAssignment rows are written. The cucumber
    /// scenarios for AC#4 (assigned-courses panel) and AC#5 (enrollments panel)
    /// will fail until both seeds succeed together.
    /// </summary>
    private static async Task SeedInstructorRw006Async(SchoolContext db, ILogger logger)
    {
        try
        {
            var alreadySeeded = await db.Instructors
                .AsNoTracking()
                .AnyAsync(i => i.LastName == SeedRw006InstructorLastName);

            if (alreadySeeded)
            {
                return;
            }

            // Insert the Instructor first so the IDENTITY-generated PK is
            // available for the OfficeAssignment shared-PK + CourseAssignment FK.
            var instructor = new Instructor
            {
                LastName = SeedRw006InstructorLastName,
                FirstMidName = SeedRw006InstructorFirstName,
                HireDate = new DateTime(2024, 1, 1)
            };
            db.Instructors.Add(instructor);
            await db.SaveChangesAsync();

            // 1:1 OfficeAssignment (shared-PK with Instructor via InstructorID).
            db.OfficeAssignments.Add(new OfficeAssignment
            {
                InstructorID = instructor.ID,
                Location = SeedRw006OfficeLocation
            });
            await db.SaveChangesAsync();

            // CourseAssignment to the rw-005 seed Course (CourseID=99001) IF
            // that Course exists. If SeedDepartmentAndCourseAsync skipped
            // because of a missing FK, we silently skip the join row too —
            // the rw-006 Index scenarios will fail until both seeds succeed.
            var seedCourseExists = await db.Courses
                .AsNoTracking()
                .AnyAsync(c => c.CourseID == SeedReaderVisibleCourseId);

            if (seedCourseExists)
            {
                db.CourseAssignments.Add(new CourseAssignment
                {
                    InstructorID = instructor.ID,
                    CourseID = SeedReaderVisibleCourseId
                });
                await db.SaveChangesAsync();

                // One Enrollment so the AC#5 sub-detail pane is non-empty.
                // Reuse the rw-004 seed Alpha Student (LastName="Alpha",
                // FirstMidName="Reader-Sortable"). If absent (e.g. seed order
                // changed), skip the Enrollment — AC#5 will fail loudly.
                var alphaStudent = await db.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.LastName == SeedStudentAlphaLastName
                                           && s.FirstMidName == SeedStudentFirstNameMarker);

                if (alphaStudent != null)
                {
                    var enrollmentExists = await db.Enrollments
                        .AsNoTracking()
                        .AnyAsync(e => e.CourseID == SeedReaderVisibleCourseId
                                    && e.StudentID == alphaStudent.ID);

                    if (!enrollmentExists)
                    {
                        db.Enrollments.Add(new Enrollment
                        {
                            CourseID = SeedReaderVisibleCourseId,
                            StudentID = alphaStudent.ID,
                            Grade = Grade.A
                        });
                        await db.SaveChangesAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to seed the rw-006 Instructor (LastName={LastName}). The rw-006 cucumber "
                + "scenarios will fail until the seed succeeds.",
                SeedRw006InstructorLastName);
        }
    }

    private static void EnsureSeedImageFileExists(IWebHostEnvironment env, ILogger logger)
    {
        try
        {
            var absolutePath = Path.Combine(
                env.ContentRootPath,
                SeedCourseImageRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (!File.Exists(absolutePath))
            {
                // 3-byte placeholder. RED-baseline cucumber scenarios do not
                // validate the bytes — only the response status (200) and
                // Content-Disposition header (attachment). Step-3 impl can
                // replace this with valid JPEG bytes if magic-byte download
                // validation is later added.
                File.WriteAllBytes(absolutePath, new byte[] { 0x42, 0x42, 0x42 });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to ensure the rw-005 seed teaching-material file exists at {Path}. "
                + "AC#10 (Reader GET /Courses/Image/{Id}) will fail until the file can be created.",
                SeedCourseImageRelativePath, SeedReaderVisibleCourseId);
        }
    }
}
