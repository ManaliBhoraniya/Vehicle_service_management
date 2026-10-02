using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // =====================================================
        // LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // =====================================================
        // LOGIN - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "",
                    "Email and password are required.");

                return View();
            }

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View();
            }

            // =================================================
            // CHECK WORKER JOB REQUEST STATUS
            // =================================================

            if (await _userManager.IsInRoleAsync(
                    user,
                    "Worker"))
            {
                var worker =
                    await _context.Workers
                        .FirstOrDefaultAsync(
                            w =>
                                w.ApplicationUserId ==
                                user.Id);

                if (worker != null)
                {
                    // Worker is waiting for manager approval

                    if (worker.Status == "Pending")
                    {
                        ModelState.AddModelError(
                            "",
                            "Your job request is still pending. Please wait for the Service Manager to review your application.");

                        return View();
                    }

                    // Worker was rejected

                    if (worker.Status == "Rejected")
                    {
                        ModelState.AddModelError(
                            "",
                            "Your job request has been rejected. You cannot log in with this account.");

                        return View();
                    }

                    // Worker must be accepted

                    if (worker.Status != "Accepted")
                    {
                        ModelState.AddModelError(
                            "",
                            "Your worker account is not active.");

                        return View();
                    }
                }
            }

            // =================================================
            // PASSWORD LOGIN
            // =================================================

            var result =
                await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    password,
                    rememberMe,
                    lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View();
            }

            // =================================================
            // MANAGER
            // =================================================

            if (await _userManager.IsInRoleAsync(
                    user,
                    "Manager"))
            {
                return RedirectToAction(
                    "Index",
                    "ServiceManager");
            }

            // =================================================
            // ADMIN
            // =================================================

            if (await _userManager.IsInRoleAsync(
                    user,
                    "Admin"))
            {
                return RedirectToAction(
                    "Index",
                    "ServiceManager");
            }

            // =================================================
            // WORKER
            // =================================================

            if (await _userManager.IsInRoleAsync(
                    user,
                    "Worker"))
            {
                return RedirectToAction(
                    "Dashboard",
                    "Worker");
            }

            // =================================================
            // CUSTOMER
            // =================================================

            if (await _userManager.IsInRoleAsync(
                    user,
                    "Customer"))
            {
                // Customer goes to Dashboard after login
                return RedirectToAction(
                    "Dashboard",
                    "Customer");
            }

            // =================================================
            // INVALID ROLE
            // =================================================

            await _signInManager.SignOutAsync();

            ModelState.AddModelError(
                "",
                "Your account does not have a valid role.");

            return View();
        }

        // =====================================================
        // REGISTER - GET
        // =====================================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // =====================================================
        // REGISTER - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string name,
            string email,
            string password,
            string confirmPassword,
            string role,
            List<string>? specialities,
            IFormFile? resume)
        {
            // =================================================
            // BASIC VALIDATION
            // =================================================

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError(
                    "",
                    "All required fields must be filled.");

                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Passwords do not match.");

                return View();
            }

            // =================================================
            // ALLOWED REGISTRATION ROLES
            // =================================================
            //
            // Manager is NOT allowed to register.
            // Only Customer and Worker can register.
            // =================================================

            if (role != "Customer" &&
                role != "Worker")
            {
                ModelState.AddModelError(
                    "Role",
                    "Only Customer and Worker accounts can be registered.");

                return View();
            }

            // =================================================
            // WORKER VALIDATION
            // =================================================

            if (role == "Worker")
            {
                // At least one speciality is required

                if (specialities == null ||
                    specialities.Count == 0)
                {
                    ModelState.AddModelError(
                        "Specialities",
                        "Please select at least one speciality.");

                    return View();
                }

                // Resume is required

                if (resume == null ||
                    resume.Length == 0)
                {
                    ModelState.AddModelError(
                        "Resume",
                        "Resume is required for worker registration.");

                    return View();
                }

                // Maximum 5 MB

                if (resume.Length >
                    5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "Resume",
                        "Resume file must be smaller than 5 MB.");

                    return View();
                }

                // Allowed file extensions

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
                    ModelState.AddModelError(
                        "Resume",
                        "Only PDF, DOC and DOCX files are allowed.");

                    return View();
                }
            }

            // =================================================
            // CHECK EXISTING USER
            // =================================================

            var existingUser =
                await _userManager.FindByEmailAsync(
                    email);

            if (existingUser != null)
            {
                // Check whether this email belongs to
                // a rejected worker.

                var rejectedWorker =
                    await _context.Workers
                        .FirstOrDefaultAsync(
                            w =>
                                w.ApplicationUserId ==
                                existingUser.Id &&
                                w.Status == "Rejected");

                if (rejectedWorker != null)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email belongs to a rejected worker account and cannot be registered again.");

                    return View();
                }

                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View();
            }

            // =================================================
            // CREATE IDENTITY USER
            // =================================================

            var user =
                new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = name
                };

            var result =
                await _userManager.CreateAsync(
                    user,
                    password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View();
            }

            // =================================================
            // ASSIGN ROLE
            // =================================================

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    role);

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                await _userManager.DeleteAsync(
                    user);

                return View();
            }

            // =================================================
            // WORKER JOB REQUEST
            // =================================================

            if (role == "Worker")
            {
                // ---------------------------------------------
                // CREATE RESUME UPLOAD FOLDER
                // ---------------------------------------------

                var uploadsFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "resumes");

                if (!Directory.Exists(
                        uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }

                // ---------------------------------------------
                // GET FILE EXTENSION
                // ---------------------------------------------

                var extension =
                    Path.GetExtension(
                        resume!.FileName)
                        .ToLowerInvariant();

                // ---------------------------------------------
                // CREATE UNIQUE FILE NAME
                // ---------------------------------------------

                var uniqueFileName =
                    Guid.NewGuid().ToString() +
                    extension;

                var filePath =
                    Path.Combine(
                        uploadsFolder,
                        uniqueFileName);

                // ---------------------------------------------
                // SAVE RESUME
                // ---------------------------------------------

                using (var stream =
                       new FileStream(
                           filePath,
                           FileMode.Create))
                {
                    await resume.CopyToAsync(
                        stream);
                }

                // ---------------------------------------------
                // CREATE WORKER
                // ---------------------------------------------

                var worker =
                    new Worker
                    {
                        ApplicationUserId =
                            user.Id,

                        IsAvailable =
                            false,

                        ResumeFileName =
                            resume.FileName,

                        ResumeFilePath =
                            "/uploads/resumes/" +
                            uniqueFileName,

                        Status =
                            "Pending"
                    };

                _context.Workers.Add(
                    worker);

                // Save first so WorkerId is generated

                await _context.SaveChangesAsync();

                // ---------------------------------------------
                // SAVE WORKER SPECIALITIES
                // ---------------------------------------------

                foreach (var speciality in specialities!)
                {
                    if (!string.IsNullOrWhiteSpace(
                            speciality))
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

                // ---------------------------------------------
                // WORKER MUST WAIT FOR MANAGER APPROVAL
                // ---------------------------------------------

                return RedirectToAction(
                    "JobRequestSubmitted",
                    "Account");
            }

            // =================================================
            // CUSTOMER
            // =================================================

            if (role == "Customer")
            {
                // ---------------------------------------------
                // CREATE CUSTOMER PROFILE
                // ---------------------------------------------

                var customer =
                    new Customer
                    {
                        ApplicationUserId =
                            user.Id,

                        Name =
                            name
                    };

                _context.Customers.Add(
                    customer);

                await _context.SaveChangesAsync();

                // ---------------------------------------------
                // SIGN IN CUSTOMER
                // ---------------------------------------------

                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false);

                // ---------------------------------------------
                // CUSTOMER DASHBOARD
                // ---------------------------------------------

                return RedirectToAction(
                    "Dashboard",
                    "Customer");
            }

            // =================================================
            // FALLBACK
            // =================================================

            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // =====================================================
        // JOB REQUEST SUBMITTED
        // =====================================================

        [HttpGet]
        public IActionResult JobRequestSubmitted()
        {
            return View();
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // =====================================================
        // DELETE ACCOUNT
        // =====================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user =
                await _userManager.GetUserAsync(
                    User);

            if (user == null)
            {
                return Challenge();
            }

            // ---------------------------------------------
            // REMOVE WORKER
            // ---------------------------------------------

            var worker =
                await _context.Workers
                    .Include(w => w.Specialities)
                    .FirstOrDefaultAsync(
                        w =>
                            w.ApplicationUserId ==
                            user.Id);

            if (worker != null)
            {
                // Remove worker specialities first

                if (worker.Specialities != null)
                {
                    _context.WorkerSpecialities
                        .RemoveRange(
                            worker.Specialities);
                }

                _context.Workers.Remove(
                    worker);
            }

            // ---------------------------------------------
            // REMOVE CUSTOMER
            // ---------------------------------------------

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c =>
                            c.ApplicationUserId ==
                            user.Id);

            if (customer != null)
            {
                _context.Customers.Remove(
                    customer);
            }

            await _context.SaveChangesAsync();

            // ---------------------------------------------
            // REMOVE IDENTITY USER
            // ---------------------------------------------

            var result =
                await _userManager.DeleteAsync(
                    user);

            if (!result.Succeeded)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Account");
            }

            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // =====================================================
        // ACCESS DENIED
        // =====================================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}