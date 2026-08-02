using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public class Zone
    {
        [Key]
        public int ZoneId { get; set; }

        [Required, MaxLength(50)]
        public string ZoneCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ZoneName { get; set; } = string.Empty;

        // 1 Zone = 1 Product Category only
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public ProductCategory? Category { get; set; }

        public ICollection<Location> Locations { get; set; } = new List<Location>();
    }
}
