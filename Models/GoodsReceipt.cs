using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public enum GRNStatus
    {
        Pending,
        PartiallyReceived,
        Completed
    }

    public class GoodsReceipt
    {
        [Key]
        public int GRNId { get; set; }

        [Required, MaxLength(50)]
        public string GRNNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PONumber { get; set; }

        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

        public int ReceivedByUserId { get; set; }
        [ForeignKey("ReceivedByUserId")]
        public AppUser? ReceivedBy { get; set; }

        public GRNStatus Status { get; set; } = GRNStatus.Pending;

        public ICollection<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
    }

    public class GoodsReceiptItem
    {
        [Key]
        public int GRNItemId { get; set; }

        public int GRNId { get; set; }
        [ForeignKey("GRNId")]
        public GoodsReceipt? GoodsReceipt { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public ProductMaster? Product { get; set; }

        public int ExpectedQty { get; set; }
        public int ReceivedQty { get; set; }

        public int? PutAwayLocationId { get; set; }
        [ForeignKey("PutAwayLocationId")]
        public Location? PutAwayLocation { get; set; }

        public bool IsPutAway { get; set; }
    }
}
