using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Purchase
    {
        public int PurchaseId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        public int SupplierId { get; set; }

        public Supplier? Supplier { get; set; }

        [Required]
        [Range(1, 100000, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000)]
        public decimal PurchasePrice { get; set; }

        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

    }
}