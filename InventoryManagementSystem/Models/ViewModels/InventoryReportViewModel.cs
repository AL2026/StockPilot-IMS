using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class InventoryReportViewModel
    {
        public List<Product> Products { get; set; } = new();

        public decimal TotalInventoryValue { get; set; }

        public int TotalProducts { get; set; }

        public int LowStockProducts { get; set; }

        public int OutOfStockProducts { get; set; }
    }
}