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
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }


        // GET: Purchases
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Purchases.Include(p => p.Product).Include(p => p.Supplier);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Purchases/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchase = await _context.Purchases
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);

            if (purchase == null)
            {
                return NotFound();
            }

            return View(purchase);
        }

        // GET: Purchases/Create
        [Authorize(Roles = AppRoles.AdminOrPurchase)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            ViewBag.LastPurchasePrices = await GetLastPurchasePricesAsync();
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Name");
            return View();
        }

        // POST: Purchases/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrPurchase)]
        public async Task<IActionResult> Create([Bind("PurchaseId,ProductId,SupplierId,Quantity,PurchasePrice,PurchaseDate")] Purchase purchase)
        {
            if (purchase.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be greater than zero.");
            }

            if (purchase.PurchasePrice <= 0)
            {
                ModelState.AddModelError("PurchasePrice", "Purchase price must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == purchase.ProductId);

                if (product == null)
                {
                    return NotFound();
                }

                // Increase stock
                product.Quantity += purchase.Quantity;

                _context.Purchases.Add(purchase);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Purchase recorded successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            ViewBag.LastPurchasePrices = await GetLastPurchasePricesAsync();
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Name", purchase.SupplierId);
            return View(purchase);
        }

        // GET: Purchases/Edit/5
        [Authorize(Roles = AppRoles.AdminOrPurchase)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var purchase = await _context.Purchases
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);
            if (purchase == null)
            {
                return NotFound();
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", purchase.ProductId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Name", purchase.SupplierId);
            return View(purchase);
        }

        // POST: Purchases/Edit/5


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrPurchase)]
        public async Task<IActionResult> Edit(int id, [Bind("PurchaseId,ProductId,SupplierId,Quantity,PurchasePrice,PurchaseDate")] Purchase purchase)
        {
            if (id != purchase.PurchaseId)
            {
                return NotFound();
            }

            if (purchase.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be greater than zero.");
            }

            if (purchase.PurchasePrice <= 0)
            {
                ModelState.AddModelError("PurchasePrice", "Purchase price must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original purchase from the database
                    var oldPurchase = await _context.Purchases
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PurchaseId == purchase.PurchaseId);

                    if (oldPurchase == null)
                    {
                        return NotFound();
                    }

                    // Get the product
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductId == purchase.ProductId);

                    if (product == null)
                    {
                        return NotFound();
                    }

                    // Adjust stock
                    var adjustedQuantity = product.Quantity - oldPurchase.Quantity + purchase.Quantity;

               
                    if (adjustedQuantity < 0)
                    {
                        ModelState.AddModelError("Quantity", $"Can't reduce this purchase — {product.Name} only has {product.Quantity} in stock and some of this purchase's stock has likely already been sold.");

                        purchase.Product = product;
                        purchase.Supplier = await _context.Suppliers.FindAsync(purchase.SupplierId);

                        ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", purchase.ProductId);
                        ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Name", purchase.SupplierId);
                        return View(purchase);
                    }

                    product.Quantity = adjustedQuantity;

                    // Update the purchase
                    _context.Update(purchase);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Purchase updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PurchaseExists(purchase.PurchaseId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", purchase.ProductId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Name", purchase.SupplierId);

            return View(purchase);
        }

        // GET: Purchases/Delete/5
        [Authorize(Roles = AppRoles.AdminOrPurchase)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchase = await _context.Purchases
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.PurchaseId == id);
            if (purchase == null)
            {
                return NotFound();
            }

            return View(purchase);
        }

        // POST: Purchases/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrPurchase)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);

            if (purchase != null)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == purchase.ProductId);

                if (product != null)
                {
                    
                    if (product.Quantity - purchase.Quantity < 0)
                    {
                        TempData["ErrorMessage"] = $"Can't delete this purchase — {product.Name} only has {product.Quantity} in stock and some of this purchase's stock has likely already been sold.";
                        return RedirectToAction(nameof(Index));
                    }

                    product.Quantity -= purchase.Quantity;
                }

                _context.Purchases.Remove(purchase);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Purchase deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PurchaseExists(int id)
        {
            return _context.Purchases.Any(e => e.PurchaseId == id);
        }
         
        private async Task<Dictionary<int, decimal>> GetLastPurchasePricesAsync()
        {
            return await _context.Purchases
                .GroupBy(p => p.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    LastPrice = g.OrderByDescending(p => p.PurchaseDate).First().PurchasePrice
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.LastPrice);
        }
    }
}
