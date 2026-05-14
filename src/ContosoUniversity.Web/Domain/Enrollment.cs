#nullable disable
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Web.Domain
{
    public enum Grade
    {
        A, B, C, D, F
    }

    /// <summary>
    /// Enrollment entity ported from src/ContosoUniversity/Models/Enrollment.cs (legacy MVC 5).
    /// </summary>
    public class Enrollment
    {
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public int StudentID { get; set; }

        [DisplayFormat(NullDisplayText = "No grade")]
        public Grade? Grade { get; set; }

        public virtual Course Course { get; set; }
        public virtual Student Student { get; set; }
    }
}
