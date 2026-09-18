using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class SalesReportViewModel
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<Sale> Sales { get; set; } = new();

        public int TotalTransactions { get; set; }

        public decimal AverageSale { get; set; }

        public decimal HighestSale { get; set; }

        public decimal LowestSale { get; set; }
    }
}