using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sales
        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Product)
                .ToListAsync();

            return View(sales);
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sale = await _context.Sales
                .Include(s => s.Product)
                .FirstOrDefaultAsync(m => m.SaleId == id);
            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        // GET: Sales/Create

        [Authorize(Roles = AppRoles.AdminOrSales)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View();
        }

        // POST: Sales/Create 

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrSales)]
        public async Task<IActionResult> Create([Bind("SaleId,ProductId,Quantity,SalePrice,SaleDate")] Sale sale)
        {
            if (sale.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be greater than zero.");
            }

            if (sale.SalePrice <= 0)
            {
                ModelState.AddModelError("SalePrice", "Sale price must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == sale.ProductId);

                if (product == null)
                {
                    return NotFound();
                }

                // Check available stock
                if (sale.Quantity > product.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Not enough stock available.");

                    ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

                    return View(sale);
                }

                // Decrease stock
                product.Quantity -= sale.Quantity;

                _context.Sales.Add(sale);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Sale recorded successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View(sale);
        }

        // GET: Sales/Edit/5
        [Authorize(Roles = AppRoles.AdminOrSales)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
            {
                return NotFound();
            }
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View(sale);
        }

        // POST: Sales/Edit/5 

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrSales)]
        public async Task<IActionResult> Edit(int id, [Bind("SaleId,ProductId,Quantity,SalePrice,SaleDate")] Sale sale)
        {
            if (id != sale.SaleId)
            {
                return NotFound();
            }

            if (sale.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be greater than zero.");
            }

            if (sale.SalePrice <= 0)
            {
                ModelState.AddModelError("SalePrice", "Sale price must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the old sale
                    var oldSale = await _context.Sales
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.SaleId == sale.SaleId);

                    if (oldSale == null)
                    {
                        return NotFound();
                    }


                    // Get the product
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductId == sale.ProductId);

                    if (product == null)
                    {
                        return NotFound();
                    }


                    // Restore old quantity then subtract new quantity
                    var adjustedQuantity = product.Quantity + oldSale.Quantity - sale.Quantity;


                    // Prevent negative stock
                    if (adjustedQuantity < 0)
                    {
                        ModelState.AddModelError("Quantity", "Not enough stock available.");

                        sale.Product = product;
                        ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

                        return View(sale);
                    }

                    product.Quantity = adjustedQuantity;


                    _context.Update(sale);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sale updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SaleExists(sale.SaleId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

            return View(sale);
        }

        // GET: Sales/Delete/5
        [Authorize(Roles = AppRoles.AdminOrSales)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sale = await _context.Sales
                .Include(s => s.Product)
                .FirstOrDefaultAsync(m => m.SaleId == id);
            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        // POST: Sales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrSales)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sale = await _context.Sales
                .FirstOrDefaultAsync(s => s.SaleId == id);

            if (sale != null)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == sale.ProductId);

                if (product != null)
                {
                    // Return sold quantity to stock
                    product.Quantity += sale.Quantity;
                }

                _context.Sales.Remove(sale);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sale deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SaleExists(int id)
        {
            return _context.Sales.Any(e => e.SaleId == id);
        }
    }
}
