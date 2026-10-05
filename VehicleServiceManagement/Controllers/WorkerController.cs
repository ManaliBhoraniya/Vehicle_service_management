using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Controllers
{
    [Authorize(Roles = "Worker,Manager,Admin")]
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

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (User.IsInRole("Manager") || User.IsInRole("Admin"))
            {
                return RedirectToAction("WorkerDetails", "ServiceManager", new { id = id });
            }

            var worker = await _context.Workers
                .Include(w => w.ApplicationUser)
                .Include(w => w.Specialities)
                .Include(w => w.Availabilities)
                .Include(w => w.ServiceAssignments)
                    .ThenInclude(sa => sa.ServiceRequest)
                .FirstOrDefaultAsync(w => w.WorkerId == id);

            if (worker == null)
            {
                return NotFound("Worker profile was not found.");
            }

            return View(worker);
        }

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

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Name is required.");
            }


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


            worker.ApplicationUser!.Name =
                name.Trim();

            worker.ApplicationUser.PhoneNumber =
                phone?.Trim();

            _context.WorkerSpecialities.RemoveRange(
                worker.Specialities);

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvailability(
            bool isAvailable,
            string? returnUrl = null)
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
                ? "You are now marked as Available for new work."
                : "You are now marked as Unavailable.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Availability));
        }

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

            ViewBag.IsAvailable = worker.IsAvailable;
            ViewBag.WorkerId = worker.WorkerId;

            var assignments =
                await _context.ServiceAssignments
                    .Include(sa => sa.ServiceRequest)
                        .ThenInclude(sr => sr!.Vehicle)
                            .ThenInclude(v => v!.Customer)
                    .Where(sa =>
                        sa.WorkerId ==
                        worker.WorkerId)
                    .OrderByDescending(sa =>
                        sa.AssignedDate)
                    .ToListAsync();

            return View(assignments);
        }

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

            // Worker accepts -> mark unavailable!
            worker.IsAvailable = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Work request accepted successfully. Your status is now marked as Unavailable.";

            return RedirectToAction(
                nameof(MyServices));
        }

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

            // Mark service request status as Rejected so manager sees it
            if (assignment.ServiceRequest != null)
            {
                assignment.ServiceRequest.Status =
                    "Rejected";
            }

            // Worker remains / becomes available
            worker.IsAvailable = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Task rejected. The Manager has been notified and can assign another worker.";

            return RedirectToAction(
                nameof(MyServices));
        }

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
                    .Include(sa => sa.ServiceRequest)
                    .FirstOrDefaultAsync(sa =>
                        sa.ServiceAssignmentId ==
                            assignmentId &&
                        sa.Worker!.ApplicationUserId ==
                            user.Id &&
                        (sa.Status == "Accepted" || sa.Status == "Pending"));

            if (assignment == null)
            {
                return NotFound();
            }

            assignment.Status = "Accepted";
            assignment.Notes = "Service started by worker.";

            if (assignment.ServiceRequest != null)
            {
                assignment.ServiceRequest.Status = "Assigned";
            }

            // Worker accepts/starts -> mark unavailable!
            if (assignment.Worker != null)
            {
                assignment.Worker.IsAvailable = false;
            }
            else
            {
                var w = await _context.Workers.FirstOrDefaultAsync(x => x.WorkerId == assignment.WorkerId);
                if (w != null)
                {
                    w.IsAvailable = false;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Service started successfully. Your status is now marked as Unavailable.";

            return RedirectToAction(
                nameof(MyServices));
        }

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
                        (sa.Status == "Accepted" || sa.Status == "In Progress" || sa.Status == "Pending"));

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
            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(w =>
                        w.WorkerId ==
                        assignment.WorkerId);

            if (worker != null)
            {
                worker.IsAvailable = false;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Service marked as completed successfully. Please manually mark yourself as 'Available' when you are ready to receive new service assignments.";

            return RedirectToAction(
                nameof(MyServices));
        }
    }
}