using Microsoft.AspNetCore.Mvc;

namespace HR_Products.Controllers
{
    public class UserDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
