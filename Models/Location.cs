using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WH_Logistic.Models
{
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        [Required, MaxLength(20)]
        public string BinCode { get; set; } = string.Empty; // e.g. A-01-02

        public int ZoneId { get; set; }
        [ForeignKey("ZoneId")]
        public Zone? Zone { get; set; }

        public int Capacity { get; set; } = 100; // max items this bin can hold

        public ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();
    }
}
