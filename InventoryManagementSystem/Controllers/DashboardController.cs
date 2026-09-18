using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models.ViewModels;

namespace InventoryManagementSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var dashboard = new DashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(),

                TotalCategories = await _context.Categories.CountAsync(),

                TotalSuppliers = await _context.Suppliers.CountAsync(),

                TotalPurchases = await _context.Purchases.CountAsync(),

                TotalSales = await _context.Sales.CountAsync(),




                TotalPurchaseAmount = await _context.Purchases
                .SumAsync(p => p.PurchasePrice * p.Quantity),

                LowStockProducts = await _context.Products
                .Where(p => p.Quantity <= 5)
                .CountAsync(),

                LowStockItems = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Quantity <= 5)
                .ToListAsync(),

                LatestPurchases = await _context.Purchases
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(5)
                .ToListAsync(),

                LatestSales = await _context.Sales
                .Include(s => s.Product)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync(),

                CategoryNames = await _context.Categories
                .Select(c => c.Name)
                .ToListAsync(),

                ProductCounts = await _context.Categories
                .Select(c => _context.Products.Count(p => p.CategoryId == c.CategoryId))
                .ToListAsync(),

                TotalSalesAmount = await _context.Sales
                .SumAsync(s => s.SalePrice * s.Quantity)





            };


            return View(dashboard);
        }
    }
}