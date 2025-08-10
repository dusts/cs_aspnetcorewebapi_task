using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS_aspnetcorewebapidevtask_1.Models
{
    public class ProductAudit
    {
        public int Id { get; set; }

        [Required]
        public string Operation { get; set; } = string.Empty; // e.g., "Created", "Updated", "Deleted"

        [Required]
        public int ProductId { get; set; } // Reference to the affected product

        [Required]
        public string ChangedData { get; set; } = string.Empty; // JSON or text summary of changes

        [Required]
        public string UserId { get; set; } = string.Empty; // ID of the user who made the change

        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // Navigation property to User

        [Required]
        public DateTime ChangedAt { get; set; } // Timestamp of the change
    }
}
