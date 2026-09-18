using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        // Navigation Property
        public ICollection<Product> Products { get; set; } = new List<Product>();

        //Purchases 
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}

//SupplierId 
//Name 
//Phone 
//Email
//Address 
