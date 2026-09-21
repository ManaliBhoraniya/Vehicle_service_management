using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;

namespace VehicleServiceManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServiceManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceManagerController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ServiceManager
        public async Task<IActionResult> Index()
        {
            ViewBag.CustomerCount =
                await _context.Customers.CountAsync();

            ViewBag.VehicleCount =
                await _context.Vehicles.CountAsync();

            ViewBag.WorkerCount =
                await _context.Workers.CountAsync();

            ViewBag.ServiceRequestCount =
                await _context.ServiceRequests.CountAsync();

            ViewBag.PendingRequests =
                await _context.ServiceRequests
                    .CountAsync(s => s.Status == "Pending");

            ViewBag.AssignedRequests =
                await _context.ServiceRequests
                    .CountAsync(s => s.Status == "Assigned");

            ViewBag.CompletedRequests =
                await _context.ServiceRequests
                    .CountAsync(s => s.Status == "Completed");

            return View();
        }

        // GET: /ServiceManager/ServiceRequests
        [HttpGet]
        public async Task<IActionResult> ServiceRequests()
        {
            var requests = await _context.ServiceRequests
                .Include(s => s.Vehicle)
                    .ThenInclude(v => v.Customer)
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        // POST: /ServiceManager/UpdateRequestStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(
            int id,
            string status)
        {
            var request =
                await _context.ServiceRequests
                    .FirstOrDefaultAsync(s => s.Id == id);

            if (request == null)
                return NotFound();

            request.Status = status;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ServiceRequests));
        }

        // GET: /ServiceManager/Customers
        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            var customers = await _context.Customers
                .Include(c => c.ApplicationUser)
                .ToListAsync();

            return View(customers);
        }

        // GET: /ServiceManager/Workers
        [HttpGet]
        public async Task<IActionResult> Workers()
        {
            var workers = await _context.Workers
                .Include(w => w.ApplicationUser)
                .ToListAsync();

            return View(workers);
        }

        // GET: /ServiceManager/Vehicles
        [HttpGet]
        public async Task<IActionResult> Vehicles()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Customer)
                    .ThenInclude(c => c.ApplicationUser)
                .ToListAsync();

            return View(vehicles);
        }

        // GET: /ServiceManager/Assignments
        [HttpGet]
        public async Task<IActionResult> Assignments()
        {
            var assignments = await _context.ServiceAssignments
                .Include(a => a.ServiceRequest)
                .Include(a => a.Worker)
                    .ThenInclude(w => w.ApplicationUser)
                .ToListAsync();

            return View(assignments);
        }
    }
}