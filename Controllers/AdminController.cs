using HealthcareAppointmentSystem.Data;
using HealthcareAppointmentSystem.Models;
using HealthcareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAppointmentSystem.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        var model = new AdminDashboardViewModel
        {
            TotalPatients = await _db.Patients.CountAsync(),
            TotalDoctors = await _db.Doctors.CountAsync(d => d.IsActive),
            TotalAppointments = await _db.Appointments.CountAsync(),
            Pending = await _db.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending),
            Confirmed = await _db.Appointments.CountAsync(a => a.Status == AppointmentStatus.Confirmed),
            Completed = await _db.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed),
            Cancelled = await _db.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled),
            Recent = await _db.Appointments.WithDetails()
                .OrderByDescending(a => a.CreatedAt).Take(8).ToListAsync()
        };
        return View(model);
    }

    // ---------- Patients ----------
    public async Task<IActionResult> Patients()
    {
        var patients = await _db.Patients.Include(p => p.User).Include(p => p.Appointments)
            .OrderBy(p => p.User.FullName).ToListAsync();
        return View(patients);
    }

    // ---------- Doctors ----------
    public async Task<IActionResult> Doctors()
    {
        var doctors = await _db.Doctors.Include(d => d.User).Include(d => d.Specialization)
            .OrderBy(d => d.User.FullName).ToListAsync();
        return View(doctors);
    }

    [HttpGet]
    public async Task<IActionResult> CreateDoctor()
    {
        var model = new DoctorFormViewModel { Specializations = await LoadSpecializationsAsync() };
        return View("DoctorForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDoctor(DoctorFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Please set an initial password for the doctor.");
        if (model.SpecializationId.HasValue && !await _db.Specializations.AnyAsync(s => s.Id == model.SpecializationId))
            ModelState.AddModelError(nameof(model.SpecializationId), "Please choose a valid specialization.");
        if (await _userManager.FindByEmailAsync(model.Email ?? "") != null)
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");

        if (!ModelState.IsValid)
        {
            model.Specializations = await LoadSpecializationsAsync();
            return View("DoctorForm", model);
        }

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
            foreach (var error in result.Errors) ModelState.AddModelError(nameof(model.Password), error.Description);
            model.Specializations = await LoadSpecializationsAsync();
            return View("DoctorForm", model);
        }

        await _userManager.AddToRoleAsync(user, AppRoles.Doctor);
        _db.Doctors.Add(new Doctor
        {
            UserId = user.Id,
            SpecializationId = model.SpecializationId.Value,
            Qualification = model.Qualification,
            ExperienceYears = model.ExperienceYears,
            ConsultationFee = model.ConsultationFee,
            Bio = model.Bio
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{user.FullName} was added. They can now sign in and set their schedule.";
        return RedirectToAction(nameof(Doctors));
    }

    [HttpGet]
    public async Task<IActionResult> EditDoctor(int id)
    {
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null) return NotFound();

        return View("DoctorForm", new DoctorFormViewModel
        {
            Id = doctor.Id,
            FullName = doctor.User.FullName,
            Email = doctor.User.Email,
            Phone = doctor.User.PhoneNumber,
            SpecializationId = doctor.SpecializationId,
            Qualification = doctor.Qualification,
            ExperienceYears = doctor.ExperienceYears,
            ConsultationFee = doctor.ConsultationFee,
            Bio = doctor.Bio,
            Specializations = await LoadSpecializationsAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDoctor(DoctorFormViewModel model)
    {
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == model.Id);
        if (doctor == null) return NotFound();
        model.Email = doctor.User.Email; // email is the login name and is not edited here

        if (model.SpecializationId.HasValue && !await _db.Specializations.AnyAsync(s => s.Id == model.SpecializationId))
            ModelState.AddModelError(nameof(model.SpecializationId), "Please choose a valid specialization.");

        if (!ModelState.IsValid)
        {
            model.Specializations = await LoadSpecializationsAsync();
            return View("DoctorForm", model);
        }

        doctor.User.FullName = model.FullName.Trim();
        doctor.User.PhoneNumber = model.Phone;
        doctor.SpecializationId = model.SpecializationId.Value;
        doctor.Qualification = model.Qualification;
        doctor.ExperienceYears = model.ExperienceYears;
        doctor.ConsultationFee = model.ConsultationFee;
        doctor.Bio = model.Bio;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(doctor.User);
            var reset = await _userManager.ResetPasswordAsync(doctor.User, token, model.Password);
            if (!reset.Succeeded)
            {
                foreach (var error in reset.Errors) ModelState.AddModelError(nameof(model.Password), error.Description);
                model.Specializations = await LoadSpecializationsAsync();
                return View("DoctorForm", model);
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Doctor details updated.";
        return RedirectToAction(nameof(Doctors));
    }

    // "Removing" a doctor deactivates them so their appointment history stays intact.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDoctor(int id)
    {
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null) return NotFound();

        doctor.IsActive = !doctor.IsActive;
        doctor.User.IsActive = doctor.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = doctor.IsActive ? "Doctor reactivated." : "Doctor deactivated. They no longer appear in search or can sign in.";
        return RedirectToAction(nameof(Doctors));
    }

    // ---------- Specializations ----------
    public async Task<IActionResult> Specializations()
    {
        var items = await _db.Specializations.Include(s => s.Doctors).OrderBy(s => s.Name).ToListAsync();
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSpecialization(int id, string name, string description)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0 || name.Length > 80)
        {
            TempData["Error"] = "Specialization name is required (max 80 characters).";
            return RedirectToAction(nameof(Specializations));
        }
        if (await _db.Specializations.AnyAsync(s => s.Name == name && s.Id != id))
        {
            TempData["Error"] = $"A specialization named \"{name}\" already exists.";
            return RedirectToAction(nameof(Specializations));
        }

        if (id == 0)
        {
            _db.Specializations.Add(new Specialization { Name = name, Description = description });
        }
        else
        {
            var spec = await _db.Specializations.FindAsync(id);
            if (spec == null) return NotFound();
            spec.Name = name;
            spec.Description = description;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Specialization saved.";
        return RedirectToAction(nameof(Specializations));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSpecialization(int id)
    {
        var spec = await _db.Specializations.FindAsync(id);
        if (spec == null) return NotFound();

        if (await _db.Doctors.AnyAsync(d => d.SpecializationId == id))
        {
            TempData["Error"] = "This specialization is assigned to doctors, so it can't be deleted.";
            return RedirectToAction(nameof(Specializations));
        }
        _db.Specializations.Remove(spec);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Specialization deleted.";
        return RedirectToAction(nameof(Specializations));
    }

    // ---------- Appointments ----------
    public async Task<IActionResult> Appointments(string status)
    {
        var query = _db.Appointments.WithDetails().AsQueryable();
        if (Enum.TryParse<AppointmentStatus>(status, out var parsed))
            query = query.Where(a => a.Status == parsed);
        else
            status = null;

        var items = await query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ToListAsync();
        return View(new AdminAppointmentsViewModel { Status = status, Items = items });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(int id, string status)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null) return NotFound();

        if (appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Confirmed)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Appointment cancelled.";
        }
        else TempData["Error"] = "Only pending or confirmed appointments can be cancelled.";

        return RedirectToAction(nameof(Appointments), new { status });
    }

    // ---------- Users ----------
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var rows = new List<UserRowViewModel>();
        foreach (var user in users)
        {
            rows.Add(new UserRowViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            });
        }
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You can't change your own administrator access.";
            return RedirectToAction(nameof(Users));
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            await _userManager.RemoveFromRoleAsync(user, AppRoles.Admin);
            TempData["Success"] = $"{user.FullName} is no longer an administrator.";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, AppRoles.Admin);
            TempData["Success"] = $"{user.FullName} is now an administrator.";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You can't deactivate your own account.";
            return RedirectToAction(nameof(Users));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
        if (doctor != null)
        {
            doctor.IsActive = user.IsActive;
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = user.IsActive ? "Account reactivated." : "Account deactivated.";
        return RedirectToAction(nameof(Users));
    }

    private Task<List<Specialization>> LoadSpecializationsAsync() =>
        _db.Specializations.OrderBy(s => s.Name).ToListAsync();
}
