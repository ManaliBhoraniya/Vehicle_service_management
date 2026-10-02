using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Controllers
{
    [Authorize(Roles = "Worker")]
    public class WorkerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public WorkerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // ====================================================
        // DASHBOARD
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .Include(w => w.ApplicationUser)
                .Include(w => w.Specialities)
                .Include(w => w.Availabilities)
                .Include(w => w.ServiceAssignments)
                    .ThenInclude(sa => sa.ServiceRequest)
                .FirstOrDefaultAsync(w =>
                    w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound("Worker profile was not found.");
            }

            return View(worker);
        }

        // ====================================================
        // MY PROFILE
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .Include(w => w.ApplicationUser)
                .Include(w => w.Specialities)
                .Include(w => w.Availabilities)
                .Include(w => w.ServiceAssignments)
                    .ThenInclude(sa => sa.ServiceRequest)
                .FirstOrDefaultAsync(w =>
                    w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound("Worker profile was not found.");
            }

            return View(worker);
        }

        // ====================================================
        // EDIT MY PROFILE - GET
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .Include(w => w.ApplicationUser)
                .Include(w => w.Specialities)
                .FirstOrDefaultAsync(w =>
                    w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound("Worker profile was not found.");
            }

            return View(worker);
        }

        // ====================================================
        // EDIT MY PROFILE - POST
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int workerId,
            string name,
            string? phone,
            List<string>? specialities)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .Include(w => w.ApplicationUser)
                .Include(w => w.Specialities)
                .FirstOrDefaultAsync(w =>
                    w.WorkerId == workerId &&
                    w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound();
            }

            // -----------------------------------------------
            // VALIDATE NAME
            // -----------------------------------------------

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Name is required.");
            }

            // -----------------------------------------------
            // VALIDATE SPECIALITIES
            // -----------------------------------------------

            if (specialities == null ||
                specialities.Count == 0)
            {
                ModelState.AddModelError(
                    "Specialities",
                    "Please select at least one speciality.");
            }

            if (!ModelState.IsValid)
            {
                return View(worker);
            }

            // -----------------------------------------------
            // UPDATE USER PROFILE
            // -----------------------------------------------

            worker.ApplicationUser!.Name =
                name.Trim();

            worker.ApplicationUser.PhoneNumber =
                phone?.Trim();

            // -----------------------------------------------
            // REMOVE OLD SPECIALITIES
            // -----------------------------------------------

            _context.WorkerSpecialities.RemoveRange(
                worker.Specialities);

            // -----------------------------------------------
            // ADD NEW SPECIALITIES
            // -----------------------------------------------

            foreach (var speciality in specialities!)
            {
                if (!string.IsNullOrWhiteSpace(speciality))
                {
                    _context.WorkerSpecialities.Add(
                        new WorkerSpeciality
                        {
                            WorkerId =
                                worker.WorkerId,

                            Speciality =
                                speciality.Trim()
                        });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Profile and specialities updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ====================================================
        // AVAILABILITY
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .Include(w => w.Availabilities)
                .FirstOrDefaultAsync(w =>
                    w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound();
            }

            return View(worker);
        }

        // ====================================================
        // UPDATE GENERAL AVAILABILITY
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvailability(
            bool isAvailable)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w =>
                    w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound();
            }

            worker.IsAvailable = isAvailable;

            await _context.SaveChangesAsync();

            TempData["Success"] = isAvailable
                ? "You are now available for work."
                : "You are now unavailable for work.";

            return RedirectToAction(nameof(Availability));
        }

        // ====================================================
        // UPDATE WEEKLY AVAILABILITY
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWeeklyAvailability(
            int availabilityId,
            bool isAvailable,
            TimeSpan? startTime,
            TimeSpan? endTime)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var availability =
                await _context.WorkerAvailabilities
                    .Include(a => a.Worker)
                    .FirstOrDefaultAsync(a =>
                        a.WorkerAvailabilityId ==
                            availabilityId &&
                        a.Worker!.ApplicationUserId ==
                            user.Id);

            if (availability == null)
            {
                return NotFound();
            }

            availability.IsAvailable =
                isAvailable;

            if (isAvailable)
            {
                availability.StartTime =
                    startTime;

                availability.EndTime =
                    endTime;
            }
            else
            {
                availability.StartTime = null;
                availability.EndTime = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{availability.DayOfWeek} availability updated.";

            return RedirectToAction(nameof(Availability));
        }

        // ====================================================
        // UPLOAD RESUME
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResume(
            IFormFile resume)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w =>
                    w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound();
            }

            if (resume == null ||
                resume.Length == 0)
            {
                TempData["Error"] =
                    "Please select a resume.";

                return RedirectToAction(nameof(Index));
            }

            var extension =
                Path.GetExtension(resume.FileName)
                    .ToLowerInvariant();

            var allowedExtensions =
                new[]
                {
                    ".pdf",
                    ".doc",
                    ".docx"
                };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] =
                    "Only PDF, DOC and DOCX files are allowed.";

                return RedirectToAction(nameof(Index));
            }

            if (resume.Length >
                5 * 1024 * 1024)
            {
                TempData["Error"] =
                    "Resume must be less than 5 MB.";

                return RedirectToAction(nameof(Index));
            }

            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "resumes");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var fileName =
                Guid.NewGuid().ToString() +
                extension;

            var filePath =
                Path.Combine(
                    folder,
                    fileName);

            using (var stream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await resume.CopyToAsync(stream);
            }

            worker.ResumeFileName =
                resume.FileName;

            worker.ResumeFilePath =
                "/uploads/resumes/" +
                fileName;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Resume uploaded successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ====================================================
        // MY SERVICES
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> MyServices()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w =>
                    w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound();
            }

            var assignments =
                await _context.ServiceAssignments
                    .Include(sa => sa.ServiceRequest)
                        .ThenInclude(sr => sr.Vehicle)
                    .Where(sa =>
                        sa.WorkerId ==
                        worker.WorkerId)
                    .OrderByDescending(sa =>
                        sa.AssignedDate)
                    .ToListAsync();

            return View(assignments);
        }

        // ====================================================
        // ACCEPT TASK
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptTask(
            int assignmentId)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(w =>
                        w.ApplicationUserId ==
                        user.Id &&
                        w.Status == "Accepted");

            if (worker == null)
            {
                return Forbid();
            }

            var assignment =
                await _context.ServiceAssignments
                    .Include(sa => sa.ServiceRequest)
                    .FirstOrDefaultAsync(sa =>
                        sa.ServiceAssignmentId ==
                            assignmentId &&
                        sa.WorkerId ==
                            worker.WorkerId &&
                        sa.Status == "Pending");

            if (assignment == null)
            {
                TempData["Error"] =
                    "This task is no longer available.";

                return RedirectToAction(
                    nameof(MyServices));
            }

            assignment.Status =
                "Accepted";

            if (assignment.ServiceRequest != null)
            {
                assignment.ServiceRequest.Status =
                    "Assigned";
            }

            worker.IsAvailable = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Task accepted successfully.";

            return RedirectToAction(
                nameof(MyServices));
        }

        // ====================================================
        // REJECT TASK
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectTask(
            int assignmentId)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(w =>
                        w.ApplicationUserId ==
                        user.Id &&
                        w.Status == "Accepted");

            if (worker == null)
            {
                return Forbid();
            }

            var assignment =
                await _context.ServiceAssignments
                    .Include(sa => sa.ServiceRequest)
                    .FirstOrDefaultAsync(sa =>
                        sa.ServiceAssignmentId ==
                            assignmentId &&
                        sa.WorkerId ==
                            worker.WorkerId &&
                        sa.Status == "Pending");

            if (assignment == null)
            {
                TempData["Error"] =
                    "This task is no longer available.";

                return RedirectToAction(
                    nameof(MyServices));
            }

            assignment.Status =
                "Rejected";

            assignment.Notes =
                "Worker rejected this task.";

            if (assignment.ServiceRequest != null)
            {
                assignment.ServiceRequest.Status =
                    "Pending";
            }

            worker.IsAvailable = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Task rejected. The Manager can assign it to another worker.";

            return RedirectToAction(
                nameof(MyServices));
        }

        // ====================================================
        // START SERVICE
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartService(
            int assignmentId)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var assignment =
                await _context.ServiceAssignments
                    .Include(sa => sa.Worker)
                    .FirstOrDefaultAsync(sa =>
                        sa.ServiceAssignmentId ==
                            assignmentId &&
                        sa.Worker!.ApplicationUserId ==
                            user.Id &&
                        sa.Status == "Accepted");

            if (assignment == null)
            {
                return NotFound();
            }

            // Keep status as Accepted.
            // The new workflow uses:
            // Pending -> Accepted -> Completed.

            assignment.Notes =
                "Service started by worker.";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Service started successfully.";

            return RedirectToAction(
                nameof(MyServices));
        }

        // ====================================================
        // COMPLETE SERVICE
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteService(
            int assignmentId,
            string? notes)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var assignment =
                await _context.ServiceAssignments
                    .Include(sa => sa.Worker)
                    .Include(sa => sa.ServiceRequest)
                    .FirstOrDefaultAsync(sa =>
                        sa.ServiceAssignmentId ==
                            assignmentId &&
                        sa.Worker!.ApplicationUserId ==
                            user.Id &&
                        sa.Status == "Accepted");

            if (assignment == null)
            {
                return NotFound();
            }

            assignment.Status =
                "Completed";

            assignment.CompletedDate =
                DateTime.Now;

            if (!string.IsNullOrWhiteSpace(notes))
            {
                assignment.Notes =
                    notes.Trim();
            }

            if (assignment.ServiceRequest != null)
            {
                assignment.ServiceRequest.Status =
                    "Completed";
            }

            // Worker becomes available again
        workerAvailability:
            ;

            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(w =>
                        w.WorkerId ==
                        assignment.WorkerId);

            if (worker != null)
            {
                worker.IsAvailable = true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Service marked as completed.";

            return RedirectToAction(
                nameof(MyServices));
        }
    }
}