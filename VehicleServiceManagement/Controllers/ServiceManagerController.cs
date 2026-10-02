using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

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

        [HttpGet]
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

            ViewBag.PendingJobRequests =
                await _context.Workers
                    .CountAsync(w => w.Status == "Pending");

            ViewBag.RejectedAssignments =
                await _context.ServiceAssignments
                    .CountAsync(a => a.Status == "Rejected");

            return View();
        }


        // =====================================================
        // SERVICE REQUESTS
        // GET: /ServiceManager/ServiceRequests
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ServiceRequests()
        {
            var requests =
                await _context.ServiceRequests
                    .Include(s => s.Vehicle)
                        .ThenInclude(v => v.Customer)
                    .OrderByDescending(
                        s => s.RequestDate)
                    .ToListAsync();

            return View(requests);
        }


        // =====================================================
        // UPDATE SERVICE REQUEST STATUS
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(
            int id,
            string status)
        {
            var request =
                await _context.ServiceRequests
                    .FirstOrDefaultAsync(
                        s => s.ServiceRequestId == id);

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
        // ASSIGN WORKER - GET
        // Shows only accepted + available workers
        // whose speciality matches the service request
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AssignWorker(
            int id)
        {
            var request =
                await _context.ServiceRequests
                    .Include(s => s.Vehicle)
                        .ThenInclude(v => v.Customer)
                    .FirstOrDefaultAsync(
                        s => s.ServiceRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            // -------------------------------------------------
            // Find workers whose speciality matches
            // the requested service type.
            // -------------------------------------------------

            var workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Include(w => w.Specialities)
                    .Where(w =>
                        w.Status == "Accepted" &&
                        w.IsAvailable &&
                        w.Specialities.Any(
                            s => s.Speciality ==
                                 request.ServiceType))
                    .OrderBy(w =>
                        w.ApplicationUser!.Name)
                    .ToListAsync();

            ViewBag.Workers = workers;

            return View(request);
        }


        // =====================================================
        // ASSIGN WORKER - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignWorker(
            int serviceRequestId,
            int workerId,
            DateTime serviceDate,
            TimeSpan serviceTime)
        {
            var request =
                await _context.ServiceRequests
                    .FirstOrDefaultAsync(
                        s =>
                            s.ServiceRequestId ==
                            serviceRequestId);

            if (request == null)
            {
                return NotFound();
            }

            // -------------------------------------------------
            // Make sure the selected worker:
            // 1. Exists
            // 2. Is accepted
            // 3. Is available
            // 4. Has matching speciality
            // -------------------------------------------------

            var worker =
                await _context.Workers
                    .Include(w => w.Specialities)
                    .FirstOrDefaultAsync(
                        w =>
                            w.WorkerId == workerId &&
                            w.Status == "Accepted" &&
                            w.IsAvailable &&
                            w.Specialities.Any(
                                s => s.Speciality ==
                                     request.ServiceType));

            if (worker == null)
            {
                TempData["Error"] =
                    "The selected worker is not available or does not have the required speciality.";

                return RedirectToAction(
                    nameof(ServiceRequests));
            }

            // -------------------------------------------------
            // Prevent duplicate active assignment
            // -------------------------------------------------

            var existingAssignment =
                await _context.ServiceAssignments
                    .AnyAsync(
                        a =>
                            a.ServiceRequestId ==
                                serviceRequestId &&
                            (a.Status == "Pending" ||
                             a.Status == "Accepted"));

            if (existingAssignment)
            {
                TempData["Error"] =
                    "This service request already has an active worker assignment.";

                return RedirectToAction(
                    nameof(ServiceRequests));
            }

            // -------------------------------------------------
            // Create assignment
            // -------------------------------------------------

            var assignment =
                new ServiceAssignment
                {
                    ServiceRequestId =
                        serviceRequestId,

                    WorkerId =
                        workerId,

                    Status = "Pending",

                    AssignedDate =
                        serviceDate.Date.Add(serviceTime),

                    Notes =
                        "Task assigned by Service Manager."
                };

            _context.ServiceAssignments.Add(
                assignment);

            // IMPORTANT:
            // Request remains Pending until worker accepts.
            request.Status = "Pending";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Task assigned successfully. The worker must accept or reject the task.";

            return RedirectToAction(
                nameof(ServiceRequests));
        }


        // =====================================================
        // REASSIGN REJECTED TASK - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ReassignWorker(
            int id)
        {
            var assignment =
                await _context.ServiceAssignments
                    .Include(a => a.ServiceRequest)
                    .Include(a => a.Worker)
                        .ThenInclude(w => w.ApplicationUser)
                    .FirstOrDefaultAsync(
                        a =>
                            a.ServiceAssignmentId == id &&
                            a.Status == "Rejected");

            if (assignment == null)
            {
                return NotFound();
            }

            var serviceType =
                assignment.ServiceRequest?.ServiceType;

            if (string.IsNullOrWhiteSpace(serviceType))
            {
                TempData["Error"] =
                    "The service type could not be found.";

                return RedirectToAction(
                    nameof(ServiceRequests));
            }

            var rejectedWorkerId =
                assignment.WorkerId;

            // -------------------------------------------------
            // Find another accepted and available worker
            // with the required speciality.
            // -------------------------------------------------

            var workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Include(w => w.Specialities)
                    .Where(w =>
                        w.Status == "Accepted" &&
                        w.IsAvailable &&
                        w.WorkerId != rejectedWorkerId &&
                        w.Specialities.Any(
                            s => s.Speciality == serviceType))
                    .OrderBy(w =>
                        w.ApplicationUser!.Name)
                    .ToListAsync();

            ViewBag.Workers = workers;

            return View(assignment);
        }


        // =====================================================
        // REASSIGN REJECTED TASK - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignWorker(
            int assignmentId,
            int workerId,
            DateTime serviceDate,
            TimeSpan serviceTime)
        {
            var oldAssignment =
                await _context.ServiceAssignments
                    .Include(a => a.ServiceRequest)
                    .FirstOrDefaultAsync(
                        a =>
                            a.ServiceAssignmentId ==
                                assignmentId &&
                            a.Status == "Rejected");

            if (oldAssignment == null)
            {
                return NotFound();
            }

            var request =
                oldAssignment.ServiceRequest;

            if (request == null)
            {
                return NotFound();
            }

            // -------------------------------------------------
            // Find another matching worker
            // -------------------------------------------------

            var worker =
                await _context.Workers
                    .Include(w => w.Specialities)
                    .FirstOrDefaultAsync(
                        w =>
                            w.WorkerId == workerId &&
                            w.Status == "Accepted" &&
                            w.IsAvailable &&
                            w.WorkerId !=
                                oldAssignment.WorkerId &&
                            w.Specialities.Any(
                                s => s.Speciality ==
                                     request.ServiceType));

            if (worker == null)
            {
                TempData["Error"] =
                    "The selected worker is not available or does not have the required speciality.";

                return RedirectToAction(
                    nameof(ServiceRequests));
            }

            // -------------------------------------------------
            // Create new assignment
            // -------------------------------------------------

            var newAssignment =
                new ServiceAssignment
                {
                    ServiceRequestId =
                        request.ServiceRequestId,

                    WorkerId =
                        workerId,

                    Status = "Pending",

                    AssignedDate =
                        serviceDate.Date.Add(serviceTime),

                    Notes =
                        "Task reassigned by Service Manager."
                };

            _context.ServiceAssignments.Add(
                newAssignment);

            // Service request is again waiting for
            // the new worker's response.
            request.Status = "Pending";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Task has been reassigned to another suitable worker.";

            return RedirectToAction(
                nameof(ServiceRequests));
        }


        // =====================================================
        // CUSTOMERS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            var customers =
                await _context.Customers
                    .Include(c => c.ApplicationUser)
                    .Include(c => c.Vehicles)
                    .ToListAsync();

            return View(customers);
        }


        // =====================================================
        // WORKERS
        // Only accepted workers
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Workers()
        {
            var workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Include(w => w.Specialities)
                    .Where(w => w.Status == "Accepted")
                    .ToListAsync();

            return View(workers);
        }


        // =====================================================
        // VEHICLES
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Vehicles()
        {
            var vehicles =
                await _context.Vehicles
                    .Include(v => v.Customer)
                        .ThenInclude(c =>
                            c.ApplicationUser)
                    .ToListAsync();

            return View(vehicles);
        }


        // =====================================================
        // ASSIGNMENTS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Assignments()
        {
            var assignments =
                await _context.ServiceAssignments
                    .Include(a => a.ServiceRequest)
                        .ThenInclude(s => s.Vehicle)
                    .Include(a => a.Worker)
                        .ThenInclude(w =>
                            w.ApplicationUser)
                    .OrderByDescending(
                        a => a.AssignedDate)
                    .ToListAsync();

            return View(assignments);
        }


        // =====================================================
        // JOB REQUESTS
        // Worker registration requests
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> JobRequests()
        {
            var requests =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Include(w => w.Specialities)
                    .Where(w => w.Status == "Pending")
                    .OrderBy(w => w.WorkerId)
                    .ToListAsync();

            return View(requests);
        }


        // =====================================================
        // ACCEPT WORKER JOB REQUEST
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

            if (worker.Status != "Pending")
            {
                TempData["Error"] =
                    "This job request has already been processed.";

                return RedirectToAction(
                    nameof(JobRequests));
            }

            worker.Status = "Accepted";

            worker.IsAvailable = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Worker job request accepted successfully.";

            return RedirectToAction(
                nameof(JobRequests));
        }


        // =====================================================
        // REJECT WORKER JOB REQUEST
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

            if (worker.Status != "Pending")
            {
                TempData["Error"] =
                    "This job request has already been processed.";

                return RedirectToAction(
                    nameof(JobRequests));
            }

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