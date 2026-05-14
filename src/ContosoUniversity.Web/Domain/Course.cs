#nullable disable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Web.Domain
{
    /// <summary>
    /// Course entity ported verbatim from src/ContosoUniversity/Models/Course.cs (legacy MVC 5).
    /// Schema parity is mandatory: column names, types, lengths, and constraints must match the
    /// legacy Code-First model so the rewrite reads/writes the same database during co-existence.
    /// </summary>
    public class Course
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "Number")]
        public int CourseID { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string Title { get; set; }

        [Range(0, 5)]
        public int Credits { get; set; }

        public int DepartmentID { get; set; }

        [Display(Name = "Teaching Material Image")]
        [StringLength(255)]
        public string TeachingMaterialImagePath { get; set; }

        public virtual Department Department { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
        public virtual ICollection<CourseAssignment> CourseAssignments { get; set; }
    }
}
