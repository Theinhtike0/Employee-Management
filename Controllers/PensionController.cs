using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using HR_Products.Models.Entitites;
using HR_Products.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Azure;
using System.ComponentModel.DataAnnotations;
using Xceed.Workbooks.NET;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Mvc.Rendering;
using HR_Products.ViewModels;
using HR_Products.Services;

[Authorize]
public class PensionController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IEmailService _emailService; 
    private readonly ILogger<PensionController> _logger;

    public PensionController(AppDbContext context, IWebHostEnvironment webHostEnvironment, IEmailService emailService, ILogger<PensionController> logger)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _emailService = emailService; 
        _logger = logger;             
    }



    [HttpGet]
    public async Task<IActionResult> PensionRequest(int? empeId = null)
    {
        try
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Employee not found. Please log in again.";
                return RedirectToAction("Index", "Home");
            }

            bool isAdmin = currentUser.JobTitle == "Admin" || currentUser.JobTitle == "HR-Admin";
            ViewBag.IsAdmin = isAdmin;

            EmployeeProfile selectedEmployee = currentUser;
            var adminApprover = GetAdminApprover();

            if (isAdmin && empeId.HasValue)
            {
                selectedEmployee = await _context.EMPE_PROFILE.FirstOrDefaultAsync(e => e.EmpeId == empeId);
                if (selectedEmployee == null)
                {
                    TempData["ErrorMessage"] = "Selected employee not found.";
                    return RedirectToAction("Index", "Home");
                }
            }

            if (isAdmin)
            {
                ViewBag.Employees = await _context.EMPE_PROFILE
                    .OrderBy(e => e.EmpeName)
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmpeId.ToString(),
                        Text = e.EmpeName
                    })
                    .ToListAsync();
            }

            var (age, serviceYears) = CalculateEmployeeYears(selectedEmployee);

            var model = new PensionRequest
            {
                EmpeId = selectedEmployee.EmpeId,
                EmpeName = selectedEmployee.EmpeName,
                Department = selectedEmployee.PostalCode,
                Position = selectedEmployee.Status,
                Age = age,
                ServiceYears = serviceYears,
                ApprovedById = adminApprover?.EmpeId,
                RequestDate = DateTime.Now,
                Status = "Pending",
                ApproverName = adminApprover?.EmpeName
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pension request form for user {User}", HttpContext.User.Identity?.Name);
            TempData["ErrorMessage"] = "Error loading pension request form. Please try again or contact support.";
            return RedirectToAction("Index", "Home");
        }
    }


    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> PensionRequest(PensionRequest model, IFormFile file)
    //{
    //    try
    //    {
    //        var employee = GetCurrentUser();
    //        if (employee == null)
    //        {
    //            TempData["ErrorMessage"] = "Employee not found. Please log in again.";
    //            return RedirectToAction("Index", "Home");
    //        }

    //        var (age, serviceYears) = CalculateEmployeeYears(employee);
    //        model.Age = age;
    //        model.ServiceYears = serviceYears;
    //        if (model.Reason != PensionReason.Medical_Pension)
    //        {
    //            switch (model.Reason)
    //            {
    //                case PensionReason.Age_Pension_62_Years when age < 62:
    //                    TempData["ErrorMessage"] = "You don't meet the age requirement for this pension type.";
    //                    return View(model);
    //                case PensionReason.Service_And_Age_Pension_75 when (age + serviceYears) < 75:
    //                    TempData["ErrorMessage"] = "Your combined age and service years don't meet requirements.";
    //                    return View(model);
    //                case PensionReason.Service_Length_Pension_30_Years when serviceYears < 30:
    //                    TempData["ErrorMessage"] = "You don't have enough service years for this pension type.";
    //                    return View(model);
    //            }
    //        }

    //        if (model.Reason == PensionReason.Medical_Pension && (file == null || file.Length == 0))
    //        {
    //            ModelState.AddModelError("file", "Medical Pension requires supporting documentation");
    //            return View(model);
    //        }

    //        if (file != null && file.Length > 0)
    //        {
    //            if (file.Length > 5 * 1024 * 1024)
    //            {
    //                ModelState.AddModelError("file", "File size cannot exceed 5MB");
    //                return View(model);
    //            }

    //            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
    //            var fileExtension = Path.GetExtension(file.FileName).ToLower();
    //            if (!allowedExtensions.Contains(fileExtension))
    //            {
    //                ModelState.AddModelError("file", "Only PDF, Word, JPG, and PNG files are allowed");
    //                return View(model);
    //            }
    //        }

    //        var adminApprover = GetAdminApprover();
    //        model.EmpeName = employee.EmpeName;
    //        model.Department = employee.PostalCode;
    //        model.Position = employee.Status;
    //        model.ApprovedById = adminApprover?.EmpeId;
    //        model.ApproverName = adminApprover?.EmpeName;
    //        model.RequestDate = DateTime.UtcNow;
    //        model.Status = "Pending";
    //        if (file != null && file.Length > 0)
    //        {
    //            var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "pension-documents");
    //            Directory.CreateDirectory(uploadsDir); 
    //            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    //            var filePath = Path.Combine(uploadsDir, uniqueFileName);

    //            using (var fileStream = new FileStream(filePath, FileMode.Create))
    //            {
    //                await file.CopyToAsync(fileStream);
    //            }

    //            model.AttachFileName = file.FileName;
    //            model.AttachFileType = file.ContentType;
    //            model.AttachFileSize = file.Length;
    //            model.AttachFilePath = $"/pension-documents/{uniqueFileName}";
    //            model.AttachFileUploadDate = DateTime.UtcNow;
    //        }

    //        _context.PENSION.Add(model);
    //        await _context.SaveChangesAsync();

    //        TempData["SuccessMessage"] = "Pension request submitted successfully!";
    //        return RedirectToAction(nameof(PensionRequest));
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error submitting pension request for employee {EmployeeId}", model?.EmpeId);
    //        TempData["ErrorMessage"] = "An error occurred while submitting your request. Please try again.";
    //        return View(model);
    //    }
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PensionRequest(PensionRequest model, IFormFile file)
    {
        // Set IsAdmin flag at the start
        var currentUser = GetCurrentUser();
        ViewBag.IsAdmin = currentUser?.JobTitle == "Admin" || currentUser?.JobTitle == "HR-Admin";

        if (ViewBag.IsAdmin)
        {
            ViewBag.Employees = await _context.EMPE_PROFILE
                .OrderBy(e => e.EmpeName)
                .Select(e => new SelectListItem
                {
                    Value = e.EmpeId.ToString(),
                    Text = e.EmpeName
                })
                .ToListAsync();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var employee = currentUser ?? GetCurrentUser(); // Changed to async call
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found. Please log in again.";
                return RedirectToAction("Index", "Home");
            }

            var (age, serviceYears) = CalculateEmployeeYears(employee);
            model.Age = age;
            model.ServiceYears = serviceYears;

            var lastMonth = DateTime.Now.AddMonths(-1);
            var basicSalary = await _context.PAYROLLS
                .Where(p => p.EmpeId == employee.EmpeId &&
                            p.PayDate.Month == lastMonth.Month &&
                            p.PayDate.Year == lastMonth.Year)
                .OrderByDescending(p => p.PayDate)
                .Select(p => p.BasicSalary)
                .FirstOrDefaultAsync();

            if (basicSalary <= 0)
            {
                basicSalary = await _context.PAYROLLS
                    .Where(p => p.EmpeId == employee.EmpeId)
                    .OrderByDescending(p => p.PayDate)
                    .Select(p => p.BasicSalary)
                    .FirstOrDefaultAsync();
            }

            if (basicSalary <= 0)
            {
                TempData["ErrorMessage"] = "Could not determine your basic salary. Please contact HR.";
                return View(model);
            }

            model.ServiceBonus = serviceYears * basicSalary * 0.5m;
            model.PensionSalary = serviceYears * basicSalary * 0.015m;

            if (model.Reason != PensionReason.Medical_Pension)
            {
                switch (model.Reason)
                {
                    case PensionReason.Age_Pension_62_Years when age < 62:
                        TempData["ErrorMessage"] = "You don't meet the age requirement for this pension type.";
                        return View(model);
                    case PensionReason.Service_And_Age_Pension_75 when (age + serviceYears) < 75:
                        TempData["ErrorMessage"] = "Your combined age and service years don't meet requirements.";
                        return View(model);
                    case PensionReason.Service_Length_Pension_30_Years when serviceYears < 30:
                        TempData["ErrorMessage"] = "You don't have enough service years for this pension type.";
                        return View(model);
                }
            }

            if (model.Reason == PensionReason.Medical_Pension && (file == null || file.Length == 0))
            {
                ModelState.AddModelError("file", "Medical Pension requires supporting documentation");
                return View(model);
            }

            string filePath = null;
            if (file != null && file.Length > 0)
            {
                if (file.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("file", "File size cannot exceed 5MB");
                    return View(model);
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("file", "Only PDF, Word, JPG, and PNG files are allowed");
                    return View(model);
                }

                var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "pension-documents");
                Directory.CreateDirectory(uploadsDir);
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                filePath = Path.Combine(uploadsDir, uniqueFileName);
            }

            var adminApprover = GetAdminApprover();

            model.EmpeName = employee.EmpeName;
            model.Department = employee.PostalCode;
            model.Position = employee.Status;
            model.ApprovedById = adminApprover?.EmpeId;
            model.ApproverName = adminApprover?.EmpeName;
            model.RequestDate = DateTime.UtcNow;
            model.Status = "Pending";

            _context.PENSION.Add(model);

            if (filePath != null)
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                model.AttachFileName = file.FileName;
                model.AttachFileType = file.ContentType;
                model.AttachFileSize = file.Length;
                model.AttachFilePath = $"/pension-documents/{Path.GetFileName(filePath)}";
                model.AttachFileUploadDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (adminApprover != null && !string.IsNullOrEmpty(adminApprover.Email))
            {
                string recipientEmail = adminApprover.Email;
                string subject = $"New Pension Request Submitted by {employee.EmpeName}";
                string message = $"Dear {adminApprover.EmpeName},<br/><br/>" +
                                 $"A new pension request has been submitted by <b>{employee.EmpeName}</b>.<br/><br/>" +
                                 $"<b>Employee ID:</b> {employee.EmpeId}<br/>" +
                                 $"<b>Reason:</b> {model.Reason.ToString().Replace("_", " ")}<br/>" +
                                 $"<b>Request Date:</b> {model.RequestDate.ToString("MM/dd/yyyy")}<br/>" +
                                 $"<b>Service Years:</b> {model.ServiceYears}<br/>" +
                                 $"<b>Age:</b> {model.Age}<br/>" +
                                 $"<b>Calculated Service Bonus:</b> {model.ServiceBonus.ToString("N2")}<br/>" +
                                 $"<b>Calculated Pension Salary:</b> {model.PensionSalary.ToString("N2")}<br/>" +
                                 (string.IsNullOrEmpty(model.AttachFilePath) ? "" : $"<b>Attachment:</b> Available for review in the system.<br/>") +
                                 $"<br/>Please log in to the HR Products system to review this request.<br/><br/>" +
                                 $"Best regards,<br/>" +
                                 $"HR Products System";

                string senderEmailFromProfile = employee.Email;
                string senderNameFromProfile = employee.EmpeName;

                string actualSenderEmail = !string.IsNullOrEmpty(senderEmailFromProfile) ? senderEmailFromProfile : "no-reply@yourcompany.com";
                string actualSenderName = !string.IsNullOrEmpty(senderNameFromProfile) ? senderNameFromProfile : "HR Products System";

                try
                {
                    await _emailService.SendEmailAsync(recipientEmail, subject, message, actualSenderEmail, actualSenderName);
                    TempData["SuccessMessage"] += " Email notification sent to approver!";
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send pension request notification email to {RecipientEmail} from {SenderEmail}", recipientEmail, actualSenderEmail);
                    TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Email notification to approver failed.";
                }
            }
            else
            {
                _logger.LogWarning("Approver email not found or empty for pension request from employee {EmployeeName}. Email notification skipped.", employee.EmpeName);
                TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Approver email not found for notification.";
            }

            TempData["SuccessMessage"] = "Pension request submitted successfully!" + (TempData["SuccessMessage"] as string ?? "");
            return RedirectToAction(nameof(PensionRequest));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error submitting pension request for employee {EmployeeId}", model?.EmpeId);
            TempData["ErrorMessage"] = "An error occurred while submitting your request. Please try again.";
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult GetBasicSalary()
    {
        var employee = GetCurrentUser();
        if (employee == null)
        {
            return Json(new { success = false });
        }

        var lastMonth = DateTime.Now.AddMonths(-1);
        var basicSalary = _context.PAYROLLS
            .Where(p => p.EmpeId == employee.EmpeId &&
                       p.PayDate.Month == lastMonth.Month &&
                       p.PayDate.Year == lastMonth.Year)
            .OrderByDescending(p => p.PayDate)
            .Select(p => p.BasicSalary)
            .FirstOrDefault();

        if (basicSalary <= 0)
        {
            basicSalary = _context.PAYROLLS
                .Where(p => p.EmpeId == employee.EmpeId)
                .OrderByDescending(p => p.PayDate)
                .Select(p => p.BasicSalary)
                .FirstOrDefault();
        }

        return Json(new { success = true, basicSalary });
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

    private EmployeeProfile GetAdminApprover()
    {
        return _context.EMPE_PROFILE
            .AsNoTracking()
            .FirstOrDefault(e => e.JobTitle != null &&
                                e.JobTitle.ToLower() == "admin");
    }

    private (int Age, int ServiceYears) CalculateEmployeeYears(EmployeeProfile employee)
    {
        var today = DateTime.Today;
        int age = 0, serviceYears = 0;

        if (employee.DateOfBirth.HasValue)
        {
            age = today.Year - employee.DateOfBirth.Value.Year;
            if (employee.DateOfBirth.Value.Date > today.AddYears(-age)) age--;
        }

        if (employee.JoinDate != default)
        {
            serviceYears = today.Year - employee.JoinDate.Year;
            if (employee.JoinDate.Date > today.AddYears(-serviceYears)) serviceYears--;
        }

        return (age, serviceYears);
    }

    [HttpGet]
    public async Task<IActionResult> PensionList()
    {
        try
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User session expired or not found. Please re-login.";
                return RedirectToAction("Index", "Home");
            }

            var isAdmin = HttpContext.User.IsInRole("Admin") || HttpContext.User.IsInRole("HR-Admin");

            var query = _context.PENSION.AsNoTracking();

            if (!isAdmin)
            {
                query = query.Where(p => p.EmpeId == currentUser.EmpeId);
            }

            var requests = await query
                .OrderByDescending(p => p.RequestDate)
                .ToListAsync();

            var docPhysicalPathPrefix = Path.Combine(_webHostEnvironment.WebRootPath, "pension-documents");

            if (Directory.Exists(docPhysicalPathPrefix))
            {
                foreach (var request in requests)
                {
                    if (!string.IsNullOrEmpty(request.AttachFilePath))
                    {
                        request.DisplayWebPath = $"/{request.AttachFilePath.Replace("\\", "/")}";
                        var fileExtension = Path.GetExtension(request.AttachFilePath).ToLowerInvariant();
                        request.IsImageFile = new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(fileExtension);
                    }
                }
            }

            return View(new PensionListViewModel
            {
                PensionRequests = requests,
                IsAdmin = isAdmin,
                CurrentUserName = currentUser.EmpeName,
                ShowActionColumn = isAdmin && requests.Any(r => r.Status == "Pending")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pension list");
            TempData["ErrorMessage"] = "Error loading pension requests. Please try again.";
            return RedirectToAction("Index", "Home");
        }
    }
    public IActionResult DownloadPensionDocument(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return NotFound();

        var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath,
                                      "pension-documents",
                                      fileName);

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(physicalPath, contentType, fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePensionRequest(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var request = await _context.PENSION
                                .FirstOrDefaultAsync(p => p.RequestId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Pension request not found.";
                return RedirectToAction(nameof(PensionList));
            }

            if (request.Status != "Pending")
            {
                TempData["WarningMessage"] = "Only pending requests can be approved.";
                return RedirectToAction(nameof(PensionList));
            }
            var employeeProfile = await _context.EMPE_PROFILE
                                        .FirstOrDefaultAsync(e => e.EmpeId == request.EmpeId);

            if (employeeProfile == null)
            {
                TempData["ErrorMessage"] = "Associated employee profile not found for this pension request. Cannot send email notification.";
                await transaction.RollbackAsync();
                return RedirectToAction(nameof(PensionList));
            }


            request.Status = "Approved";
            request.ApprovalDate = DateTime.Now;

            _context.PENSION.Update(request);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Pension request {request.RequestId} successfully Approved.";

            if (!string.IsNullOrEmpty(employeeProfile.Email))
            {
                string recipientEmail = employeeProfile.Email;
                string subject = $"Your Pension Request Has Been Approved - {request.Reason.ToString().Replace("_", " ")}";
                string message = $"Dear {employeeProfile.EmpeName},<br/><br/>" + 
                                 $"We are pleased to inform you that your pension request for <b>{request.Reason.ToString().Replace("_", " ")}</b> has been **APPROVED**.<br/><br/>" +
                                 $"<b>Request ID:</b> {request.RequestId}<br/>" +
                                 $"<b>Approval Date:</b> {request.ApprovalDate?.ToString("MM/dd/yyyy") ?? "N/A"}<br/>" +
                                 $"<b>Service Years:</b> {request.ServiceYears}<br/>" +
                                 $"<b>Age:</b> {request.Age}<br/>" +
                                 $"<b>Service Bonus:</b> {request.ServiceBonus.ToString("C")}<br/>" +
                                 $"<b>Pension Salary:</b> {request.PensionSalary.ToString("C")}<br/>" +
                                 $"Further instructions regarding your pension payout will be communicated by the Finance Department.<br/><br/>" +
                                 $"Best regards,<br/>" +
                                 $"The HR Products Team";

                var approverProfile = GetCurrentUser();
                string senderEmailFromProfile = approverProfile?.Email;
                string senderNameFromProfile = approverProfile?.EmpeName;

                string actualSenderEmail = !string.IsNullOrEmpty(senderEmailFromProfile) ? senderEmailFromProfile : "no-reply@yourcompany.com";
                string actualSenderName = !string.IsNullOrEmpty(senderNameFromProfile) ? senderNameFromProfile : "HR Products System";

                try
                {
                    await _emailService.SendEmailAsync(recipientEmail, subject, message, actualSenderEmail, actualSenderName);
                    TempData["SuccessMessage"] += " Email notification sent to employee!";
                }
                catch (Exception emailEx)
                {
                    TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Email notification to employee failed.";
                }
            }
            else
            {
                TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Employee email not found for approval notification.";
            }

            return RedirectToAction(nameof(PensionList));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Failed to approve pension request {id}: {ex.Message}";
            return RedirectToAction(nameof(PensionList));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPensionRequest(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var request = await _context.PENSION
                                .FirstOrDefaultAsync(p => p.RequestId == id);

            if (request == null)
            {
                _logger.LogWarning("RejectPensionRequest: Pension request with ID {RequestId} not found.", id);
                TempData["ErrorMessage"] = "Pension request not found.";
                return RedirectToAction(nameof(PensionList));
            }
            if (request.Status != "Pending")
            {
                _logger.LogWarning("RejectPensionRequest: Pension request with ID {RequestId} is not pending (current status: {Status}).", id, request.Status);
                TempData["WarningMessage"] = "Only pending requests can be rejected.";
                return RedirectToAction(nameof(PensionList));
            }

            
            var employeeProfile = await _context.EMPE_PROFILE
                                        .FirstOrDefaultAsync(e => e.EmpeId == request.EmpeId);

            if (employeeProfile == null)
            {
                TempData["ErrorMessage"] = "Associated employee profile not found for this pension request. Cannot send email notification.";
                await transaction.RollbackAsync(); 
                return RedirectToAction(nameof(PensionList));
            }


            request.Status = "Rejected";
            request.ApprovalDate = DateTime.Now; 

            _context.PENSION.Update(request);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Pension request {request.RequestId} successfully Rejected.";
            
            if (!string.IsNullOrEmpty(employeeProfile.Email))
            {
                string recipientEmail = employeeProfile.Email;
                string subject = $"Your Pension Request Has Been Rejected - {request.Reason.ToString().Replace("_", " ")}";
                string message = $"Dear {employeeProfile.EmpeName},<br/><br/>" + 
                                 $"We regret to inform you that your pension request for <b>{request.Reason.ToString().Replace("_", " ")}</b> has been **REJECTED**.<br/><br/>" +
                                 $"<b>Request ID:</b> {request.RequestId}<br/>" +
                                 $"<b>Rejection Date:</b> {request.ApprovalDate?.ToString("MM/dd/yyyy") ?? "N/A"}<br/>" + 
                                 $"Please contact the HR Department for further details or clarification.<br/><br/>" +
                                 $"Best regards,<br/>" +
                                 $"The HR Products Team";

                var approverProfile = GetCurrentUser();
                string senderEmailFromProfile = approverProfile?.Email;
                string senderNameFromProfile = approverProfile?.EmpeName;

                string actualSenderEmail = !string.IsNullOrEmpty(senderEmailFromProfile) ? senderEmailFromProfile : "no-reply@yourcompany.com";
                string actualSenderName = !string.IsNullOrEmpty(senderNameFromProfile) ? senderNameFromProfile : "HR Products System";

                try
                {
                    await _emailService.SendEmailAsync(recipientEmail, subject, message, actualSenderEmail, actualSenderName);
                    TempData["SuccessMessage"] += " Email notification sent to employee!";
                }
                catch (Exception emailEx)
                {
                    TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Email notification to employee failed.";
                }
            }
            else
            {
                TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Employee email not found for rejection notification.";
            }

            return RedirectToAction(nameof(PensionList));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Failed to reject pension request {id}: {ex.Message}";
            return RedirectToAction(nameof(PensionList));
        }
    }

    [HttpGet]
    public IActionResult CalculateServiceBonus()
    {
        var employee = GetCurrentUser();
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Employee not found. Please log in again.";
            return RedirectToAction("Index", "Home");
        }

        var (_, serviceYears) = CalculateEmployeeYears(employee);
        var lastMonthSalary = GetLastMonthBasicSalary(employee.EmpeId);

        var model = new PensionBonusViewModel
        {
            EmployeeId = employee.EmpeId,
            EmployeeName = employee.EmpeName,
            ServiceYears = serviceYears,
            LastMonthBasicSalary = lastMonthSalary
        };

        model.ServiceYearBonus = CalculateBonus(serviceYears, lastMonthSalary);

        return View(model);
    }

    private decimal CalculateBonus(int serviceYears, decimal basicSalary)
    {
        if (serviceYears < 5)
        {
            return serviceYears * basicSalary * 1.0m; // 100%
        }
        else
        {
            return serviceYears * basicSalary * 1.5m; // 150%
        }
    }

    private decimal GetLastMonthBasicSalary(int employeeId)
    {
        var lastMonth = DateTime.Now.AddMonths(-1);
        return _context.PAYROLLS
            .Where(p => p.EmpeId == employeeId &&
                       p.PayDate.Month == lastMonth.Month &&
                       p.PayDate.Year == lastMonth.Year)
            .OrderByDescending(p => p.PayDate)
            .Select(p => p.BasicSalary)
            .FirstOrDefault();
    }



}