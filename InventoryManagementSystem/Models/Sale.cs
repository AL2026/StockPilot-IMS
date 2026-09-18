using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Sale
    {
        public int SaleId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        [Range(1, 100000, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000)]
        public decimal SalePrice { get; set; }

        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; } = DateTime.Now;
    }
}