using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Controllers
{
    [Authorize]
    public class VehicleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VehicleController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Vehicle
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Admin can see all vehicles
            if (User.IsInRole("Admin"))
            {
                var allVehicles =
                    await _context.Vehicles
                        .Include(v => v.Customer)
                            .ThenInclude(c => c.ApplicationUser)
                        .ToListAsync();

                return View(allVehicles);
            }

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            if (customer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Customer");
            }

            var vehicles =
                await _context.Vehicles
                    .Where(v =>
                        v.CustomerId == customer.Id)
                    .ToListAsync();

            return View(vehicles);
        }

        // GET: /Vehicle/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Vehicle/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Vehicle vehicle)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            if (customer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Customer");
            }

            if (!ModelState.IsValid)
                return View(vehicle);

            vehicle.CustomerId = customer.Id;

            _context.Vehicles.Add(vehicle);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Vehicle/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vehicle =
                await _context.Vehicles
                    .Include(v => v.Customer)
                        .ThenInclude(c => c.ApplicationUser)
                    .FirstOrDefaultAsync(
                        v => v.Id == id);

            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        // GET: /Vehicle/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vehicle =
                await _context.Vehicles
                    .FirstOrDefaultAsync(
                        v => v.Id == id);

            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        // POST: /Vehicle/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Vehicle vehicle)
        {
            if (id != vehicle.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(vehicle);

            var existingVehicle =
                await _context.Vehicles
                    .FirstOrDefaultAsync(
                        v => v.Id == id);

            if (existingVehicle == null)
                return NotFound();

            existingVehicle.VehicleNumber =
                vehicle.VehicleNumber;

            existingVehicle.VehicleModel =
                vehicle.VehicleModel;

            existingVehicle.VehicleType =
                vehicle.VehicleType;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Vehicle/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle =
                await _context.Vehicles
                    .Include(v => v.Customer)
                    .FirstOrDefaultAsync(
                        v => v.Id == id);

            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        // POST: /Vehicle/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var vehicle =
                await _context.Vehicles
                    .FirstOrDefaultAsync(
                        v => v.Id == id);

            if (vehicle == null)
                return NotFound();

            _context.Vehicles.Remove(vehicle);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}