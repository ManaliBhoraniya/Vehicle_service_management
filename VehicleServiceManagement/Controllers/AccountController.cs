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

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
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
            // MANAGER
            // =========================

            if (await _userManager.IsInRoleAsync(user, "Manager"))
            {
                return RedirectToAction(
                    "Index",
                    "ServiceManager");
            }

            // =========================
            // ADMIN
            // =========================

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction(
                    "Index",
                    "ServiceManager");
            }

            // =========================
            // WORKER
            // =========================

            if (await _userManager.IsInRoleAsync(user, "Worker"))
            {
                return RedirectToAction(
                    "Dashboard",
                    "Worker");
            }

            // =========================
            // CUSTOMER
            // =========================

            if (await _userManager.IsInRoleAsync(user, "Customer"))
            {
                return RedirectToAction(
                    "Dashboard",
                    "Customer");
            }

            // =========================
            // INVALID ROLE
            // =========================

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

            // =========================
            // PASSWORD CONFIRMATION
            // =========================

            if (password != confirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Passwords do not match.");

                return View();
            }

            // =========================
            // ALLOWED ROLES
            // =========================

            if (role != "Customer" &&
                role != "Worker" &&
                role != "Manager")
            {
                ModelState.AddModelError(
                    "Role",
                    "Invalid registration role.");

                return View();
            }

            // =========================
            // WORKER PROFESSION
            // =========================

            if (role == "Worker" &&
                string.IsNullOrWhiteSpace(profession))
            {
                ModelState.AddModelError(
                    "Profession",
                    "Profession is required for workers.");

                return View();
            }

            // =========================
            // CHECK EXISTING USER
            // =========================

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View();
            }

            // =========================
            // CREATE USER
            // =========================

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = name
            };

            var result = await _userManager.CreateAsync(
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

            // =========================
            // ASSIGN ROLE
            // =========================

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

                // Delete the user if role assignment failed
                await _userManager.DeleteAsync(user);

                return View();
            }

            // =========================
            // CREATE WORKER PROFILE
            // =========================

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

                // =========================
                // DEFAULT WEEKLY AVAILABILITY
                // =========================

                string[] days =
                {
                    "Monday",
                    "Tuesday",
                    "Wednesday",
                    "Thursday",
                    "Friday",
                    "Saturday",
                    "Sunday"
                };

                foreach (var day in days)
                {
                    var availability =
                        new WorkerAvailability
                        {
                            WorkerId = worker.WorkerId,

                            DayOfWeek = day,

                            IsAvailable =
                                day != "Sunday",

                            StartTime =
                                day != "Sunday"
                                    ? new TimeSpan(9, 0, 0)
                                    : null,

                            EndTime =
                                day != "Sunday"
                                    ? new TimeSpan(18, 0, 0)
                                    : null
                        };

                    _context.WorkerAvailabilities.Add(
                        availability);
                }

                await _context.SaveChangesAsync();
            }

            // =========================
            // SIGN IN AFTER REGISTRATION
            // =========================

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            // =========================
            // MANAGER
            // =========================

            if (role == "Manager")
            {
                return RedirectToAction(
                    "Index",
                    "ServiceManager");
            }

            // =========================
            // ADMIN
            // =========================

            if (role == "Admin")
            {
                return RedirectToAction(
                    "Index",
                    "ServiceManager");
            }

            // =========================
            // WORKER
            // =========================

            if (role == "Worker")
            {
                return RedirectToAction(
                    "Dashboard",
                    "Worker");
            }

            // =========================
            // CUSTOMER
            // =========================

            if (role == "Customer")
            {
                return RedirectToAction(
                    "Dashboard",
                    "Customer");
            }

            // Fallback
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
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
        // DELETE ACCOUNT
        // =========================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // =========================
            // DELETE WORKER PROFILE
            // =========================

            var worker =
                await _context.Workers
                    .FirstOrDefaultAsync(
                        w => w.ApplicationUserId == user.Id);

            if (worker != null)
            {
                _context.Workers.Remove(worker);
            }

            // =========================
            // DELETE CUSTOMER PROFILE
            // =========================

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(
                        c => c.ApplicationUserId == user.Id);

            if (customer != null)
            {
                _context.Customers.Remove(customer);
            }

            // =========================
            // SAVE PROFILE DELETION
            // =========================

            await _context.SaveChangesAsync();

            // =========================
            // DELETE IDENTITY ACCOUNT
            // =========================

            var result =
                await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return RedirectToAction(
                    "AccessDenied",
                    "Account");
            }

            // =========================
            // SIGN OUT
            // =========================

            await _signInManager.SignOutAsync();

            // =========================
            // GO TO LOGIN
            // =========================

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

