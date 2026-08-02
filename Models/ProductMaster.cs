using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public class ProductMaster
    {
        [Key]
        public int ProductId { get; set; }

        [Required, MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public ProductCategory? Category { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public decimal Width { get; set; }
        public decimal Length { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }

        public int MinStock { get; set; }
        public int MaxStock { get; set; }
    }
}
