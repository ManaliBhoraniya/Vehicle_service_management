using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServiceAssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceAssignmentController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ServiceAssignment
        public async Task<IActionResult> Index()
        {
            var assignments = await _context.ServiceAssignments
                .Include(a => a.ServiceRequest)
                .Include(a => a.Worker)
                    .ThenInclude(w => w!.ApplicationUser)
                .ToListAsync();

            return View(assignments);
        }

        // GET: /ServiceAssignment/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCreateData();

            return View();
        }

        // POST: /ServiceAssignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ServiceAssignment assignment)
        {
            if (!ModelState.IsValid)
            {
                await LoadCreateData();

                return View(assignment);
            }

            // Check service request
            var serviceRequest =
                await _context.ServiceRequests
                    .FirstOrDefaultAsync(
                        s => s.ServiceRequestId ==
                             assignment.ServiceRequestId);

            if (serviceRequest == null)
            {
                ModelState.AddModelError(
                    "ServiceRequestId",
                    "Selected service request was not found.");

                await LoadCreateData();

                return View(assignment);
            }

            // Check worker
            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(
                        w => w.WorkerId ==
                             assignment.WorkerId);

            if (worker == null)
            {
                ModelState.AddModelError(
                    "WorkerId",
                    "Selected worker was not found.");

                await LoadCreateData();

                return View(assignment);
            }

            // Worker must be available
            if (!worker.IsAvailable)
            {
                ModelState.AddModelError(
                    "WorkerId",
                    "Selected worker is not available.");

                await LoadCreateData();

                return View(assignment);
            }

            // Set initial values
            assignment.Status = "Pending";
            assignment.AssignedDate = DateTime.Now;

            _context.ServiceAssignments.Add(assignment);

            // Update service request
            serviceRequest.Status = "Assigned";

            // Worker becomes unavailable
            worker.IsAvailable = false;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /ServiceAssignment/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var assignment =
                await _context.ServiceAssignments
                    .Include(a => a.ServiceRequest)
                    .Include(a => a.Worker)
                        .ThenInclude(w => w!.ApplicationUser)
                    .FirstOrDefaultAsync(
                        a => a.ServiceAssignmentId == id);

            if (assignment == null)
            {
                return NotFound();
            }

            return View(assignment);
        }

        // GET: /ServiceAssignment/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var assignment =
                await _context.ServiceAssignments
                    .FirstOrDefaultAsync(
                        a => a.ServiceAssignmentId == id);

            if (assignment == null)
            {
                return NotFound();
            }

            ViewBag.Workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Where(w =>
                        w.IsAvailable ||
                        w.WorkerId == assignment.WorkerId)
                    .ToListAsync();

            ViewBag.ServiceRequests =
                await _context.ServiceRequests
                    .Where(s =>
                        s.Status != "Completed" &&
                        s.Status != "Cancelled" ||
                        s.ServiceRequestId ==
                        assignment.ServiceRequestId)
                    .ToListAsync();

            return View(assignment);
        }

        // POST: /ServiceAssignment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ServiceAssignment assignment)
        {
            if (id != assignment.ServiceAssignmentId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await LoadEditData(assignment);

                return View(assignment);
            }

            var existingAssignment =
                await _context.ServiceAssignments
                    .FirstOrDefaultAsync(
                        a => a.ServiceAssignmentId == id);

            if (existingAssignment == null)
            {
                return NotFound();
            }

            // Check worker
            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(
                        w => w.WorkerId ==
                             assignment.WorkerId);

            if (worker == null)
            {
                ModelState.AddModelError(
                    "WorkerId",
                    "Selected worker was not found.");

                await LoadEditData(assignment);

                return View(assignment);
            }

            // Check service request
            var serviceRequest =
                await _context.ServiceRequests
                    .FirstOrDefaultAsync(
                        s => s.ServiceRequestId ==
                             assignment.ServiceRequestId);

            if (serviceRequest == null)
            {
                ModelState.AddModelError(
                    "ServiceRequestId",
                    "Selected service request was not found.");

                await LoadEditData(assignment);

                return View(assignment);
            }

            existingAssignment.WorkerId =
                assignment.WorkerId;

            existingAssignment.ServiceRequestId =
                assignment.ServiceRequestId;

            existingAssignment.Status =
                assignment.Status;

            existingAssignment.Notes =
                assignment.Notes;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /ServiceAssignment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var assignment =
                await _context.ServiceAssignments
                    .FirstOrDefaultAsync(
                        a => a.ServiceAssignmentId == id);

            if (assignment == null)
            {
                return NotFound();
            }

            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(
                        w => w.WorkerId ==
                             assignment.WorkerId);

            if (worker != null)
            {
                worker.IsAvailable = true;
            }

            _context.ServiceAssignments.Remove(assignment);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCreateData()
        {
            ViewBag.ServiceRequests =
                await _context.ServiceRequests
                    .Where(s =>
                        s.Status != "Completed" &&
                        s.Status != "Cancelled")
                    .ToListAsync();

            ViewBag.Workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Where(w => w.IsAvailable)
                    .ToListAsync();
        }

        private async Task LoadEditData(
            ServiceAssignment assignment)
        {
            ViewBag.Workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Where(w =>
                        w.IsAvailable ||
                        w.WorkerId == assignment.WorkerId)
                    .ToListAsync();

            ViewBag.ServiceRequests =
                await _context.ServiceRequests
                    .Where(s =>
                        (s.Status != "Completed" &&
                         s.Status != "Cancelled") ||
                        s.ServiceRequestId ==
                        assignment.ServiceRequestId)
                    .ToListAsync();
        }
    }
}