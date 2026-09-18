using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class PurchaseReportViewModel
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public decimal TotalPurchaseAmount { get; set; }

        public int TotalTransactions { get; set; }

        public decimal AveragePurchase { get; set; }

        public decimal HighestPurchase { get; set; }

        public decimal LowestPurchase { get; set; }

        public List<Purchase> Purchases { get; set; } = new();
    }
}