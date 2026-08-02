using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Module { get; set; }

        [MaxLength(500)]
        public string? Details { get; set; }

        [MaxLength(50)]
        public string? LocationCode { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
