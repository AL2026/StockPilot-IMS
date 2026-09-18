using System.Text;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------- Sales Report ----------

        public async Task<IActionResult> SalesReport(DateTime? startDate, DateTime? endDate)
        {
            var salesList = await GetFilteredSalesAsync(startDate, endDate);

            var model = new SalesReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Sales = salesList,

                TotalRevenue = salesList.Sum(s => s.SalePrice * s.Quantity),

                TotalTransactions = salesList.Count,

                AverageSale = salesList.Any()
                    ? salesList.Average(s => s.SalePrice * s.Quantity)
                    : 0,

                HighestSale = salesList.Any()
                    ? salesList.Max(s => s.SalePrice * s.Quantity)
                    : 0,

                LowestSale = salesList.Any()
                    ? salesList.Min(s => s.SalePrice * s.Quantity)
                    : 0
            };

            return View(model);
        }

        public async Task<IActionResult> ExportSalesReportCsv(DateTime? startDate, DateTime? endDate)
        {
            var salesList = await GetFilteredSalesAsync(startDate, endDate);

            var sb = new StringBuilder();
            sb.AppendLine("Product,Quantity,Sale Price,Total,Date");

            foreach (var sale in salesList)
            {
                sb.AppendLine(string.Join(",",
                    CsvField(sale.Product?.Name ?? "Unknown"),
                    CsvField(sale.Quantity.ToString()),
                    CsvField(sale.SalePrice.ToString("F2")),
                    CsvField((sale.SalePrice * sale.Quantity).ToString("F2")),
                    CsvField(sale.SaleDate.ToString("yyyy-MM-dd"))
                ));
            }

            return CsvFile(sb.ToString(), "sales-report");
        }

        private async Task<List<Sale>> GetFilteredSalesAsync(DateTime? startDate, DateTime? endDate)
        {
            var sales = _context.Sales
                .Include(s => s.Product)
                .AsQueryable();

            if (startDate.HasValue)
            {
                sales = sales.Where(s => s.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                sales = sales.Where(s => s.SaleDate <= endDate.Value);
            }

            return await sales
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        // ---------- Purchase Report ----------

        public async Task<IActionResult> PurchaseReport(DateTime? startDate, DateTime? endDate)
        {
            var purchaseList = await GetFilteredPurchasesAsync(startDate, endDate);

            var model = new PurchaseReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Purchases = purchaseList,

                TotalPurchaseAmount = purchaseList.Sum(p => p.PurchasePrice * p.Quantity),

                TotalTransactions = purchaseList.Count,

                AveragePurchase = purchaseList.Any()
                    ? purchaseList.Average(p => p.PurchasePrice * p.Quantity)
                    : 0,

                HighestPurchase = purchaseList.Any()
                    ? purchaseList.Max(p => p.PurchasePrice * p.Quantity)
                    : 0,

                LowestPurchase = purchaseList.Any()
                    ? purchaseList.Min(p => p.PurchasePrice * p.Quantity)
                    : 0
            };

            return View(model);
        }

        public async Task<IActionResult> ExportPurchaseReportCsv(DateTime? startDate, DateTime? endDate)
        {
            var purchaseList = await GetFilteredPurchasesAsync(startDate, endDate);

            var sb = new StringBuilder();
            sb.AppendLine("Product,Supplier,Quantity,Unit Price,Total,Date");

            foreach (var purchase in purchaseList)
            {
                sb.AppendLine(string.Join(",",
                    CsvField(purchase.Product?.Name ?? "Unknown"),
                    CsvField(purchase.Supplier?.Name ?? "Unknown"),
                    CsvField(purchase.Quantity.ToString()),
                    CsvField(purchase.PurchasePrice.ToString("F2")),
                    CsvField((purchase.PurchasePrice * purchase.Quantity).ToString("F2")),
                    CsvField(purchase.PurchaseDate.ToString("yyyy-MM-dd"))
                ));
            }

            return CsvFile(sb.ToString(), "purchase-report");
        }

        private async Task<List<Purchase>> GetFilteredPurchasesAsync(DateTime? startDate, DateTime? endDate)
        {
            var purchases = _context.Purchases
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (startDate.HasValue)
            {
                purchases = purchases.Where(p => p.PurchaseDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                purchases = purchases.Where(p => p.PurchaseDate <= endDate.Value);
            }

            return await purchases
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
        }

        // ---------- Inventory Report ----------

        public async Task<IActionResult> InventoryReport(string searchString)
        {
            var productList = await GetFilteredProductsAsync(searchString);

            var model = new InventoryReportViewModel
            {
                Products = productList,

                TotalProducts = productList.Count,

                LowStockProducts = productList.Count(p => p.Quantity > 0 && p.Quantity <= 5),

                OutOfStockProducts = productList.Count(p => p.Quantity == 0),

                TotalInventoryValue = productList.Sum(p => p.Price * p.Quantity)
            };

            ViewData["CurrentFilter"] = searchString;

            return View(model);
        }

        public async Task<IActionResult> ExportInventoryReportCsv(string searchString)
        {
            var productList = await GetFilteredProductsAsync(searchString);

            var sb = new StringBuilder();
            sb.AppendLine("Product,Category,Supplier,Price,Quantity,Total Value");

            foreach (var product in productList)
            {
                sb.AppendLine(string.Join(",",
                    CsvField(product.Name),
                    CsvField(product.Category?.Name ?? "Unknown"),
                    CsvField(product.Supplier?.Name ?? "Unknown"),
                    CsvField(product.Price.ToString("F2")),
                    CsvField(product.Quantity.ToString()),
                    CsvField((product.Price * product.Quantity).ToString("F2"))
                ));
            }

            return CsvFile(sb.ToString(), "inventory-report");
        }

        private async Task<List<Product>> GetFilteredProductsAsync(string searchString)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString));
            }

            return await products.ToListAsync();
        }

        // ---------- Profit Report ----------

        public async Task<IActionResult> ProfitReport()
        {
            var model = await BuildProfitReportAsync();
            return View(model);
        }

        public async Task<IActionResult> ExportProfitReportCsv()
        {
            var model = await BuildProfitReportAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Product,Quantity Sold,Revenue,Cost,Profit");

            foreach (var item in model.Products)
            {
                sb.AppendLine(string.Join(",",
                    CsvField(item.ProductName),
                    CsvField(item.QuantitySold.ToString()),
                    CsvField(item.Revenue.ToString("F2")),
                    CsvField(item.Cost.ToString("F2")),
                    CsvField(item.Profit.ToString("F2"))
                ));
            }

            return CsvFile(sb.ToString(), "profit-report");
        }

        private async Task<ProfitReportViewModel> BuildProfitReportAsync()
        {
            var sales = await _context.Sales
                .Include(s => s.Product)
                .ToListAsync();

            var purchases = await _context.Purchases
                .Include(p => p.Product)
                .ToListAsync();

            var totalRevenue = sales.Sum(s => s.SalePrice * s.Quantity);
            var totalCost = purchases.Sum(p => p.PurchasePrice * p.Quantity);

            // Grouped by ProductId (not Name) so two different products that happen to
            // share a name don't have their profit numbers silently merged.
            var productProfit = sales
                .GroupBy(s => s.ProductId)
                .Select(g =>
                {
                    var productName = g.First().Product?.Name ?? "Unknown Product";
                    var revenue = g.Sum(x => x.SalePrice * x.Quantity);
                    var cost = purchases
                        .Where(p => p.ProductId == g.Key)
                        .Sum(p => p.PurchasePrice * p.Quantity);

                    return new ProductProfit
                    {
                        ProductName = productName,
                        QuantitySold = g.Sum(x => x.Quantity),
                        Revenue = revenue,
                        Cost = cost,
                        Profit = revenue - cost
                    };
                })
                .ToList();

            return new ProfitReportViewModel
            {
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                TotalProfit = totalRevenue - totalCost,
                Products = productProfit
            };
        }

        // ---------- CSV helpers ----------

  
        private static string CsvField(string value)
        {
            value ??= string.Empty;

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private FileContentResult CsvFile(string csvContent, string fileNamePrefix)
        {
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            var fileName = $"{fileNamePrefix}-{DateTime.Now:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
}
