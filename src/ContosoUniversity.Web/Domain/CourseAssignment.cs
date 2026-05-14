#nullable disable
namespace ContosoUniversity.Web.Domain
{
    /// <summary>
    /// CourseAssignment entity ported from src/ContosoUniversity/Models/CourseAssignment.cs (legacy MVC 5).
    /// Composite key on (CourseID, InstructorID) configured in SchoolContext.OnModelCreating.
    /// </summary>
    public class CourseAssignment
    {
        public int InstructorID { get; set; }
        public int CourseID { get; set; }
        public virtual Instructor Instructor { get; set; }
        public virtual Course Course { get; set; }
    }
}
