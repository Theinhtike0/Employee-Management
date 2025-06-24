using Microsoft.AspNetCore.Mvc;
using HR_Products.Models.Entitites;
using System.Threading.Tasks;
using HR_Products.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; 
using System.IO;
using HR_Products.ViewModels;


namespace HR_Products.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public EmployeeController(AppDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }


        public async Task<IActionResult> Index()
        {
            var employees = await _context.EMPE_PROFILE.ToListAsync();

            var viewModel = new EmployeeListViewModel
            {
                Employees = employees,
                IsAdminOrHrAdmin = User.IsInRole("Admin") || User.IsInRole("HR-Admin"),
                IsFinanceAdmin = User.IsInRole("Finance-Admin")
            };

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult Create()
        {

            var newEmployeeProfile = new EmployeeProfile();
            newEmployeeProfile.JoinDate = DateTime.Today;
            return View(newEmployeeProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(EmployeeProfile empeprofile, IFormFile ProfilePicFile)
        {

            var existingProfile = await _context.EMPE_PROFILE
                .FirstOrDefaultAsync(ep => ep.EmpeCode == empeprofile.EmpeCode);

            if (existingProfile != null)
            {
                ModelState.AddModelError("EmpeCode", "An employee profile with this EMPE ID already exists.");

                return View(empeprofile);
            }
            if (ProfilePicFile != null && ProfilePicFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images", "profile");


                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(ProfilePicFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePicFile.CopyToAsync(stream);
                }

                empeprofile.ProfilePic = fileName;
            }
            else
            {

            }

            empeprofile.DeptId = 1;
            empeprofile.CreatedAt = DateTime.Now;
            empeprofile.UpdatedAt = DateTime.Now;

            _context.EMPE_PROFILE.Add(empeprofile);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeProfile employeeProfile, IFormFile ProfilePicFile)
        {


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
            existingEmployee.UpdatedAt = DateTime.Now;

            if (ProfilePicFile != null && ProfilePicFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images", "profile");

                Directory.CreateDirectory(uploadsFolder);

                if (!string.IsNullOrEmpty(existingEmployee.ProfilePic))
                {
                    var oldFilePath = Path.Combine(uploadsFolder, existingEmployee.ProfilePic);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(ProfilePicFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePicFile.CopyToAsync(stream);
                }

                existingEmployee.ProfilePic = fileName;
            }

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeProfileExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool EmployeeProfileExists(int id)
        {
            return _context.EMPE_PROFILE.Any(e => e.EmpeId == id);
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.EMPE_PROFILE.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }


            if (!string.IsNullOrEmpty(employee.ProfilePic))
            {
                var imagePath = Path.Combine(_hostingEnvironment.WebRootPath, "images", "profile", employee.ProfilePic);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.EMPE_PROFILE.Remove(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeProfile = await _context.EMPE_PROFILE
                .FirstOrDefaultAsync(m => m.EmpeId == id);

            if (employeeProfile == null)
            {
                return NotFound();
            }

            return View(employeeProfile);
        }

        public async Task<IActionResult> Profile()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return NotFound("Employee not found.");
            }

            return RedirectToAction("Detail", new { id = currentUser.EmpeId });
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

        public async Task<IActionResult> Adjustment()
        {
            try
            {
                var currentUser = GetCurrentUser();
                bool isAdminRoleUser = User.IsInRole("Admin") || User.IsInRole("HR-Admin");
                bool isFinanceAdminByJobTitle = (currentUser?.JobTitle == "Finance-Admin");
                bool canSeeAllRecords = isAdminRoleUser || isFinanceAdminByJobTitle;

                IQueryable<EmployeeProfile> employeeQuery = _context.EMPE_PROFILE;

                if (!canSeeAllRecords)
                {
                    
                    if (currentUser == null)
                    {
                        TempData["ErrorMessage"] = "Your employee profile could not be found. Please log in again.";
                        return View(new List<EmployeeAdjustmentViewModel>());
                    }
                    employeeQuery = employeeQuery.Where(e => e.EmpeId == currentUser.EmpeId);
                }

                var adjustmentList = await employeeQuery
                    .Select(employee => new
                    {
                        
                        Employee = employee, 
                        CalculatedServiceYear = (DateTime.Now.Year - employee.JoinDate.Year) -
                                                (employee.JoinDate.Date > DateTime.Now.AddYears(-(DateTime.Now.Year - employee.JoinDate.Year)).Date ? 1 : 0)
                    })
                    .Where(x => x.CalculatedServiceYear % 2 == 0)
                    .Select(x => new EmployeeAdjustmentViewModel
                    {
                        EmpeId = x.Employee.EmpeId,
                        ProfilePic = x.Employee.ProfilePic,
                        EmpeName = x.Employee.EmpeName,
                        Email = x.Employee.Email,
                        JoinDate = x.Employee.JoinDate,
                        DateOfBirth = x.Employee.DateOfBirth,
                        Age = x.Employee.DateOfBirth.HasValue
                                ? (DateTime.Now.Year - x.Employee.DateOfBirth.Value.Year) -
                                  (x.Employee.DateOfBirth.Value.Date > DateTime.Now.AddYears(-(DateTime.Now.Year - x.Employee.DateOfBirth.Value.Year)).Date ? 1 : 0)
                                : (int?)null,
                        ServiceYear = x.CalculatedServiceYear, 
                        LatestNetPay = _context.PAYROLLS
                                               .Where(p => p.EmpeId == x.Employee.EmpeId)
                                               .OrderByDescending(p => p.PayDate)
                                               .Select(p => (decimal?)p.NetPay) 
                                               .FirstOrDefault() 
                    })
                    .OrderBy(x => x.EmpeName) 
                    .ToListAsync();

                return View(adjustmentList); 
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading employee adjustment data.";
                
                return View(new List<EmployeeAdjustmentViewModel>());
            }
        }
    }
}