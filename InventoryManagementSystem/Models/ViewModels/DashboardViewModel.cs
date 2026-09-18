using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }

        public int TotalCategories { get; set; }

        public int TotalSuppliers { get; set; }

        public int TotalPurchases { get; set; }

        public int TotalSales { get; set; }

        public int LowStockProducts { get; set; }


        public decimal TotalPurchaseAmount { get; set; }

        public decimal TotalSalesAmount { get; set; }

        public List<string> CategoryNames { get; set; } = new();

        public List<int> ProductCounts { get; set; } = new();

        public List<Product> LowStockItems { get; set; } = new();

        public List<Purchase> LatestPurchases { get; set; } = new();

        public List<Sale> LatestSales { get; set; } = new();
    }
}