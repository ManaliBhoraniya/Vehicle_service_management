using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Controllers
{
    [Authorize]
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceRequestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /ServiceRequest
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            if (User.IsInRole("Admin") || User.IsInRole("Worker"))
            {
                var allRequests = await _context.ServiceRequests
                    .Include(s => s.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .ToListAsync();

                return View(allRequests);
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null)
                return RedirectToAction("Create", "Customer");

            var requests = await _context.ServiceRequests
                .Include(s => s.Vehicle)
                .Where(s => s.Vehicle.CustomerId == customer.Id)
                .ToListAsync();

            return View(requests);
        }

        // GET: /ServiceRequest/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null)
                return RedirectToAction("Create", "Customer");

            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.CustomerId == customer.Id)
                .ToListAsync();

            return View();
        }

        // POST: /ServiceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest serviceRequest)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null)
                return RedirectToAction("Create", "Customer");

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v =>
                    v.Id == serviceRequest.VehicleId &&
                    v.CustomerId == customer.Id);

            if (vehicle == null)
            {
                ModelState.AddModelError(
                    "VehicleId",
                    "Please select a valid vehicle.");

                ViewBag.Vehicles = await _context.Vehicles
                    .Where(v => v.CustomerId == customer.Id)
                    .ToListAsync();

                return View(serviceRequest);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Vehicles = await _context.Vehicles
                    .Where(v => v.CustomerId == customer.Id)
                    .ToListAsync();

                return View(serviceRequest);
            }

            serviceRequest.RequestDate = DateTime.Now;
            serviceRequest.Status = "Pending";

            _context.ServiceRequests.Add(serviceRequest);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /ServiceRequest/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.ServiceRequests
                .Include(s => s.Vehicle)
                .ThenInclude(v => v.Customer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (request == null)
                return NotFound();

            return View(request);
        }

        // POST: /ServiceRequest/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Worker")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status)
        {
            var request = await _context.ServiceRequests
                .FirstOrDefaultAsync(s => s.Id == id);

            if (request == null)
                return NotFound();

            request.Status = status;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /ServiceRequest/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var request = await _context.ServiceRequests
                .FirstOrDefaultAsync(s => s.Id == id);

            if (request == null)
                return NotFound();

            request.Status = "Cancelled";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}