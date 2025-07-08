using Microsoft.AspNetCore.Identity;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Mvc;
using HR_Products.Models; // Assuming Users and other models are here
using HR_Products.Models.Entitites; // For EmployeeProfile
using Microsoft.EntityFrameworkCore;
using HR_Products.Data;
using Microsoft.Extensions.Logging; // Add this using directive

namespace HR_Products.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager; // Inject RoleManager
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger; // Inject ILogger

        public AccountController(
            SignInManager<Users> signInManager,
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager, // Add RoleManager to constructor
            AppDbContext context,
            ILogger<AccountController> logger) // Add ILogger to constructor
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager; // Initialize RoleManager
            this._context = context;
            this._logger = logger; // Initialize Logger
        }

        public IActionResult Login(string returnUrl = null) // Add returnUrl parameter
        {
            ViewData["ReturnUrl"] = returnUrl; // Pass returnUrl to view
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email.ToLower());
                _logger.LogInformation("Login attempt for email: {Email}", model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("Email", "Email not found.");
                    _logger.LogWarning("Login failed: Email not found for {Email}", model.Email);
                    return View(model);
                }

                bool isPasswordValid = await userManager.CheckPasswordAsync(user, model.Password);
                if (!isPasswordValid)
                {
                    ModelState.AddModelError("Password", "Incorrect password.");
                    _logger.LogWarning("Login failed: Incorrect password for {Email}", model.Email);
                    return View(model);
                }

                var result = await signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserName} successfully logged in.", user.UserName);

                    var employee = await _context.EMPE_PROFILE
                        .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

                    if (employee != null)
                    {
                        // --- CRITICAL FIX: Role Assignment Logic ---
                        string targetRole = employee.JobTitle; // Get role from EmployeeProfile

                        if (!string.IsNullOrEmpty(targetRole))
                        {
                            // Ensure the role exists in Identity system
                            if (!await roleManager.RoleExistsAsync(targetRole))
                            {
                                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(targetRole));
                                if (createRoleResult.Succeeded)
                                {
                                    _logger.LogInformation("Created missing Identity role: {RoleName}", targetRole);
                                }
                                else
                                {
                                    _logger.LogError("Failed to create Identity role {RoleName}: {Errors}", targetRole, string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                                }
                            }

                            // Add user to the role if they are not already in it
                            if (!await userManager.IsInRoleAsync(user, targetRole))
                            {
                                var addRoleResult = await userManager.AddToRoleAsync(user, targetRole);
                                if (addRoleResult.Succeeded)
                                {
                                    _logger.LogInformation("User {UserName} (JobTitle: {JobTitle}) added to role: {RoleName}", user.UserName, employee.JobTitle, targetRole);
                                    // Re-sign in the user to refresh their claims (including new role)
                                    await signInManager.RefreshSignInAsync(user);
                                }
                                else
                                {
                                    _logger.LogError("Failed to add user {UserName} to role {RoleName}: {Errors}", user.UserName, targetRole, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Employee {EmpeId} has null/empty JobTitle. Skipping role assignment.", employee.EmpeId);
                        }
                        // --- END CRITICAL FIX ---

                        switch (employee.JobTitle)
                        {
                            case "HR-Admin":
                            case "Admin":
                                _logger.LogInformation("Redirecting Admin/HR-Admin user {UserName} to Admin Dashboard.", user.UserName);
                                return RedirectToAction("Dashboard", "Admin");
                            case "Finance-Admin":
                                _logger.LogInformation("Redirecting Finance-Admin user {UserName} to Finance Dashboard.", user.UserName);
                                return RedirectToAction("Index", "FinanceDashboard");
                            default:
                                _logger.LogInformation("Redirecting regular user {UserName} to User Dashboard.", user.UserName);
                                return RedirectToAction("Index", "UserDashboard");
                        }
                    }
                    else
                    {
                        // If Identity user exists but no corresponding employee profile
                        _logger.LogWarning("Login succeeded for Identity user {UserName}, but no corresponding EmployeeProfile found for email {Email}. Signing out.", user.UserName, user.Email);
                        ModelState.AddModelError(string.Empty, "Employee profile not found for your account. Please contact HR.");
                        await signInManager.SignOutAsync(); // Sign out the user if no profile
                        return View(model);
                    }
                }
                // Handle other SignInResult outcomes explicitly for better debugging
                else if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Account locked out.");
                    _logger.LogWarning("Login failed: Account locked out for {Email}", model.Email);
                    return View(model);
                }
                else if (result.RequiresTwoFactor)
                {
                    ModelState.AddModelError(string.Empty, "Two-factor authentication required.");
                    _logger.LogWarning("Login failed: Two-factor authentication required for {Email}", model.Email);
                    return View(model);
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Login not allowed (e.g., email not confirmed).");
                    _logger.LogWarning("Login failed: User not allowed to sign in for {Email}", model.Email);
                    return View(model);
                }
                else
                {
                    // Generic error for any other unhandled result
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    _logger.LogError("Login failed for {Email}: Unknown SignInResult reason.", model.Email);
                    return View(model);
                }
            }
            _logger.LogWarning("Login attempt failed due to invalid ModelState for {Email}", model.Email);
            return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string email)
        {
            _logger.LogInformation("ConfirmEmail called for email: {Email}", email);
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                user.EmailConfirmed = true;
                var updateResult = await userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    _logger.LogInformation("Email {Email} confirmed successfully.", email);
                    return Content("Email confirmed successfully.");
                }
                else
                {
                    _logger.LogError("Failed to update user {Email} for email confirmation: {Errors}", email, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    return Content("Failed to confirm email.");
                }
            }
            _logger.LogWarning("ConfirmEmail: User not found for email: {Email}", email);
            return Content("User not found.");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Users
                {
                    FullName = model.Name,
                    UserName = model.Email, // Ensure UserName is set to Email for consistency
                    Email = model.Email,
                    EmailConfirmed = true, // Assuming you want to auto-confirm on registration
                    PhoneNumberConfirmed = true // Assuming you want to auto-confirm
                };

                _logger.LogInformation("Register attempt for email: {Email}", model.Email);
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserName} successfully registered.", user.UserName);

                    var employee = await _context.EMPE_PROFILE
                        .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

                    if (employee != null)
                    {
                        // --- CRITICAL FIX: Role Assignment Logic for Register ---
                        string targetRole = employee.JobTitle;

                        if (!string.IsNullOrEmpty(targetRole))
                        {
                            // Ensure the role exists in Identity system
                            if (!await roleManager.RoleExistsAsync(targetRole))
                            {
                                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(targetRole));
                                if (createRoleResult.Succeeded)
                                {
                                    _logger.LogInformation("Created missing Identity role: {RoleName}", targetRole);
                                }
                                else
                                {
                                    _logger.LogError("Failed to create Identity role {RoleName} during registration: {Errors}", targetRole, string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                                }
                            }

                            // Add user to the role
                            var addRoleResult = await userManager.AddToRoleAsync(user, targetRole);
                            if (addRoleResult.Succeeded)
                            {
                                _logger.LogInformation("Registered user {UserName} (JobTitle: {JobTitle}) added to role: {RoleName}", user.UserName, employee.JobTitle, targetRole);
                                // No RefreshSignInAsync needed here as user is not yet signed in.
                                // User will get roles on first successful login.
                            }
                            else
                            {
                                _logger.LogError("Failed to add registered user {UserName} to role {RoleName}: {Errors}", user.UserName, targetRole, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Employee {EmpeId} has null/empty JobTitle during registration. Skipping role assignment.", employee.EmpeId);
                        }
                        // --- END CRITICAL FIX ---

                        // Redirect after successful registration and role assignment
                        switch (employee.JobTitle)
                        {
                            case "HR-Admin":
                            case "Admin":
                                _logger.LogInformation("Redirecting newly registered Admin/HR-Admin user {UserName} to Admin Dashboard.", user.UserName);
                                return RedirectToAction("Dashboard", "Admin");
                            case "Finance-Admin":
                                _logger.LogInformation("Redirecting newly registered Finance-Admin user {UserName} to Finance Dashboard.", user.UserName);
                                return RedirectToAction("Index", "FinanceDashboard");
                            default:
                                _logger.LogInformation("Redirecting newly registered regular user {UserName} to Home page.", user.UserName);
                                return RedirectToAction("Index", "Home"); // Default redirect for regular users
                        }
                    }
                    else
                    {
                        _logger.LogWarning("User {UserName} registered, but no corresponding EmployeeProfile found for email {Email}. Please create EmployeeProfile.", user.UserName, user.Email);
                        ModelState.AddModelError(string.Empty, "Registration successful, but no employee profile found. Please contact HR to create your profile.");
                        // You might want to automatically sign out here if you don't want them logged in without a profile
                        return View(model); // Show error on Register page
                    }
                }

                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                        ModelState.AddModelError("Email", "Email already registered.");
                    else
                        ModelState.AddModelError(string.Empty, error.Description);
                }
                _logger.LogWarning("Registration failed for {Email}: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            _logger.LogWarning("Registration attempt failed due to invalid ModelState for {Email}", model.Email);
            return View(model);
        }

        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Something is wrong!");
                    _logger.LogWarning("VerifyEmail failed: User not found for email {Email}", model.Email);
                    return View(model);
                }
                else
                {
                    _logger.LogInformation("VerifyEmail: Redirecting to ChangePassword for user {UserName}", user.UserName);
                    return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
                }
            }
            _logger.LogWarning("VerifyEmail attempt failed due to invalid ModelState for {Email}", model.Email);
            return View(model);
        }

        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("ChangePassword (GET): Username is null or empty, redirecting to VerifyEmail.");
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePassworViewModel { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePassworViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);
                if (user != null)
                {
                    _logger.LogInformation("ChangePassword (POST): Attempting password change for user {UserName}", user.UserName);
                    var result = await userManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddPasswordAsync(user, model.NewPassword);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Password successfully changed for user {UserName}.", user.UserName);
                            return RedirectToAction("Login", "Account");
                        }
                        else
                        {
                            _logger.LogError("Failed to add new password for user {UserName}: {Errors}", user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to remove old password for user {UserName}: {Errors}", user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                        foreach (var error in result.Errors)
                        {
                            // This case might happen if no password exists, but RemovePasswordAsync still fails for other reasons.
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("ChangePassword (POST): User not found for email {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Email Not Found");
                    return View(model);
                }
            }
            else
            {
                _logger.LogWarning("ChangePassword (POST): Invalid ModelState for email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Something went wrong. Try again.");
                return View(model);
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login", "Account"); // Redirect to login page instead of Home
        }

    }
}
