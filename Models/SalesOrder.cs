using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public enum OrderStatus
    {
        Pending,
        PickingInProgress,
        Picked,
        Packed,
        Shipped
    }

    public class SalesOrder
    {
        [Key]
        public int OrderId { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? CustomerName { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
        public ICollection<PickingList> PickingLists { get; set; } = new List<PickingList>();
    }

    public class SalesOrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public SalesOrder? SalesOrder { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public ProductMaster? Product { get; set; }

        public int Quantity { get; set; }
        public int PickedQty { get; set; }
    }
}
