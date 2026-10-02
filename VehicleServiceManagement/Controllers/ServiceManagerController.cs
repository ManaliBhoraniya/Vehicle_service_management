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
        // HELPER: Map Service Type to Matching Specialities
        // =====================================================
        public static List<string> GetMatchingSpecialities(string? serviceType)
        {
            if (string.IsNullOrWhiteSpace(serviceType))
            {
                return new List<string>();
            }

            var st = serviceType.Trim();
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { st };

            // Brake / Break
            if (st.Contains("brake", StringComparison.OrdinalIgnoreCase) ||
                st.Contains("break", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("Brake Repair");
                list.Add("Brake Problem");
                list.Add("Break Repair");
                list.Add("Break Problem");
            }
            // Engine
            else if (st.Contains("engine", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("Engine Repair");
                list.Add("Engine Problem");
            }
            // AC
            else if (st.Contains("ac", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("AC Repair");
                list.Add("AC Problem");
            }
            // Electrical / Battery
            else if (st.Contains("electric", StringComparison.OrdinalIgnoreCase) ||
                     st.Contains("battery", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("Electrical Repair");
                list.Add("Electrical Problem");
                list.Add("Battery Problem");
            }
            // Tyre / Tire / Wheel
            else if (st.Contains("tyre", StringComparison.OrdinalIgnoreCase) ||
                     st.Contains("tire", StringComparison.OrdinalIgnoreCase) ||
                     st.Contains("wheel", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("Tire & Wheel Service");
                list.Add("Tyre Problem");
                list.Add("Tire Problem");
            }
            // Transmission
            else if (st.Contains("transmission", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("Transmission Repair");
                list.Add("Transmission Problem");
            }
            // Oil Change
            else if (st.Contains("oil", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("Oil Change");
            }
            // General Service
            else if (st.Contains("general", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("General Service");
            }

            return list.ToList();
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
                    .Include(s => s.Customer)
                        .ThenInclude(c => c!.ApplicationUser)
                    .Include(s => s.Vehicle)
                        .ThenInclude(v => v!.Customer)
                            .ThenInclude(c => c!.ApplicationUser)
                    .Include(s => s.ServiceAssignments)
                        .ThenInclude(sa => sa.Worker)
                            .ThenInclude(w => w!.ApplicationUser)
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
        // DELETE SERVICE REQUEST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.ServiceRequests
                .Include(s => s.ServiceAssignments)
                .FirstOrDefaultAsync(s => s.ServiceRequestId == id);

            if (request == null)
            {
                TempData["Error"] = "Service request not found.";
                return RedirectToAction(nameof(ServiceRequests));
            }

            try
            {
                // Free up any worker who was assigned/in progress with this request
                if (request.ServiceAssignments != null && request.ServiceAssignments.Any())
                {
                    var assignedWorkerIds = request.ServiceAssignments
                        .Where(a => a.Status == "Accepted" || a.Status == "In Progress")
                        .Select(a => a.WorkerId)
                        .Distinct()
                        .ToList();

                    foreach (var workerId in assignedWorkerIds)
                    {
                        var worker = await _context.Workers.FindAsync(workerId);
                        if (worker != null)
                        {
                            worker.IsAvailable = true;
                        }
                    }

                    _context.ServiceAssignments.RemoveRange(request.ServiceAssignments);
                }

                _context.ServiceRequests.Remove(request);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Service Request #{id} has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unable to delete service request: {ex.Message}";
            }

            return RedirectToAction(nameof(ServiceRequests));
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
                    .Include(s => s.Customer)
                        .ThenInclude(c => c!.ApplicationUser)
                    .Include(s => s.Vehicle)
                        .ThenInclude(v => v!.Customer)
                            .ThenInclude(c => c!.ApplicationUser)
                    .Include(s => s.ServiceAssignments)
                        .ThenInclude(sa => sa.Worker)
                            .ThenInclude(w => w!.ApplicationUser)
                    .FirstOrDefaultAsync(
                        s => s.ServiceRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            // Find worker IDs who rejected this request previously
            var rejectedWorkerIds = request.ServiceAssignments
                .Where(a => a.Status == "Rejected")
                .Select(a => a.WorkerId)
                .Distinct()
                .ToList();

            ViewBag.RejectedWorkerIds = rejectedWorkerIds;

            // -------------------------------------------------
            // Find workers whose speciality matches
            // the requested service type.
            // -------------------------------------------------

            var matchingSpecialities = GetMatchingSpecialities(request.ServiceType);

            var allMatchingWorkers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Include(w => w.Specialities)
                    .Where(w =>
                        w.Status == "Accepted" &&
                        w.IsAvailable &&
                        w.Specialities.Any(
                            s => matchingSpecialities.Contains(s.Speciality)))
                    .OrderBy(w =>
                        w.ApplicationUser!.Name)
                    .ToListAsync();

            // If there are other matching workers available, exclude the rejected worker(s)
            var eligibleWorkers = allMatchingWorkers
                .Where(w => !rejectedWorkerIds.Contains(w.WorkerId))
                .ToList();

            ViewBag.Workers = eligibleWorkers.Any() ? eligibleWorkers : allMatchingWorkers;

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

            var matchingSpecialities = GetMatchingSpecialities(request.ServiceType);

            var worker =
                await _context.Workers
                    .Include(w => w.Specialities)
                    .FirstOrDefaultAsync(
                        w =>
                            w.WorkerId == workerId &&
                            w.Status == "Accepted" &&
                            w.IsAvailable &&
                            w.Specialities.Any(
                                s => matchingSpecialities.Contains(s.Speciality)));

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
                             a.Status == "Accepted" ||
                             a.Status == "In Progress"));

            if (existingAssignment)
            {
                TempData["Error"] =
                    "This service request already has an active worker assignment.";

                return RedirectToAction(
                    nameof(ServiceRequests));
            }

            // -------------------------------------------------
            // Create assignment (Status: Pending)
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

            // Update service request status to Assigned
            request.Status = "Assigned";

            // Note: Per user requirement, worker remains available until they accept the request.
            // worker.IsAvailable is NOT set to false here.

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Task assigned successfully. The worker has received the work request and must accept or reject it.";

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
                        .ThenInclude(w => w!.ApplicationUser)
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

            var matchingSpecialities = GetMatchingSpecialities(serviceType);

            var workers =
                await _context.Workers
                    .Include(w => w.ApplicationUser)
                    .Include(w => w.Specialities)
                    .Where(w =>
                        w.Status == "Accepted" &&
                        w.IsAvailable &&
                        w.WorkerId != rejectedWorkerId &&
                        w.Specialities.Any(
                            s => matchingSpecialities.Contains(s.Speciality)))
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

            var matchingSpecialities = GetMatchingSpecialities(request.ServiceType);

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
                                s => matchingSpecialities.Contains(s.Speciality)));

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

            // Update request status to Assigned
            request.Status = "Assigned";

            // Worker remains available until they accept
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Task has been reassigned. Waiting for the worker to accept or reject the task.";

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
                            c!.ApplicationUser)
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
                        .ThenInclude(s => s!.Vehicle)
                    .Include(a => a.Worker)
                        .ThenInclude(w =>
                            w!.ApplicationUser)
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