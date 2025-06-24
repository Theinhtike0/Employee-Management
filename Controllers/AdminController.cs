using HR_Products.Data;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_Products.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AdminController(AppDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }
        public IActionResult Dashboard()
        {
            var model = new DashboardCountsViewModel
            {
                LeaveRequestCount = _context.LEAV_REQUESTS.Count(l => l.Status == "Pending"),

                PayrollTransactionCount = _context.PAYROLLS.Count(),

                PensionCount = _context.PENSION.Count(p => p.Status == "Pending"),

                //AttendanceCount = _context.ATTENDANCE.Count(),

                PendingLeaves = _context.LEAV_REQUESTS
                    .Include(l => l.Employee)
                    .Where(l => l.Status == "Pending")
                    .Select(l => new LeaveViewModel
                    {
                        EmployeeName = l.Employee != null ? l.Employee.EmpeName : "Unknown",
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        RequestedAt = l.RequestedAt,
                        Duration = l.Duration,
                        Status = l.Status
                    }).ToList(),

                PendingPension = _context.PENSION
                    .Where(p => p.Status == "Pending")
                    .Join(_context.EMPE_PROFILE,
                        p => p.EmpeId,
                        e => e.EmpeId,
                        (pension, employee) => new PensionViewModel
                        {
                            EmployeeName = employee.EmpeName ?? "Unknown",
                            Department = employee.PostalCode ?? "Not Specified",
                            Position = employee.Status ?? "Not Specified",
                            Reason = pension.Reason.HasValue ?
                                   pension.Reason.Value.ToString() :
                                   "Not Specified",
                            Age = employee.DateOfBirth.HasValue ?
                                 CalculateAge(employee.DateOfBirth.Value, DateTime.Today) : 0,
                            ServiceYears = CalculateServiceYears(employee.JoinDate, DateTime.Today),
                            Status = pension.Status
                        })
                    .ToList()
            };

            return View(model);
        }

        private static int CalculateAge(DateTime dateOfBirth, DateTime currentDate)
        {
            int age = currentDate.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > currentDate.AddYears(-age)) age--;
            return age;
        }

        private static int CalculateServiceYears(DateTime joinDate, DateTime currentDate)
        {
            int years = currentDate.Year - joinDate.Year;
            if (joinDate.Date > currentDate.AddYears(-years)) years--;
            return years;
        }

        public IActionResult ManageUsers()
        {
            return View();
        }
    }
}
