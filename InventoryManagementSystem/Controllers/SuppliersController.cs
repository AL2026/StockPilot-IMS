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
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Suppliers.ToListAsync());
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.SupplierId == id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: Suppliers/Create
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Suppliers/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> Create([Bind("SupplierId,Name,Phone,Email,Address")] Supplier supplier)
        {
            if (!string.IsNullOrWhiteSpace(supplier.Name) &&
                await _context.Suppliers.AnyAsync(s => s.Name.ToLower() == supplier.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "A supplier with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Supplier \"{supplier.Name}\" created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Suppliers/Edit/5
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: Suppliers/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> Edit(int id, [Bind("SupplierId,Name,Phone,Email,Address")] Supplier supplier)
        {
            if (id != supplier.SupplierId)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(supplier.Name) &&
                await _context.Suppliers.AnyAsync(s => s.Name.ToLower() == supplier.Name.ToLower() && s.SupplierId != supplier.SupplierId))
            {
                ModelState.AddModelError("Name", "A supplier with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Supplier \"{supplier.Name}\" updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.SupplierId))
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
            return View(supplier);
        }

        // GET: Suppliers/Delete/5
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.SupplierId == id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrInventory)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Supplier deleted successfully.";
                }
                catch (DbUpdateException)
                {

                    TempData["ErrorMessage"] = $"Can't delete \"{supplier.Name}\" — it still has products or purchase history linked to it.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.SupplierId == id);
        }
    }
}
