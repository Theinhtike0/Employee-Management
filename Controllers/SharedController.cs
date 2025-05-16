using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_Products.Controllers
{
    [Authorize]
    public class SharedController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
