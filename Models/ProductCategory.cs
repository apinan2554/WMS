using System.ComponentModel.DataAnnotations;

namespace WH_Logistic.Models
{
    public class ProductCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public ICollection<ProductMaster> Products { get; set; } = new List<ProductMaster>();
        public ICollection<Zone> Zones { get; set; } = new List<Zone>();
    }
}
