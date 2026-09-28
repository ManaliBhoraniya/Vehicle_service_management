using Microsoft.AspNetCore.Mvc;

namespace VehicleServiceManagement.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }
    }
}