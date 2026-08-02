using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public enum CycleCountStatus
    {
        Pending,
        InProgress,
        Completed
    }

    public class CycleCount
    {
        [Key]
        public int CycleCountId { get; set; }

        [Required, MaxLength(50)]
        public string CountNumber { get; set; } = string.Empty;

        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Location? Location { get; set; }

        public DateTime CountDate { get; set; } = DateTime.UtcNow;

        public int CountByUserId { get; set; }
        [ForeignKey("CountByUserId")]
        public AppUser? CountBy { get; set; }

        public CycleCountStatus Status { get; set; } = CycleCountStatus.Pending;

        public ICollection<CycleCountItem> Items { get; set; } = new List<CycleCountItem>();
    }

    public class CycleCountItem
    {
        [Key]
        public int CountItemId { get; set; }

        public int CycleCountId { get; set; }
        [ForeignKey("CycleCountId")]
        public CycleCount? CycleCount { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public ProductMaster? Product { get; set; }

        public int SystemQty { get; set; }
        public int ActualQty { get; set; }
        public int Variance => ActualQty - SystemQty;

        public bool IsAdjusted { get; set; }
    }
}
