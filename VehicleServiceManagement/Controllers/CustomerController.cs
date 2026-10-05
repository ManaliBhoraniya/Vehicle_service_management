using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var customer =
                await _context.Customers
                    .Include(c => c.ApplicationUser)
                    .Include(c => c.Vehicles)
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            // Customer profile may not exist immediately
            // after registration.
            if (customer == null)
            {
                ViewBag.CustomerName =
                    user.Name ?? user.Email ?? "Customer";

                ViewBag.VehicleCount = 0;
                ViewBag.PendingCount = 0;
                ViewBag.InProgressCount = 0;
                ViewBag.CompletedCount = 0;
                ViewBag.TotalRequests = 0;

                return View(null);
            }

            // Get service request statistics
            var serviceRequests =
                await _context.ServiceRequests
                    .Include(s => s.Vehicle)
                    .Where(s =>
                        s.Vehicle != null &&
                        s.Vehicle.CustomerId == customer.Id)
                    .ToListAsync();

            ViewBag.CustomerName =
                customer.Name;

            ViewBag.VehicleCount =
                customer.Vehicles?.Count ?? 0;

            ViewBag.TotalRequests =
                serviceRequests.Count;

            ViewBag.PendingCount =
                serviceRequests.Count(
                    s => s.Status == "Pending");

            ViewBag.InProgressCount =
                serviceRequests.Count(
                    s => s.Status == "In Progress" ||
                         s.Status == "Assigned");

            ViewBag.CompletedCount =
                serviceRequests.Count(
                    s => s.Status == "Completed");

            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var customer =
                await _context.Customers
                    .Include(c => c.ApplicationUser)
                    .Include(c => c.Vehicles)
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            if (customer == null)
            {
                return RedirectToAction(
                    nameof(Create));
            }

            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var existingCustomer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            if (existingCustomer != null)
            {
                return RedirectToAction(
                    nameof(Dashboard));
            }

            // Pre-fill name from Identity account
            var customer = new Customer
            {
                Name = user.Name ?? string.Empty
            };

            return View(customer);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Customer customer)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            if (!ModelState.IsValid)
                return View(customer);

            var existingCustomer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            if (existingCustomer != null)
            {
                return RedirectToAction(
                    nameof(Dashboard));
            }

            customer.ApplicationUserId =
                user.Id;

            _context.Customers.Add(customer);

            await _context.SaveChangesAsync();

            // Keep Identity user's display name synchronized
            user.Name = customer.Name;

            await _userManager.UpdateAsync(user);

            TempData["Success"] =
                "Customer profile created successfully.";

            return RedirectToAction(
                nameof(Dashboard));
        }


        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Customer customer)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var existingCustomer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            if (existingCustomer == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(customer);

            existingCustomer.Name =
                customer.Name;

            existingCustomer.Phone =
                customer.Phone;

            existingCustomer.Address =
                customer.Address;

            // Keep Identity user's name synchronized
            user.Name =
                customer.Name;

            await _userManager.UpdateAsync(user);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Profile updated successfully.";

            return RedirectToAction(
                nameof(Dashboard));
        }
    }
}