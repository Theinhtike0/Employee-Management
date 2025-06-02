using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_Products.Controllers
{
    [Authorize(Roles = "HR-Admin")]
    public class HrDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
