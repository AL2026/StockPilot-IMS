using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

//---Category---
//CategoryId
//Name 
//Description 