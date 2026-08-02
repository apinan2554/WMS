using System.ComponentModel.DataAnnotations;

namespace WH_Logistic.Models
{
    public enum UserRole
    {
        Admin,
        WarehouseStaff,
        TransportStaff,
        InboundStaff,
        OutboundStaff,
        FactoryManager
    }

    public class AppUser
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
