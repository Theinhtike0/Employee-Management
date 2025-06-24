using HR_Products.ViewModels;
using Microsoft.AspNetCore.Mvc;
using HR_Products.Data;
using Microsoft.EntityFrameworkCore;
using HR_Products.Models.Entitites;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace HR_Products.Controllers
{
    public class LeaveBalanceController : Controller
    {
        private readonly AppDbContext _context;

        public LeaveBalanceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var currentUser = GetCurrentUser();
            bool isRegularUser = !User.IsInRole("Admin") && !User.IsInRole("HR-Admin");

            ViewBag.IsRegularUser = isRegularUser;
            ViewBag.CurrentUser = currentUser;
            ViewBag.Employees = new SelectList(_context.EMPE_PROFILE.ToList(), "EmpeId", "EmpeName");
            ViewBag.LeaveTypes = new SelectList(_context.LEAV_TYPE.ToList(), "LEAV_TYPE_ID", "LEAV_TYPE_NAME");

            return View(new List<LeaveBalanceViewModel>());
        }

        private EmployeeProfile GetCurrentUser()
        {
            var userEmail = HttpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return null;
            }

            var employees = _context.EMPE_PROFILE.Where(e => e.Email != null).ToList();
            var employee = employees.FirstOrDefault(e =>
                e.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase));

            if (employee == null)
            {
                System.Diagnostics.Debug.WriteLine($"No employee found for email: {userEmail}");
                throw new InvalidOperationException($"Employee not found for email: {userEmail}");
            }

            return employee;
        }



        [HttpPost]
        public IActionResult Calculate(int empeId, int leaveTypeId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                bool isRegularUser = !User.IsInRole("Admin") && !User.IsInRole("HR-Admin");
                if (isRegularUser && currentUser != null)
                {
                    empeId = currentUser.EmpeId;
                }
                IQueryable<EmployeeProfile> employeeQuery = _context.EMPE_PROFILE;

                if (empeId != 0)
                {
                    employeeQuery = employeeQuery.Where(e => e.EmpeId == empeId);
                }

                var employees = employeeQuery.ToList();
                IQueryable<LeaveType> leaveTypeQuery = _context.LEAV_TYPE;

                if (leaveTypeId != 0)
                {
                    leaveTypeQuery = leaveTypeQuery.Where(lt => lt.LEAV_TYPE_ID == leaveTypeId);
                }

                var leaveTypes = leaveTypeQuery.ToList();
                var results = new List<LeaveBalanceViewModel>();
                int currentYear = DateTime.Now.Year; // Get current year

                foreach (var employee in employees)
                {
                    var groupedBalances = leaveTypes.Select(lt => new LeaveBalanceDetail
                    {
                        LeaveType = lt.LEAV_TYPE_NAME,
                        Balance = lt.DEFAULT_DAY_PER_YEAR,
                        
                    }).ToList();

                    results.Add(new LeaveBalanceViewModel
                    {
                        EmpeId = employee.EmpeId,
                        EmpeName = employee.EmpeName,
                        Year = currentYear,
                        Age = CalculateAge(employee.DateOfBirth),
                        ServiceYear = CalculateServiceYears(employee.JoinDate),
                        LeaveBalances = groupedBalances
                    });
                }

                ViewBag.IsRegularUser = isRegularUser;
                ViewBag.CurrentUser = currentUser;
                ViewBag.Employees = new SelectList(_context.EMPE_PROFILE.ToList(), "EmpeId", "EmpeName", empeId);
                ViewBag.LeaveTypes = new SelectList(_context.LEAV_TYPE.ToList(), "LEAV_TYPE_ID", "LEAV_TYPE_NAME", leaveTypeId);

                return View("Index", results);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error calculating balances: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private int CalculateAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue) return 0;
            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;
            return age;
        }

        private int CalculateServiceYears(DateTime? joinDate)
        {
            if (!joinDate.HasValue) return 0;
            return DateTime.Today.Year - joinDate.Value.Year;
        }

    }

}
