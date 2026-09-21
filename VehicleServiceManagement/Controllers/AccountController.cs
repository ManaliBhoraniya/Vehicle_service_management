using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        // =========================
        // LOGIN
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

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

            // =========================
            // ROLE BASED REDIRECTION
            // =========================

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction(
                    "Index",
                    "ServiceManager");
            }

            if (await _userManager.IsInRoleAsync(user, "Worker"))
            {
                return RedirectToAction(
                    "Index",
                    "Worker");
            }

            if (await _userManager.IsInRoleAsync(user, "Customer"))
            {
                return RedirectToAction(
                    "Index",
                    "Customer");
            }

            // User has no valid application role
            await _signInManager.SignOutAsync();

            ModelState.AddModelError(
                "",
                "Your account does not have a valid role.");

            return View();
        }

        // =========================
        // REGISTER
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string name,
            string email,
            string password,
            string confirmPassword,
            string role,
            string profession)
        {
            // -------------------------
            // Basic validation
            // -------------------------

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

            // -------------------------
            // Only Customer and Worker
            // can be selected publicly
            // -------------------------

            if (role != "Customer" &&
                role != "Worker")
            {
                ModelState.AddModelError(
                    "Role",
                    "Invalid registration role.");

                return View();
            }

            // -------------------------
            // Worker profession
            // -------------------------

            if (role == "Worker" &&
                string.IsNullOrWhiteSpace(profession))
            {
                ModelState.AddModelError(
                    "Profession",
                    "Profession is required for workers.");

                return View();
            }

            // -------------------------
            // Check existing email
            // -------------------------

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View();
            }

            // -------------------------
            // Create Identity user
            // -------------------------

            var user = new ApplicationUser
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

            // -------------------------
            // Assign selected role
            // -------------------------

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

                // Remove the user if role assignment fails
                await _userManager.DeleteAsync(user);

                return View();
            }

            // -------------------------
            // Create Worker record
            // -------------------------

            if (role == "Worker")
            {
                var worker = new Worker
                {
                    ApplicationUserId = user.Id,
                    Profession = profession,
                    IsAvailable = true
                };

                _context.Workers.Add(worker);

                await _context.SaveChangesAsync();
            }

            // -------------------------
            // Login automatically
            // -------------------------

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            // -------------------------
            // Redirect according to role
            // -------------------------

            if (role == "Worker")
            {
                return RedirectToAction(
                    "Index",
                    "Worker");
            }

            return RedirectToAction(
                "Index",
                "Customer");
        }

        // =========================
        // LOGOUT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // =========================
        // ACCESS DENIED
        // =========================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}