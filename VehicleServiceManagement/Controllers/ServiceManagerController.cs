using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;

namespace VehicleServiceManagement.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ServiceManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceManagerController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // MANAGER DASHBOARD
        // GET: /ServiceManager
        // =====================================================

        public async Task<IActionResult> Index()
        {
            ViewBag.CustomerCount =
                await _context.Customers.CountAsync();

            ViewBag.VehicleCount =
                await _context.Vehicles.CountAsync();

            ViewBag.WorkerCount =
                await _context.Workers
                    .CountAsync(w => w.Status == "Accepted");

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

            // =================================================
            // PENDING WORKER JOB REQUESTS
            // =================================================

            ViewBag.PendingJobRequests =
                await _context.Workers
                    .CountAsync(w => w.Status == "Pending");

            return View();
        }


        // =====================================================
        // SERVICE REQUESTS
        // GET: /ServiceManager/ServiceRequests
        // =====================================================

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


        // =====================================================
        // UPDATE SERVICE REQUEST STATUS
        // POST: /ServiceManager/UpdateRequestStatus
        // =====================================================

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
            {
                return NotFound();
            }

            request.Status = status;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(ServiceRequests));
        }


        // =====================================================
        // CUSTOMERS
        // GET: /ServiceManager/Customers
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            var customers = await _context.Customers
                .Include(c => c.ApplicationUser)
                .Include(c => c.Vehicles)
                .ToListAsync();

            return View(customers);
        }


        // =====================================================
        // WORKERS
        // GET: /ServiceManager/Workers
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Workers()
        {
            // Only accepted workers appear here.
            // Pending workers appear under Job Requests.

            var workers = await _context.Workers
                .Include(w => w.ApplicationUser)
                .Where(w => w.Status == "Accepted")
                .ToListAsync();

            return View(workers);
        }


        // =====================================================
        // VEHICLES
        // GET: /ServiceManager/Vehicles
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Vehicles()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Customer)
                    .ThenInclude(c => c.ApplicationUser)
                .ToListAsync();

            return View(vehicles);
        }


        // =====================================================
        // ASSIGNMENTS
        // GET: /ServiceManager/Assignments
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Assignments()
        {
            var assignments = await _context.ServiceAssignments
                .Include(a => a.ServiceRequest)
                    .ThenInclude(s => s.Vehicle)
                .Include(a => a.Worker)
                    .ThenInclude(w => w.ApplicationUser)
                .ToListAsync();

            return View(assignments);
        }


        // =====================================================
        // JOB REQUESTS
        // GET: /ServiceManager/JobRequests
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> JobRequests()
        {
            var requests = await _context.Workers
                .Include(w => w.ApplicationUser)
                .Where(w => w.Status == "Pending")
                .OrderBy(w => w.WorkerId)
                .ToListAsync();

            return View(requests);
        }


        // =====================================================
        // ACCEPT JOB REQUEST
        // POST: /ServiceManager/AcceptJobRequest
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptJobRequest(
            int id)
        {
            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(
                        w => w.WorkerId == id);

            if (worker == null)
            {
                return NotFound();
            }

            // Only pending requests can be accepted.

            if (worker.Status != "Pending")
            {
                TempData["Error"] =
                    "This job request has already been processed.";

                return RedirectToAction(
                    nameof(JobRequests));
            }

            // =================================================
            // ACCEPT WORKER
            // =================================================

            worker.Status = "Accepted";

            worker.IsAvailable = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Worker job request accepted successfully.";

            return RedirectToAction(
                nameof(JobRequests));
        }


        // =====================================================
        // REJECT JOB REQUEST
        // POST: /ServiceManager/RejectJobRequest
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectJobRequest(
            int id)
        {
            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(
                        w => w.WorkerId == id);

            if (worker == null)
            {
                return NotFound();
            }

            // Only pending requests can be rejected.

            if (worker.Status != "Pending")
            {
                TempData["Error"] =
                    "This job request has already been processed.";

                return RedirectToAction(
                    nameof(JobRequests));
            }

            // =================================================
            // REJECT WORKER
            // =================================================

            worker.Status = "Rejected";

            worker.IsAvailable = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Worker job request rejected.";

            return RedirectToAction(
                nameof(JobRequests));
        }
    }
}