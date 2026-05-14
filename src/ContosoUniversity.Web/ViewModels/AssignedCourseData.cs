#nullable disable
namespace ContosoUniversity.Web.ViewModels
{
    /// <summary>
    /// rw-006 (F-003): per-row ViewModel for the Create/Edit course-assignment checkbox grid.
    /// Ported from src/ContosoUniversity/Models/SchoolViewModels/AssignedCourseData.cs (legacy MVC 5).
    /// </summary>
    public class AssignedCourseData
    {
        public int CourseID { get; set; }
        public string Title { get; set; }
        public bool Assigned { get; set; }
    }
}
