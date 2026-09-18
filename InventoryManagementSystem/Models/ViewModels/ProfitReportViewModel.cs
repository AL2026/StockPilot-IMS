using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class ProfitReportViewModel
    {
        public decimal TotalRevenue { get; set; }

        public decimal TotalCost { get; set; }

        public decimal TotalProfit { get; set; }


        public List<ProductProfit> Products { get; set; } = new();
    }


    public class ProductProfit
    {
        public string ProductName { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        public decimal Revenue { get; set; }

        public decimal Cost { get; set; }

        public decimal Profit { get; set; }
    }
}