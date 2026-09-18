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
using InventoryManagementSystem.Models.ViewModels;

namespace InventoryManagementSystem.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string searchString, string sortOrder, int page = 1)
        {
            int pageSize = 10;

            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
            ViewData["QuantitySortParm"] = sortOrder == "Quantity" ? "quantity_desc" : "Quantity";
            ViewData["CurrentFilter"] = searchString;

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();


            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString) ||
                    (p.Description != null && p.Description.Contains(searchString)));
            }


            products = sortOrder switch
            {
                "name_desc" => products.OrderByDescending(p => p.Name),
                "Price" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "Quantity" => products.OrderBy(p => p.Quantity),
                "quantity_desc" => products.OrderByDescending(p => p.Quantity),
                _ => products.OrderBy(p => p.Name)
            };

            var paginatedProducts = await PaginatedList<Product>.CreateAsync(
                 products.AsNoTracking(),
                 page,
                 pageSize);

            return View(paginatedProducts);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");

            ViewData["SupplierId"] = new SelectList(_context.Suppliers,
                "SupplierId",
                "Name");
            return View();
        }

        // POST: Products/Create
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> Create([Bind("ProductId,Name,Description,Price,CategoryId,SupplierId")] Product product)
        {
            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                product.Quantity = 0;

                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Product \"{product.Name}\" created with 0 stock. Record a Purchase to add inventory.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Name", product.SupplierId);

            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);


            ViewData["SupplierId"] = new SelectList(_context.Suppliers,
                "SupplierId",
                "Name",
                product.SupplierId);


            return View(product);
        }

        // POST: Products/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,CategoryId,SupplierId")] Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.SupplierId = product.SupplierId;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Product \"{existingProduct.Name}\" updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Name", product.SupplierId);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Product deleted successfully.";
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = $"Can't delete \"{product.Name}\" — it has purchase or sale history linked to it.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
