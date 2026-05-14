using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Web.Models.SchoolViewModels;

// Ported from src/ContosoUniversity/Models/SchoolViewModels/EnrollmentDateGroup.cs
// (legacy MVC 5). Used by HomeController.About to project Student counts grouped
// by EnrollmentDate. Nullable DateTime preserves legacy view-model shape and is
// implicitly assignable from the non-nullable Domain.Student.EnrollmentDate.
public class EnrollmentDateGroup
{
    [DataType(DataType.Date)]
    public DateTime? EnrollmentDate { get; set; }

    public int StudentCount { get; set; }
}
