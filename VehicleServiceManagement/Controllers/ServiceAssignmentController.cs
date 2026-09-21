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
                    .ThenInclude(w => w.ApplicationUser)
                .ToListAsync();

            return View(assignments);
        }

        // GET: /ServiceAssignment/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.ServiceRequests =
                await _context.ServiceRequests
                    .Where(s => s.Status != "Completed" &&
                                s.Status != "Cancelled")
                    .ToListAsync();

            ViewBag.Workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Where(w => w.IsAvailable)
                    .ToListAsync();

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

            _context.ServiceAssignments.Add(assignment);

            var serviceRequest =
                await _context.ServiceRequests
                    .FirstOrDefaultAsync(
                        s => s.Id == assignment.ServiceRequestId);

            if (serviceRequest != null)
            {
                serviceRequest.Status = "Assigned";
            }

            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(
                        w => w.Id == assignment.WorkerId);

            if (worker != null)
            {
                worker.IsAvailable = false;
            }

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
                        .ThenInclude(s => s.Vehicle)
                    .Include(a => a.Worker)
                        .ThenInclude(w => w.ApplicationUser)
                    .FirstOrDefaultAsync(
                        a => a.Id == id);

            if (assignment == null)
                return NotFound();

            return View(assignment);
        }

        // GET: /ServiceAssignment/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var assignment =
                await _context.ServiceAssignments
                    .FirstOrDefaultAsync(
                        a => a.Id == id);

            if (assignment == null)
                return NotFound();

            ViewBag.Workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Where(w => w.IsAvailable ||
                                w.Id == assignment.WorkerId)
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
            if (id != assignment.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Workers =
                    await _context.Workers
                        .Include(w => w.ApplicationUser)
                        .Where(w => w.IsAvailable ||
                                    w.Id == assignment.WorkerId)
                        .ToListAsync();

                return View(assignment);
            }

            var existingAssignment =
                await _context.ServiceAssignments
                    .FirstOrDefaultAsync(
                        a => a.Id == id);

            if (existingAssignment == null)
                return NotFound();

            existingAssignment.WorkerId =
                assignment.WorkerId;

            existingAssignment.ServiceRequestId =
                assignment.ServiceRequestId;

            existingAssignment.ServiceDate =
                assignment.ServiceDate;

            existingAssignment.ServiceTime =
                assignment.ServiceTime;

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
                        a => a.Id == id);

            if (assignment == null)
                return NotFound();

            _context.ServiceAssignments.Remove(assignment);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCreateData()
        {
            ViewBag.ServiceRequests =
                await _context.ServiceRequests
                    .Where(s => s.Status != "Completed" &&
                                s.Status != "Cancelled")
                    .ToListAsync();

            ViewBag.Workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Where(w => w.IsAvailable)
                    .ToListAsync();
        }
    }
}