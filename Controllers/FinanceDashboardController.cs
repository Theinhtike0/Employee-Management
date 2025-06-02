using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_Products.Controllers
{
    public class FinanceDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
