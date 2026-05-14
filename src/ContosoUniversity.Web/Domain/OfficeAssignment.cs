#nullable disable
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Web.Domain
{
    /// <summary>
    /// OfficeAssignment entity ported from src/ContosoUniversity/Models/OfficeAssignment.cs (legacy MVC 5).
    /// One-to-one with Instructor; primary key is the foreign key InstructorID.
    /// </summary>
    public class OfficeAssignment
    {
        [Key]
        public int InstructorID { get; set; }

        [StringLength(50)]
        [Display(Name = "Office Location")]
        public string Location { get; set; }

        public virtual Instructor Instructor { get; set; }
    }
}
