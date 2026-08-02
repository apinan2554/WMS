using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public class StockTransfer
    {
        [Key]
        public int TransferId { get; set; }

        [Required, MaxLength(50)]
        public string TransferNumber { get; set; } = string.Empty;

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public ProductMaster? Product { get; set; }

        public int FromLocationId { get; set; }
        [ForeignKey("FromLocationId")]
        public Location? FromLocation { get; set; }

        public int ToLocationId { get; set; }
        [ForeignKey("ToLocationId")]
        public Location? ToLocation { get; set; }

        public int Quantity { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        public int TransferByUserId { get; set; }
        [ForeignKey("TransferByUserId")]
        public AppUser? TransferBy { get; set; }
    }
}
