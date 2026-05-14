#nullable disable
using System.Collections.Generic;
using ContosoUniversity.Web.Domain;

namespace ContosoUniversity.Web.ViewModels
{
    /// <summary>
    /// rw-006 (F-003): Composite ViewModel for the Instructors/Index cascading drill-down
    /// (Instructors -> Courses -> Enrollments). Ported from
    /// src/ContosoUniversity/Models/SchoolViewModels/InstructorIndexData.cs (legacy MVC 5).
    /// </summary>
    public class InstructorIndexData
    {
        public IEnumerable<Instructor> Instructors { get; set; }
        public IEnumerable<Course> Courses { get; set; }
        public IEnumerable<Enrollment> Enrollments { get; set; }
    }
}
