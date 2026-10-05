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
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            if (User.IsInRole("Admin") ||
                User.IsInRole("Worker"))
            {
                var allRequests =
                    await _context.ServiceRequests
                        .Include(s => s.Vehicle)
                            .ThenInclude(v => v!.Customer)
                                .ThenInclude(c => c!.ApplicationUser)
                        .OrderByDescending(
                            s => s.RequestDate)
                        .ToListAsync();

                return View(allRequests);
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

            var requests =
                await _context.ServiceRequests
                    .Include(s => s.Vehicle)
                    .Where(s =>
                        s.Vehicle != null &&
                        s.Vehicle.CustomerId == customer.Id)
                    .OrderByDescending(
                        s => s.RequestDate)
                    .ToListAsync();

            return View(requests);
        }

        // GET: /ServiceRequest/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            // Customer profile must exist first
            if (customer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Customer");
            }

            // Load only the logged-in customer's vehicles
            ViewBag.Vehicles =
                await _context.Vehicles
                    .Where(v =>
                        v.CustomerId == customer.Id)
                    .OrderBy(v => v.VehicleNumber)
                    .ToListAsync();

            return View();
        }


        // POST: /ServiceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ServiceRequest serviceRequest)
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
            {
                return RedirectToAction(
                    "Create",
                    "Customer");
            }

            var vehicle =
                await _context.Vehicles
                    .FirstOrDefaultAsync(v =>
                        v.Id == serviceRequest.VehicleId &&
                        v.CustomerId == customer.Id);

            if (vehicle == null)
            {
                ModelState.AddModelError(
                    "VehicleId",
                    "Please select a valid vehicle.");
            }

            if (!ModelState.IsValid)
            {
                // Reload vehicles because the form
                // is being displayed again.

                ViewBag.Vehicles =
                    await _context.Vehicles
                        .Where(v =>
                            v.CustomerId == customer.Id)
                        .OrderBy(v => v.VehicleNumber)
                        .ToListAsync();

                return View(serviceRequest);
            }


            serviceRequest.CustomerId =
                customer.Id;

            serviceRequest.RequestDate =
                DateTime.Now;

            serviceRequest.Status =
                "Pending";

            _context.ServiceRequests.Add(
                serviceRequest);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index));
        }


        // GET: /ServiceRequest/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            var request =
                await _context.ServiceRequests
                    .Include(s => s.Customer)
                        .ThenInclude(c => c!.ApplicationUser)
                    .Include(s => s.Vehicle)
                        .ThenInclude(v => v!.Customer)
                            .ThenInclude(c => c!.ApplicationUser)
                    .FirstOrDefaultAsync(
                        s => s.Id == id);

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
            var request =
                await _context.ServiceRequests
                    .FirstOrDefaultAsync(
                        s => s.Id == id);

            if (request == null)
                return NotFound();

            request.Status =
                status;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index));
        }


        // POST: /ServiceRequest/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            int id)
        {
            var request =
                await _context.ServiceRequests
                    .Include(s => s.Vehicle)
                    .FirstOrDefaultAsync(
                        s => s.Id == id);

            if (request == null)
                return NotFound();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            if (User.IsInRole("Admin") ||
                User.IsInRole("Worker"))
            {
                request.Status =
                    "Cancelled";

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(Index));
            }


            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            // Customer can cancel only their own request
            if (customer == null ||
                request.Vehicle == null ||
                request.Vehicle.CustomerId != customer.Id)
            {
                return Forbid();
            }

            request.Status =
                "Cancelled";

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index));
        }


        // POST: /ServiceRequest/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.ServiceRequests
                .Include(s => s.ServiceAssignments)
                .FirstOrDefaultAsync(s => s.ServiceRequestId == id);

            if (request == null)
            {
                TempData["Error"] = "Service request not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (request.ServiceAssignments != null && request.ServiceAssignments.Any())
                {
                    _context.ServiceAssignments.RemoveRange(request.ServiceAssignments);
                }

                _context.ServiceRequests.Remove(request);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Service request deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unable to delete request: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}