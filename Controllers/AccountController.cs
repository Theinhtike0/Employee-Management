using Microsoft.AspNetCore.Identity;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Mvc;
using HR_Products.Models;
using System.Diagnostics.Eventing.Reader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HR_Products.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email.ToLower());

                if (user == null)
                {
                    ModelState.AddModelError("Email", "Email not found.");
                    return View(model);
                }

                bool isPasswordValid = await userManager.CheckPasswordAsync(user, model.Password);
                if (!isPasswordValid)
                {
                    ModelState.AddModelError("Password", "Incorrect password.");
                    return View(model);
                }
                var result = await signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Check if the user is an Admin
                    if (await userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "Login failed. Contact support if the issue persists.");
            }
            return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);
                return Content("Email confirmed successfully.");
            }
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
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true
                };

                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                    if (model.Email.ToLower() == "admin@gmail.com" || model.Email.ToLower() == "superadmin@gmail.com")
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                    }

                    await signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                        ModelState.AddModelError("Email", "Email already registered.");
                    else
                        ModelState.AddModelError(string.Empty, error.Description);
                }
            }
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
                if(user == null)
                {
                    ModelState.AddModelError("","Something is wrong!");
                    return View(model);
                }
                else
                {
                    return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
                }
            }
            return View(model);
        }

        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
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
                if(user != null)
                {
                    var result = await userManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddPasswordAsync(user, model.NewPassword);
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                                ModelState.AddModelError("Email", "Email already registered.");
                            else
                                ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email Not Found");
                    return View(model);

                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Something went wrong. Try again.");
                return View(model); 
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}
