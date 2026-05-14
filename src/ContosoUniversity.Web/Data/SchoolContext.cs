#nullable disable
using System;
using System.Linq;
using ContosoUniversity.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Web.Data
{
    /// <summary>
    /// EF Core 8 DbContext ported from src/ContosoUniversity/Data/SchoolContext.cs (legacy EF Core 3.1.32).
    /// Schema is preserved verbatim so the rewrite can read/write the same database during co-existence
    /// (legacy app at https://localhost:44300, rewrite at http://localhost:7000, both bound to
    /// (localdb)\MSSQLLocalDB.ContosoUniversity).
    /// </summary>
    public class SchoolContext : DbContext
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<OfficeAssignment> OfficeAssignments { get; set; }
        public DbSet<CourseAssignment> CourseAssignments { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure all DateTime properties to use datetime2 (matches legacy convention).
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasColumnType("datetime2");
                }
            }

            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");
            modelBuilder.Entity<CourseAssignment>().ToTable("CourseAssignment");
            modelBuilder.Entity<Notification>().ToTable("Notification");

            // Table-per-Hierarchy (TPH) inheritance for Person — same Discriminator column as legacy.
            modelBuilder.Entity<Person>()
                .ToTable("Person")
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Student>("Student")
                .HasValue<Instructor>("Instructor");

            // Composite key for CourseAssignment.
            modelBuilder.Entity<CourseAssignment>()
                .HasKey(c => new { c.CourseID, c.InstructorID });

            // Relationships.
            modelBuilder.Entity<CourseAssignment>()
                .HasOne(m => m.Course)
                .WithMany(t => t.CourseAssignments)
                .HasForeignKey(m => m.CourseID);

            modelBuilder.Entity<CourseAssignment>()
                .HasOne(m => m.Instructor)
                .WithMany(t => t.CourseAssignments)
                .HasForeignKey(m => m.InstructorID);

            // One-to-one Instructor <-> OfficeAssignment.
            modelBuilder.Entity<Instructor>()
                .HasOne(s => s.OfficeAssignment)
                .WithOne(ad => ad.Instructor)
                .HasForeignKey<OfficeAssignment>(ad => ad.InstructorID);
        }
    }
}
