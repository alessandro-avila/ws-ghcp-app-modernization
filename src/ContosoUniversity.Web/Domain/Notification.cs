#nullable disable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Web.Domain
{
    /// <summary>
    /// Notification entity ported from src/ContosoUniversity/Models/Notification.cs (legacy MVC 5).
    /// Records CRUD events on Course/Department/Student/Instructor for the real-time dashboard (F-006).
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityId { get; set; }

        [Required]
        [StringLength(20)]
        public string Operation { get; set; } // CREATE, UPDATE, DELETE

        [Required]
        [StringLength(256)]
        public string Message { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; }

        public bool IsRead { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? ReadAt { get; set; }
    }

    public enum EntityOperation
    {
        CREATE,
        UPDATE,
        DELETE
    }
}
