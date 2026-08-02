using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public enum PickingStatus
    {
        Pending,
        InProgress,
        Completed,
        Packed,
        Shipped
    }

    public class PickingList
    {
        [Key]
        public int PickingId { get; set; }

        [Required, MaxLength(50)]
        public string PickingNumber { get; set; } = string.Empty;

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public SalesOrder? SalesOrder { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int? AssignedToUserId { get; set; }
        [ForeignKey("AssignedToUserId")]
        public AppUser? AssignedTo { get; set; }

        public PickingStatus Status { get; set; } = PickingStatus.Pending;

        public ICollection<PickingListItem> Items { get; set; } = new List<PickingListItem>();
    }

    public class PickingListItem
    {
        [Key]
        public int PickingItemId { get; set; }

        public int PickingId { get; set; }
        [ForeignKey("PickingId")]
        public PickingList? PickingList { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public ProductMaster? Product { get; set; }

        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Location? Location { get; set; }

        public int RequiredQty { get; set; }
        public int PickedQty { get; set; }

        public int SortOrder { get; set; } // for optimized picking path
    }
}
