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

        public async Task<IActionResult> Dashboard()
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
                    .Include(w => w.ApplicationUser)
                    .Include(w => w.Availabilities)
                    .Include(w => w.ServiceAssignments)
                        .ThenInclude(sa =>
                            sa.ServiceRequest)
                    .FirstOrDefaultAsync(w =>
                        w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound(
                    "Worker profile was not found.");
            }

            return View(worker);
        }

        // ====================================================
        // PROFILE
        // ====================================================

        public async Task<IActionResult> Profile()
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
                    .Include(w => w.ApplicationUser)
                    .Include(w => w.Availabilities)
                    .FirstOrDefaultAsync(w =>
                        w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound(
                    "Worker profile was not found.");
            }

            return View(worker);
        }

        // ====================================================
        // EDIT PROFILE
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> EditProfile()
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
                    .Include(w => w.ApplicationUser)
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
        public async Task<IActionResult> EditProfile(
            int workerId,
            string name,
            string? phone,
            string profession)
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
                    .Include(w => w.ApplicationUser)
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

            if (string.IsNullOrWhiteSpace(profession))
            {
                ModelState.AddModelError(
                    "Profession",
                    "Profession is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(worker);
            }

            worker.ApplicationUser!.Name = name;

            worker.ApplicationUser.PhoneNumber = phone;

            worker.Profession = profession;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Profile updated successfully.";

            return RedirectToAction(
                nameof(Profile));
        }

        // ====================================================
        // AVAILABILITY
        // ====================================================

        public async Task<IActionResult> Availability()
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
        public async Task<IActionResult>
            UpdateAvailability(bool isAvailable)
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
                        w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound();
            }

            worker.IsAvailable = isAvailable;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                isAvailable
                ? "You are now available for work."
                : "You are now unavailable for work.";

            return RedirectToAction(
                nameof(Availability));
        }

        // ====================================================
        // UPDATE WEEKLY AVAILABILITY
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            UpdateWeeklyAvailability(
                int availabilityId,
                bool isAvailable,
                TimeSpan? startTime,
                TimeSpan? endTime)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
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
                availability.StartTime = startTime;
                availability.EndTime = endTime;
            }
            else
            {
                availability.StartTime = null;
                availability.EndTime = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{availability.DayOfWeek} availability updated.";

            return RedirectToAction(
                nameof(Availability));
        }

        // ====================================================
        // UPLOAD RESUME
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            UploadResume(IFormFile resume)
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

                return RedirectToAction(
                    nameof(Profile));
            }

            var extension =
                Path.GetExtension(
                    resume.FileName)
                    .ToLowerInvariant();

            var allowedExtensions =
                new[]
                {
                    ".pdf",
                    ".doc",
                    ".docx"
                };

            if (!allowedExtensions.Contains(
                    extension))
            {
                TempData["Error"] =
                    "Only PDF, DOC and DOCX files are allowed.";

                return RedirectToAction(
                    nameof(Profile));
            }

            if (resume.Length >
                5 * 1024 * 1024)
            {
                TempData["Error"] =
                    "Resume must be less than 5 MB.";

                return RedirectToAction(
                    nameof(Profile));
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
                Guid.NewGuid()
                    .ToString() +
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

            return RedirectToAction(
                nameof(Profile));
        }

        // ====================================================
        // MY SERVICES
        // ====================================================

        public async Task<IActionResult> MyServices()
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
                        w.ApplicationUserId == user.Id);

            if (worker == null)
            {
                return NotFound();
            }

            var assignments =
                await _context.ServiceAssignments
                    .Include(sa =>
                        sa.ServiceRequest)
                    .Where(sa =>
                        sa.WorkerId ==
                        worker.WorkerId)
                    .OrderByDescending(sa =>
                        sa.AssignedDate)
                    .ToListAsync();

            return View(assignments);
        }

        // ====================================================
        // MARK SERVICE AS IN PROGRESS
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            StartService(int assignmentId)
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
                            user.Id);

            if (assignment == null)
            {
                return NotFound();
            }

            assignment.Status =
                "In Progress";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Service started successfully.";

            return RedirectToAction(
                nameof(MyServices));
        }

        // ====================================================
        // MARK SERVICE AS COMPLETED
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            CompleteService(int assignmentId)
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
                            user.Id);

            if (assignment == null)
            {
                return NotFound();
            }

            assignment.Status =
                "Completed";

            assignment.CompletedDate =
                DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Service marked as completed.";

            return RedirectToAction(
                nameof(MyServices));
        }
    }
}