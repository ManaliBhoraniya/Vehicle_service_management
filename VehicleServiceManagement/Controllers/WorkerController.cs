using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Controllers
{
    [Authorize(Roles = "Admin,Worker")]
    public class WorkerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Worker
        public async Task<IActionResult> Index()
        {
            var workers = await _context.Workers
                .Include(w => w.User)
                .ToListAsync();

            return View(workers);
        }

        // GET: /Worker/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var worker = await _context.Workers
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (worker == null)
                return NotFound();

            return View(worker);
        }

        // GET: /Worker/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Worker/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Worker worker)
        {
            if (!ModelState.IsValid)
                return View(worker);

            _context.Workers.Add(worker);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Worker/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.Id == id);

            if (worker == null)
                return NotFound();

            return View(worker);
        }

        // POST: /Worker/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Worker worker)
        {
            if (id != worker.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(worker);

            _context.Workers.Update(worker);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Worker/Delete/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.Id == id);

            if (worker == null)
                return NotFound();

            return View(worker);
        }

        // POST: /Worker/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.Id == id);

            if (worker == null)
                return NotFound();

            _context.Workers.Remove(worker);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}