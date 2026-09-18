using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace InventoryManagementSystem.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        //----Category----
        // Foreign Key
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        // Navigation Property
        public Category? Category { get; set; }

        //----Supplier----
        // Foreign Key
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        // Navigation Property
        public Supplier? Supplier { get; set; }

        //Purchase
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}

//ProductId 
//Name 
//Description 
//Price
//Quantity 
//CategoryId its FK
