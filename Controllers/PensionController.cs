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

[Authorize]
public class PensionController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<PensionController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PensionController(AppDbContext context, ILogger<PensionController> logger, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }



    [HttpGet]
    public IActionResult PensionRequest()
    {
        try
        {
            var employee = GetCurrentUser();
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found. Please log in again.";
                return RedirectToAction("Index", "Home");
            }

            var adminApprover = GetAdminApprover();
            var (age, serviceYears) = CalculateEmployeeYears(employee);

            var model = new PensionRequest
            {
                EmpeId = employee.EmpeId,
                EmpeName = employee.EmpeName,
                Department = employee.PostalCode,
                Position = employee.Status,
                Age = age,
                ServiceYears = serviceYears,
                ApprovedById = adminApprover?.EmpeId,
                RequestDate = DateTime.Now,
                Status = "Pending",
                ApproverName = adminApprover?.EmpeName
            };

            ViewBag.ApproverName = adminApprover?.EmpeName ?? "No admin assigned";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pension request form for user {User}", HttpContext.User.Identity?.Name);
            TempData["ErrorMessage"] = "Error loading pension request form. Please try again or contact support.";
            return RedirectToAction("Index", "Home");
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PensionRequest(PensionRequest model, IFormFile file)
    {
        try
        {
            var employee = GetCurrentUser();
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found. Please log in again.";
                return RedirectToAction("Index", "Home");
            }

            var (age, serviceYears) = CalculateEmployeeYears(employee);
            model.Age = age;
            model.ServiceYears = serviceYears;
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
            }

            var adminApprover = GetAdminApprover();
            model.EmpeName = employee.EmpeName;
            model.Department = employee.PostalCode;
            model.Position = employee.Status;
            model.ApprovedById = adminApprover?.EmpeId;
            model.ApproverName = adminApprover?.EmpeName;
            model.RequestDate = DateTime.UtcNow;
            model.Status = "Pending";
            if (file != null && file.Length > 0)
            {
                var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "pension-documents");
                Directory.CreateDirectory(uploadsDir); 
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                model.AttachFileName = file.FileName;
                model.AttachFileType = file.ContentType;
                model.AttachFileSize = file.Length;
                model.AttachFilePath = $"/pension-documents/{uniqueFileName}";
                model.AttachFileUploadDate = DateTime.UtcNow;
            }

            _context.PENSION.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pension request submitted successfully!";
            return RedirectToAction(nameof(PensionRequest));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting pension request for employee {EmployeeId}", model?.EmpeId);
            TempData["ErrorMessage"] = "An error occurred while submitting your request. Please try again.";
            return View(model);
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
                CurrentUserName = currentUser.EmpeName
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
            var request = await _context.PENSION.FindAsync(id);

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

            request.Status = "Approved";
            request.ApprovalDate = DateTime.Now;

            _context.PENSION.Update(request);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Pension request {request.RequestId} successfully Approved.";
            return RedirectToAction(nameof(PensionList));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error approving pension request {RequestId}", id);
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
            var request = await _context.PENSION.FindAsync(id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Pension request not found.";
                return RedirectToAction(nameof(PensionList));
            }
            if (request.Status != "Pending")
            {
                TempData["WarningMessage"] = "Only pending requests can be rejected.";
                return RedirectToAction(nameof(PensionList));
            }

            request.Status = "Rejected";
            request.ApprovalDate = DateTime.Now;

            _context.PENSION.Update(request);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Pension request {request.RequestId} successfully Rejected.";
            return RedirectToAction(nameof(PensionList));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error rejecting pension request {RequestId}", id);
            TempData["ErrorMessage"] = $"Failed to reject pension request {id}: {ex.Message}";
            return RedirectToAction(nameof(PensionList));
        }
    }


}