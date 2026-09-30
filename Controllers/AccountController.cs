using HealthcareAppointmentSystem.Data;
using HealthcareAppointmentSystem.Models;
using HealthcareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareAppointmentSystem.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date >= DateTime.Today)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth must be in the past.");

        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.Phone
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var message = error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail"
                    ? "An account with this email already exists."
                    : error.Description;
                ModelState.AddModelError("", message);
            }
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, AppRoles.Patient);
        _db.Patients.Add(new Patient { UserId = user.Id, DateOfBirth = model.DateOfBirth, Gender = model.Gender });
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] = "Welcome! Your account is ready.";
        return RedirectToAction("Dashboard", "Patient");
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError("", "Invalid email or password, or the account is deactivated.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Invalid email or password, or the account is deactivated.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.Admin)) return RedirectToAction("Dashboard", "Admin");
        if (roles.Contains(AppRoles.Doctor)) return RedirectToAction("Dashboard", "Doctor");
        return RedirectToAction("Dashboard", "Patient");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
