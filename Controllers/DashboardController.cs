using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_Products.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return Ok(new { Message = "Welcome Admin!" });
        }

        [HttpGet("hr")]
        [Authorize(Roles = "HR-Admin")]
        public IActionResult HR()
        {
            return Ok(new { Message = "Welcome HR Admin!" });
        }

        [HttpGet("finance")]
        [Authorize(Roles = "Finance-Admin")]
        public IActionResult Finance()
        {
            return Ok(new { Message = "Welcome Finance Admin!" });
        }

        [HttpGet("user")]
        [Authorize(Roles = "User")]
        public IActionResult User()
        {
            return Ok(new { Message = "Welcome User!" });
        }
    }
}