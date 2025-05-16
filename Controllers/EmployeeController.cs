using Microsoft.AspNetCore.Mvc;
using HR_Products.Models.Entitites;
using System.Threading.Tasks;
using HR_Products.Data;
using Microsoft.EntityFrameworkCore;

namespace HR_Products.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Employee/Index
        public async Task<IActionResult> Index()
        {
            var employees = await _context.EMPE_PROFILE.ToListAsync();
            var employeesWithIndex = employees
                .Select((employee, index) => new { Employee = employee, Index = index + 1 })
                .ToList();

            return View(employees);
        }

        // GET: Employee/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        public async Task<IActionResult> Create(EmployeeProfile empeprofile)
        {
            // Check if an employee profile with the same EMPE_ID already exists.
            var existingProfile = await _context.EMPE_PROFILE
                .FirstOrDefaultAsync(ep => ep.EmpeCode == empeprofile.EmpeCode);

            if (existingProfile != null)
            {
                ModelState.AddModelError("EmpeCode", "An employee profile with this EMPE ID already exists.");
                return View(empeprofile); // Return the empeprofile to the View to display the error
            }

            if (!ModelState.IsValid)
            {
                return View(empeprofile);
            }

            empeprofile.CreatedAt = DateTime.Now;
            empeprofile.UpdatedAt = DateTime.Now;

            _context.EMPE_PROFILE.Add(empeprofile);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Employee/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.EMPE_PROFILE.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeProfile employeeProfile)
        {
            if (!ModelState.IsValid)
            {
                return View(employeeProfile);
            }

            var existingEmployee = await _context.EMPE_PROFILE.FindAsync(id);
            if (existingEmployee == null)
            {
                return NotFound();
            }

            existingEmployee.EmpeCode = employeeProfile.EmpeCode;
            existingEmployee.EmpeName = employeeProfile.EmpeName;
            existingEmployee.DateOfBirth = employeeProfile.DateOfBirth;
            existingEmployee.Gender = employeeProfile.Gender;
            existingEmployee.Email = employeeProfile.Email;
            existingEmployee.PhoneNo = employeeProfile.PhoneNo;
            existingEmployee.EmgcConctName = employeeProfile.EmgcConctName;
            existingEmployee.EmgcConctPhone = employeeProfile.EmgcConctPhone;
            existingEmployee.JobTitle = employeeProfile.JobTitle;
            existingEmployee.DeptId = employeeProfile.DeptId;
            existingEmployee.JoinDate = employeeProfile.JoinDate;
            existingEmployee.Status = employeeProfile.Status;
            existingEmployee.TerminateDate = employeeProfile.TerminateDate;
            existingEmployee.City = employeeProfile.City;
            existingEmployee.State = employeeProfile.State;
            existingEmployee.PostalCode = employeeProfile.PostalCode;
            existingEmployee.Country = employeeProfile.Country;
            existingEmployee.ProfilePic = employeeProfile.ProfilePic;
            existingEmployee.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Employee/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.EMPE_PROFILE.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.EMPE_PROFILE.Remove(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Employee/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.EMPE_PROFILE.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }
    }
}