using HR_Products.Data;
using HR_Products.Models.Entitites;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text; // Required for StringBuilder (if any other parts use it)
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using System.Collections.Generic;
using System.IO;
using Xceed.Words.NET;
using System.Drawing;
namespace HR_Products.Controllers
{
    public class PayrollController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment; 

        public PayrollController(AppDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment; 
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var payroll = await _context.PAYROLLS
                    .Include(p => p.Employee)
                    .ToListAsync();

                return View(payroll);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving payroll data.");
            }
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
                }

            return employee;
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = GetCurrentUser();

            var isInRole = User.IsInRole("Admin") || User.IsInRole("HR-Admin");
            var isFinanceAdmin = currentUser?.JobTitle == "Finance-Admin";

            var isAdmin = isInRole || isFinanceAdmin;

            var viewModel = new PayrollCreateViewModel
            {
                IsAdmin = isAdmin,
                Employees = await _context.EMPE_PROFILE
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmpeId.ToString(),
                        Text = $"{e.EmpeName}"
                    })
                    .ToListAsync()
            };

            if (!isAdmin && currentUser != null)
            {
                viewModel.EmpeId = currentUser.EmpeId;
                viewModel.EmpeName = currentUser.EmpeName;
                if (currentUser != null)
                {
                    viewModel.Department = currentUser.PostalCode;
                    viewModel.Position = currentUser.Status;
                }
            }

            return View(viewModel);
        }
        private async Task<decimal> GetApprovedLeaveDurationForMonth(int employeeId, string leaveTypeName, int year, int month, bool isHalfDayAdjusted = true)
        {
            DateTime firstDayOfMonth = new DateTime(year, month, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var leaves = await _context.LEAV_REQUESTS
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.EmployeeId == employeeId &&
                             lr.LeaveType.LEAV_TYPE_NAME == leaveTypeName &&
                             lr.Status == "Approved" &&
                             (lr.StartDate <= lastDayOfMonth && lr.EndDate >= firstDayOfMonth))
                .ToListAsync();

            decimal totalDuration = 0m;
            foreach (var leave in leaves)
            {
                DateTime overlapStart = leave.StartDate > firstDayOfMonth ? leave.StartDate : firstDayOfMonth;
                DateTime overlapEnd = leave.EndDate < lastDayOfMonth ? leave.EndDate : lastDayOfMonth;
                decimal actualLeaveDuration = (decimal)(overlapEnd - overlapStart).Days + 1;

                if (isHalfDayAdjusted && leave.DurationType == "Half-Day")
                {
                    actualLeaveDuration /= 2m;
                }
                totalDuration += actualLeaveDuration;
            }
            return totalDuration;
        }


        private async Task<decimal> CalculateDeductionsAndNetPay(
           int employeeId,
           decimal basicSalary,
           decimal allowance,
           decimal tax,
           decimal overtimeHours,
           decimal deductions, 
           DateTime frDate,
           DateTime toDate)
        {
            decimal totalDeductionForLeaves = 0m;

            int payrollPeriodDays = (toDate - frDate).Days + 1;
            if (payrollPeriodDays <= 0) payrollPeriodDays = 1;
            decimal dailyBasicSalary = basicSalary / payrollPeriodDays;

            var relevantUplMlLeaves = await _context.LEAV_REQUESTS
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.EmployeeId == employeeId &&
                             lr.Status == "Approved" &&
                             (lr.LeaveType.LEAV_TYPE_NAME == "UPL" || lr.LeaveType.LEAV_TYPE_NAME == "ML") &&
                             (lr.StartDate <= toDate && lr.EndDate >= frDate)) 
                .ToListAsync();

            decimal totalUPLDuration = 0m;
            decimal totalMLDuration = 0m;

            foreach (var leave in relevantUplMlLeaves)
            {
                DateTime overlapStart = leave.StartDate > frDate ? leave.StartDate : frDate;
                DateTime overlapEnd = leave.EndDate < toDate ? leave.EndDate : toDate;

                decimal actualLeaveDurationInPeriod = (decimal)(overlapEnd - overlapStart).Days + 1;

                if (leave.DurationType == "Half-Day")
                {
                    actualLeaveDurationInPeriod /= 2m;
                }

                if (leave.LeaveType.LEAV_TYPE_NAME == "UPL")
                {
                    totalUPLDuration += actualLeaveDurationInPeriod;
                }
                else if (leave.LeaveType.LEAV_TYPE_NAME == "ML")
                {
                    totalMLDuration += actualLeaveDurationInPeriod;
                }
            }

            decimal uplDeduction = dailyBasicSalary * totalUPLDuration;
            decimal mlDeduction = dailyBasicSalary * 0.5m * totalMLDuration;

            totalDeductionForLeaves += uplDeduction + mlDeduction;
           
            decimal serlDeductionAmount = 0m;
            decimal sumOfPrevious11MonthsBasicSalaries = 0m;
            decimal sumOfMonthlyValues = 0m;

            for (int i = 1; i <= 11; i++)
            {
                DateTime monthBeforeFrDate = frDate.AddMonths(-i);
                DateTime historicalMonthStart = new DateTime(monthBeforeFrDate.Year, monthBeforeFrDate.Month, 1);
                DateTime historicalMonthEnd = historicalMonthStart.AddMonths(1).AddDays(-1);
                var historicalPayroll = await _context.PAYROLLS
                    .Where(p => p.EmpeId == employeeId &&
                                p.PayDate >= historicalMonthStart &&
                                p.PayDate <= historicalMonthEnd)
                    .OrderByDescending(p => p.PayDate) 
                    .Select(p => (decimal?)p.BasicSalary)
                    .FirstOrDefaultAsync();

                decimal historicalBasicSalary = historicalPayroll ?? 0m;
                sumOfPrevious11MonthsBasicSalaries += historicalBasicSalary;

                decimal serlDurationInMonth = await GetApprovedLeaveDurationForMonth(employeeId, "SER-L", historicalMonthStart.Year, historicalMonthStart.Month);
                int totalCalendarDaysInMonth = DateTime.DaysInMonth(historicalMonthStart.Year, historicalMonthStart.Month);

                decimal monthlyValue = 1m;
                if (totalCalendarDaysInMonth > 0)
                {
                    monthlyValue = (totalCalendarDaysInMonth - serlDurationInMonth) / totalCalendarDaysInMonth;
                    if (monthlyValue < 0) monthlyValue = 0; 
                }

                sumOfMonthlyValues += monthlyValue;
            }

            if (sumOfMonthlyValues > 0)
            {
                serlDeductionAmount = sumOfPrevious11MonthsBasicSalaries / sumOfMonthlyValues;
            }

            totalDeductionForLeaves += serlDeductionAmount; 
            decimal calculatedNetPay = basicSalary + allowance - tax - totalDeductionForLeaves - deductions;

            return calculatedNetPay;
        }

        [HttpGet]
        public async Task<IActionResult> GetCalculatedNetPay(
             int employeeId,
             decimal basicSalary,
             decimal allowance,
             decimal tax,
             decimal overtimeHours,
             decimal deductions,
             DateTime frDate,
             DateTime toDate)
        {
            if (employeeId <= 0 || frDate == default(DateTime) || toDate == default(DateTime))
            {
                return BadRequest(new { error = "Invalid input for payroll calculation." });
            }

            try
            {
                decimal netPay = await CalculateDeductionsAndNetPay(
                    employeeId, basicSalary, allowance, tax, overtimeHours, deductions, frDate, toDate);

                return Json(new { netPay = netPay });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal server error during calculation: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PayrollCreateViewModel model)
        {
            var currentUser = GetCurrentUser();
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("HR-Admin") || (currentUser?.JobTitle == "Finance-Admin");

            if (!isAdmin && currentUser != null)
            {
                model.EmpeId = currentUser.EmpeId;
                model.EmpeName = currentUser.EmpeName;
            }
            else if (!isAdmin && currentUser == null)
            {
                ModelState.AddModelError("", "Your employee profile could not be found. Please log in again.");
                model.Employees = await _context.EMPE_PROFILE
                   .Select(e => new SelectListItem { Value = e.EmpeId.ToString(), Text = $"{e.EmpeName}" })
                   .ToListAsync();
                model.IsAdmin = isAdmin;
                return View(model);
            }

            EmployeeProfile selectedEmployee = null;
            if (model.EmpeId > 0)
            {
                selectedEmployee = await _context.EMPE_PROFILE.FindAsync(model.EmpeId);
            }

            if (selectedEmployee == null)
            {
                ModelState.AddModelError("EmpeId", "Selected employee not found.");
                model.Employees = await _context.EMPE_PROFILE
                    .Select(e => new SelectListItem { Value = e.EmpeId.ToString(), Text = $"{e.EmpeName}" })
                    .ToListAsync();
                model.IsAdmin = isAdmin;
                return View(model);
            }

            model.EmpeName = selectedEmployee.EmpeName;
            model.Department = selectedEmployee.PostalCode;
            model.Position = selectedEmployee.Status;

            
            model.NetPay = await CalculateDeductionsAndNetPay(
                model.EmpeId,
                model.BasicSalary,
                model.Allowance,
                model.Tax,
                model.OvertimeHours,
                model.Deductions,
                model.FrDate,
                model.ToDate
            );

            var payroll = new Payroll
            {
                EmpeId = model.EmpeId,
                EmpeName = model.EmpeName,
                Department = model.Department,
                Position = model.Position,
                BasicSalary = model.BasicSalary,
                Allowance = model.Allowance,
                OvertimeHours = model.OvertimeHours,
                Tax = model.Tax,
                Deductions = model.Deductions,
                GrossPay = model.GrossPay,
                NetPay = model.NetPay, 
                FrDate = model.FrDate,
                ToDate = model.ToDate,
                PayDate = model.PayDate
            };

            try
            {
                _context.PAYROLLS.Add(payroll);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payroll record created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving payroll record: {ex.Message}");
                model.Employees = await _context.EMPE_PROFILE
                    .Select(e => new SelectListItem { Value = e.EmpeId.ToString(), Text = $"{e.EmpeName}" })
                    .ToListAsync();
                model.IsAdmin = isAdmin;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeDetails(int employeeId)
        {
            if (employeeId <= 0)
            {
                return NotFound(new { error = "Invalid Employee ID provided." });
            }

            var employeeProfile = await _context.EMPE_PROFILE
                                               .FirstOrDefaultAsync(e => e.EmpeId == employeeId);

            if (employeeProfile == null)
            {
                return NotFound(new { error = $"Employee with ID {employeeId} not found." });
            }

            DateTime today = DateTime.Today;
            DateTime firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            DateTime lastDayOfPreviousMonth = firstDayOfCurrentMonth.AddDays(-1);
            DateTime firstDayOfPreviousMonth = new DateTime(lastDayOfPreviousMonth.Year, lastDayOfPreviousMonth.Month, 1);

            var lastMonthPayroll = await _context.PAYROLLS
                                                .Where(p => p.EmpeId == employeeId &&
                                                            p.PayDate >= firstDayOfPreviousMonth &&
                                                            p.PayDate <= lastDayOfPreviousMonth)
                                                .OrderByDescending(p => p.PayDate) 
                                                .FirstOrDefaultAsync();

            decimal lastMonthBasicSalary = lastMonthPayroll?.BasicSalary ?? 0m; 

            return Json(new
            {
                department = employeeProfile.PostalCode, 
                position = employeeProfile.Status,      
                basicSalary = lastMonthBasicSalary      
            });
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Payroll ID not provided.";
                return RedirectToAction(nameof(Index));
            }

            var payroll = await _context.PAYROLLS
                                        .Include(p => p.Employee) 
                                        .FirstOrDefaultAsync(m => m.PayrollId == id);

            if (payroll == null)
            {
                TempData["ErrorMessage"] = "Payroll record not found.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = GetCurrentUser();
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("HR-Admin") || (currentUser?.JobTitle == "Finance-Admin");

            if (!isAdmin && (currentUser == null || payroll.EmpeId != currentUser.EmpeId))
            {
                TempData["ErrorMessage"] = "You are not authorized to view this payroll record.";
                return RedirectToAction(nameof(Index)); 
            }

            return View(payroll); 
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Payroll ID not provided.";
                return RedirectToAction(nameof(Index));
            }

            var payroll = await _context.PAYROLLS
                                        .Include(p => p.Employee)
                                        .FirstOrDefaultAsync(m => m.PayrollId == id);

            if (payroll == null)
            {
                TempData["ErrorMessage"] = "Payroll record not found.";
                return RedirectToAction(nameof(Index));
            }

           
            var viewModel = new PayrollCreateViewModel
            {
                PayrollId = payroll.PayrollId,
                EmpeId = payroll.EmpeId,
                EmpeName = payroll.EmpeName, 
                Department = payroll.Department,
                Position = payroll.Position,
                BasicSalary = payroll.BasicSalary,
                Allowance = payroll.Allowance,
                OvertimeHours = payroll.OvertimeHours,
                Tax = payroll.Tax,
                Deductions = payroll.Deductions,
                GrossPay = payroll.GrossPay,
                NetPay = payroll.NetPay,
                FrDate = payroll.FrDate,
                ToDate = payroll.ToDate,
                PayDate = payroll.PayDate,
                Employees = await _context.EMPE_PROFILE 
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmpeId.ToString(),
                        Text = $"{e.EmpeName}",
                        Selected = e.EmpeId == payroll.EmpeId 
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PayrollCreateViewModel model)
        {
            if (id != model.PayrollId)
            {
                TempData["ErrorMessage"] = "Mismatched Payroll ID.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = GetCurrentUser();
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("HR-Admin") || (currentUser?.JobTitle == "Finance-Admin");

            
            model.Employees = await _context.EMPE_PROFILE
                .Select(e => new SelectListItem
                {
                    Value = e.EmpeId.ToString(),
                    Text = $"{e.EmpeName}", 
                    Selected = e.EmpeId == model.EmpeId
                })
                .ToListAsync();
            model.IsAdmin = isAdmin;

            if (!isAdmin)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit payroll records.";
                return RedirectToAction(nameof(Index));
            }

            EmployeeProfile selectedEmployee = null;
            if (model.EmpeId > 0)
            {
                selectedEmployee = await _context.EMPE_PROFILE.FindAsync(model.EmpeId);
            }

            if (selectedEmployee == null)
            {
                ModelState.AddModelError("EmpeId", "Selected employee not found for update.");
                return View(model);
            }

            model.Department = selectedEmployee.PostalCode;
            model.Position = selectedEmployee.Status;
            model.EmpeName = selectedEmployee.EmpeName; 

            
            model.NetPay = await CalculateDeductionsAndNetPay(
                model.EmpeId,
                model.BasicSalary,
                model.Allowance,
                model.Tax,
                model.OvertimeHours,
                model.Deductions,
                model.FrDate,
                model.ToDate
            );
            

            var payrollToUpdate = await _context.PAYROLLS.FindAsync(id);

            if (payrollToUpdate == null)
            {
                TempData["ErrorMessage"] = "Payroll record not found for update.";
                return RedirectToAction(nameof(Index));
            }

            payrollToUpdate.EmpeId = model.EmpeId;
            payrollToUpdate.EmpeName = model.EmpeName;
            payrollToUpdate.Department = model.Department;
            payrollToUpdate.Position = model.Position;
            payrollToUpdate.BasicSalary = model.BasicSalary;
            payrollToUpdate.Allowance = model.Allowance;
            payrollToUpdate.OvertimeHours = model.OvertimeHours;
            payrollToUpdate.Tax = model.Tax;
            payrollToUpdate.Deductions = model.Deductions;
            payrollToUpdate.GrossPay = model.GrossPay;
            payrollToUpdate.NetPay = model.NetPay; 
            payrollToUpdate.FrDate = model.FrDate;
            payrollToUpdate.ToDate = model.ToDate;
            payrollToUpdate.PayDate = model.PayDate;

            try
            {
                _context.Update(payrollToUpdate);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payroll record updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.PAYROLLS.Any(e => e.PayrollId == id))
                {
                    TempData["ErrorMessage"] = "Payroll record no longer exists.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating payroll record: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.PAYROLLS.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.PAYROLLS.Remove(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Transaction()
        {
            try
            {
                var currentUser = GetCurrentUser(); 
                bool isAdminRoleUser = User.IsInRole("Admin") || User.IsInRole("HR-Admin");
                bool isFinanceAdminByJobTitle = (currentUser?.JobTitle == "Finance-Admin");
                bool canSeeAllRecords = isAdminRoleUser || isFinanceAdminByJobTitle;

                IQueryable<Payroll> payrollQuery = _context.PAYROLLS
                                                         .Include(p => p.Employee); 

                if (!canSeeAllRecords)
                {
                    if (currentUser == null)
                    {
                        TempData["ErrorMessage"] = "Your employee profile could not be found. Please log in again.";
                        return View(new List<Payroll>());
                    }
                    payrollQuery = payrollQuery.Where(p => p.EmpeId == currentUser.EmpeId);
                }
                var transactions = await payrollQuery.OrderByDescending(p => p.PayDate).ToListAsync();

                return View(transactions);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while retrieving transaction data.";
                return View(new List<Payroll>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> TransactionDetail(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Transaction ID not provided.";
                return RedirectToAction(nameof(Transaction)); 
            }

            var transaction = await _context.PAYROLLS
                                            .Include(p => p.Employee)
                                            .FirstOrDefaultAsync(m => m.PayrollId == id);

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "Transaction record not found.";
                return RedirectToAction(nameof(Transaction));
            }

            if (transaction.Employee?.JobTitle == "User")
            {
                TempData["ErrorMessage"] = "You are not authorized to view this transaction record.";
                return RedirectToAction(nameof(Transaction));
            }

            return View(transaction); 
        }

        [HttpGet]
        public async Task<IActionResult> Report()
        {
            var viewModel = new ReportViewModel();
            var currentUser = GetCurrentUser();
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("HR-Admin") || (currentUser?.JobTitle == "Finance-Admin");

            if (!isAdmin)
            {
                viewModel.SelectedEmployeeId = currentUser.EmpeId;
                viewModel.IsEmployeeSelectionDisabled = true;
                viewModel.CurrentUserName = currentUser.EmpeName;
                viewModel.Employees = new List<SelectListItem>
                {
                    new SelectListItem
                    {
                        Value = currentUser.EmpeId.ToString(),
                        Text = currentUser.EmpeName,
                        Selected = true
                    }
                };
            }
            else
            {
                viewModel.Employees = await _context.EMPE_PROFILE
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmpeId.ToString(),
                        Text = e.EmpeName
                    })
                    .OrderBy(item => item.Text)
                    .ToListAsync();
                viewModel.IsEmployeeSelectionDisabled = false;
            }

            int currentYear = DateTime.Now.Year;
            for (int i = 0; i < 6; i++)
            {
                viewModel.Years.Add(new SelectListItem
                {
                    Value = (currentYear - i).ToString(),
                    Text = (currentYear - i).ToString(),
                    Selected = (currentYear - i) == viewModel.SelectedYear
                });
            }
            for (int i = 1; i <= 12; i++)
            {
                viewModel.Months.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = new DateTime(currentYear, i, 1).ToString("MMMM"),
                    Selected = i == viewModel.SelectedMonth
                });
            }
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePayrollReport(ReportViewModel model)
        {
            model.Employees = await _context.EMPE_PROFILE
                .Select(e => new SelectListItem { Value = e.EmpeId.ToString(), Text = e.EmpeName })
                .OrderBy(item => item.Text)
                .ToListAsync();
            int currentYear = DateTime.Now.Year;
            for (int i = 0; i < 6; i++) { model.Years.Add(new SelectListItem { Value = (currentYear - i).ToString(), Text = (currentYear - i).ToString() }); }
            for (int i = 1; i <= 12; i++) { model.Months.Add(new SelectListItem { Value = i.ToString(), Text = new DateTime(currentYear, i, 1).ToString("MMMM") }); }

            var employee = await _context.EMPE_PROFILE
                                        .FirstOrDefaultAsync(e => e.EmpeId == model.SelectedEmployeeId);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Selected employee not found.";
                return View("Report", model);
            }

            DateTime firstDayOfMonth = new DateTime(model.SelectedYear, model.SelectedMonth, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var payrollRecord = await _context.PAYROLLS
                                            .Where(p => p.EmpeId == model.SelectedEmployeeId &&
                                                        p.PayDate >= firstDayOfMonth &&
                                                        p.PayDate <= lastDayOfMonth)
                                            .OrderByDescending(p => p.PayDate)
                                            .FirstOrDefaultAsync();

            if (payrollRecord == null)
            {
                TempData["ErrorMessage"] = $"No payroll record found for {employee.EmpeName} for {firstDayOfMonth.ToString("MMMM")} {model.SelectedYear}.";
                return View("Report", model);
            }

            int serviceYear = DateTime.Now.Year - employee.JoinDate.Year;
            if (employee.JoinDate.Date > DateTime.Now.AddYears(-serviceYear).Date)
            {
                serviceYear--;
            }

            int? age = null;
            if (employee.DateOfBirth.HasValue)
            {
                age = DateTime.Now.Year - employee.DateOfBirth.Value.Year;
                if (employee.DateOfBirth.Value.Date > DateTime.Now.AddYears(-age.Value).Date)
                {
                    age--;
                }
            }

            decimal totalUPLDays = 0m;
            decimal uplDeduction = 0m;
            decimal totalMLDays = 0m;
            decimal mlDeduction = 0m;

            int payrollPeriodDays = (payrollRecord.ToDate - payrollRecord.FrDate).Days + 1;
            if (payrollPeriodDays <= 0) payrollPeriodDays = 1;
            decimal dailyBasicSalary = payrollRecord.BasicSalary / payrollPeriodDays;

            var allRelevantApprovedLeaves = await _context.LEAV_REQUESTS
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.EmployeeId == model.SelectedEmployeeId &&
                             lr.Status == "Approved" &&
                             (lr.StartDate <= payrollRecord.ToDate && lr.EndDate >= payrollRecord.FrDate))
                .OrderBy(lr => lr.StartDate) 
                .ToListAsync();

            foreach (var leave in allRelevantApprovedLeaves.Where(leave => leave.LeaveType.LEAV_TYPE_NAME == "UPL" || leave.LeaveType.LEAV_TYPE_NAME == "ML"))
            {
                DateTime overlapStart = leave.StartDate > payrollRecord.FrDate ? leave.StartDate : payrollRecord.FrDate;
                DateTime overlapEnd = leave.EndDate < payrollRecord.ToDate ? leave.EndDate : payrollRecord.ToDate;
                decimal actualLeaveDurationInPeriod = (decimal)(overlapEnd - overlapStart).Days + 1;

                if (leave.DurationType == "Half-Day") actualLeaveDurationInPeriod /= 2m;

                if (leave.LeaveType.LEAV_TYPE_NAME == "UPL") totalUPLDays += actualLeaveDurationInPeriod;
                else if (leave.LeaveType.LEAV_TYPE_NAME == "ML") totalMLDays += actualLeaveDurationInPeriod;
            }
            uplDeduction = dailyBasicSalary * totalUPLDays;
            mlDeduction = dailyBasicSalary * 0.5m * totalMLDays;


            decimal serlDeductionAmount = 0m;
            decimal sumOfPrevious11MonthsBasicSalaries = 0m;
            decimal sumOfMonthlyValues = 0m;
            decimal totalSERLDaysPast11Months = 0m;

            for (int i = 1; i <= 11; i++)
            {
                DateTime monthBeforeFrDate = payrollRecord.FrDate.AddMonths(-i);
                DateTime historicalMonthStart = new DateTime(monthBeforeFrDate.Year, monthBeforeFrDate.Month, 1);
                DateTime historicalMonthEnd = historicalMonthStart.AddMonths(1).AddDays(-1);

                var historicalPayroll = await _context.PAYROLLS
                    .Where(p => p.EmpeId == model.SelectedEmployeeId &&
                                p.PayDate >= historicalMonthStart &&
                                p.PayDate <= historicalMonthEnd)
                    .OrderByDescending(p => p.PayDate)
                    .Select(p => (decimal?)p.BasicSalary)
                    .FirstOrDefaultAsync();

                decimal historicalBasicSalary = historicalPayroll ?? 0m;
                sumOfPrevious11MonthsBasicSalaries += historicalBasicSalary;

                decimal serlDurationInMonth = await GetApprovedLeaveDurationForMonth(model.SelectedEmployeeId, "SER-L", historicalMonthStart.Year, historicalMonthStart.Month);
                totalSERLDaysPast11Months += serlDurationInMonth;

                int totalCalendarDaysInMonth = DateTime.DaysInMonth(historicalMonthStart.Year, historicalMonthStart.Month);

                decimal monthlyValue = 1m;
                if (totalCalendarDaysInMonth > 0)
                {
                    monthlyValue = (totalCalendarDaysInMonth - serlDurationInMonth) / totalCalendarDaysInMonth;
                    if (monthlyValue < 0) monthlyValue = 0;
                }
                sumOfMonthlyValues += monthlyValue;
            }

            if (sumOfMonthlyValues > 0)
            {
                serlDeductionAmount = sumOfPrevious11MonthsBasicSalaries / sumOfMonthlyValues;
            }


            StringBuilder employeeDetailsBuilder = new StringBuilder();
            employeeDetailsBuilder.AppendLine($"Employee Name => {employee.EmpeName}");
            employeeDetailsBuilder.AppendLine($"Email         => {employee.Email}");
            employeeDetailsBuilder.AppendLine($"Department    => {employee.PostalCode}");
            employeeDetailsBuilder.AppendLine($"Position      => {employee.Status}");
            employeeDetailsBuilder.AppendLine($"Date of Birth => {employee.DateOfBirth?.ToString("MM/dd/yyyy") ?? "N/A"}");
            employeeDetailsBuilder.AppendLine($"Age           => {age?.ToString() ?? "N/A"}");
            employeeDetailsBuilder.AppendLine($"Join Date     => {employee.JoinDate.ToString("MM/dd/yyyy")}");
            employeeDetailsBuilder.AppendLine($"Service Year  => {serviceYear.ToString()}");
            string employeeDetailsList = employeeDetailsBuilder.ToString();


            StringBuilder payrollSummaryBuilder = new StringBuilder();
            payrollSummaryBuilder.AppendLine($"Payroll Period    => {payrollRecord.FrDate.ToString("MM/dd/yyyy")} - {payrollRecord.ToDate.ToString("MM/dd/yyyy")}");
            payrollSummaryBuilder.AppendLine($"Pay Date          => {payrollRecord.PayDate.ToString("MM/dd/yyyy")}");
            payrollSummaryBuilder.AppendLine($"Basic Salary      => {payrollRecord.BasicSalary.ToString("0.00")}");
            payrollSummaryBuilder.AppendLine($"Allowance         => {payrollRecord.Allowance.ToString("0.00")}");
            payrollSummaryBuilder.AppendLine($"Income Tax        => {payrollRecord.Tax.ToString("0.00")}");
            payrollSummaryBuilder.AppendLine($"Other Deductions  => {payrollRecord.Deductions.ToString("0.00")}");
            payrollSummaryBuilder.AppendLine($"Gross Pay         => {payrollRecord.GrossPay.ToString("0.00")}");
            payrollSummaryBuilder.AppendLine($"Net Amount        => {payrollRecord.NetPay.ToString("0.00")}");
            string payrollSummaryList = payrollSummaryBuilder.ToString();

            StringBuilder detailedLeaveRecordsBuilder = new StringBuilder();
            if (allRelevantApprovedLeaves.Any())
            {
                foreach (var leave in allRelevantApprovedLeaves)
                {
                    decimal individualDeduction = 0m;
                    string deductionString = "N/A";

                    DateTime overlapStartForDisplay = leave.StartDate > payrollRecord.FrDate ? leave.StartDate : payrollRecord.FrDate;
                    DateTime overlapEndForDisplay = leave.EndDate < payrollRecord.ToDate ? leave.EndDate : payrollRecord.ToDate;
                    decimal actualDurationForDisplay = (decimal)(overlapEndForDisplay - overlapStartForDisplay).Days + 1;
                    if (leave.DurationType == "Half-Day") actualDurationForDisplay /= 2m;


                    if (leave.LeaveType.LEAV_TYPE_NAME == "UPL")
                    {
                        individualDeduction = dailyBasicSalary * actualDurationForDisplay;
                        deductionString = individualDeduction.ToString("0.00");
                    }
                    else if (leave.LeaveType.LEAV_TYPE_NAME == "ML")
                    {
                        individualDeduction = dailyBasicSalary * 0.5m * actualDurationForDisplay;
                        deductionString = individualDeduction.ToString("0.00");
                    }

                   
                    detailedLeaveRecordsBuilder.AppendLine($"Name           => {employee.EmpeName}"); 
                    detailedLeaveRecordsBuilder.AppendLine($"Leave Type     => {leave.LeaveType.LEAV_TYPE_NAME}");
                    detailedLeaveRecordsBuilder.AppendLine($"Start Date     => {leave.StartDate.ToString("MM/dd/yyyy")}");
                    detailedLeaveRecordsBuilder.AppendLine($"End Date       => {leave.EndDate.ToString("MM/dd/yyyy")}");
                    detailedLeaveRecordsBuilder.AppendLine($"Duration       => {actualDurationForDisplay.ToString("0.0")}");
                    detailedLeaveRecordsBuilder.AppendLine($"Deduction Amt  => {deductionString}");
                    detailedLeaveRecordsBuilder.AppendLine($"Used Date      => {leave.UsedToDate.ToString("0.00")}");
                    detailedLeaveRecordsBuilder.AppendLine($"Accrual Balance=> {leave.AccrualBalance.ToString("0.00")}");
                    detailedLeaveRecordsBuilder.AppendLine(); 
                }
            }
            else
            {
                detailedLeaveRecordsBuilder.AppendLine("No approved leaves found for this payroll period.");
            }
            string detailedLeaveRecordsList = detailedLeaveRecordsBuilder.ToString();

            string templatePath = Path.Combine(_hostingEnvironment.WebRootPath, "templates", "EmployeeReportTemplate.docx");
            if (!System.IO.File.Exists(templatePath))
            {
                TempData["ErrorMessage"] = "Report template not found on server. Please ensure 'EmployeeReportTemplate.docx' is in wwwroot/templates.";
                return View("Report", model);
            }

            try
            {
                using (Xceed.Words.NET.DocX doc = Xceed.Words.NET.DocX.Load(templatePath))
                {
                    doc.ReplaceText("{{ReportDate}}", DateTime.Now.ToString("MM/dd/yyyy"));

                    doc.ReplaceText("{{EmployeeDetailsList}}", employeeDetailsList);
                    doc.ReplaceText("{{PayrollSummaryList}}", payrollSummaryList);
                    doc.ReplaceText("{{DetailedLeaveRecordsList}}", detailedLeaveRecordsList);

                    doc.ReplaceText("{{TotalUPLDays}}", totalUPLDays.ToString("0.00"));
                    doc.ReplaceText("{{UPLDeduction}}", uplDeduction.ToString("0.00"));
                    doc.ReplaceText("{{TotalMLDays}}", totalMLDays.ToString("0.00"));
                    doc.ReplaceText("{{MLDeduction}}", mlDeduction.ToString("0.00"));
                    doc.ReplaceText("{{TotalSERLDaysPast11Months}}", totalSERLDaysPast11Months.ToString("0.00"));
                    doc.ReplaceText("{{SERLDeduction}}", serlDeductionAmount.ToString("0.00"));

                    using (MemoryStream ms = new MemoryStream())
                    {
                        doc.SaveAs(ms);
                        ms.Position = 0;

                        string fileName = $"Leave&&Payroll_Report_{employee.EmpeName.Replace(" ", "_")}_{model.SelectedYear}_{model.SelectedMonth}.docx";
                        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
                 return View("Report", model);
            }
        }
    }
}