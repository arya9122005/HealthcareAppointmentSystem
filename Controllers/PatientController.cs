using HealthcareAppointmentSystem.Data;
using HealthcareAppointmentSystem.Models;
using HealthcareAppointmentSystem.Services;
using HealthcareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAppointmentSystem.Controllers;

[Authorize(Roles = AppRoles.Patient)]
public class PatientController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAppointmentService _appointments;

    public PatientController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IAppointmentService appointments)
    {
        _db = db;
        _userManager = userManager;
        _appointments = appointments;
    }

    // Every action starts from the logged-in patient, so a patient can never reach another patient's data.
    private async Task<Patient> GetPatientAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.UserId == userId);
    }

    private static bool IsActive(Appointment a) =>
        a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed;

    public async Task<IActionResult> Dashboard()
    {
        var patient = await GetPatientAsync();
        if (patient == null) return Forbid();

        var all = await _db.Appointments.Where(a => a.PatientId == patient.Id).WithDetails()
            .OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime)
            .ToListAsync();

        var today = DateTime.Today;
        var upcoming = all.Where(a => IsActive(a) && a.AppointmentDate >= today)
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToList();

        return View(new PatientDashboardViewModel
        {
            FullName = patient.User.FullName,
            Next = upcoming.FirstOrDefault(),
            UpcomingCount = upcoming.Count,
            TotalCount = all.Count,
            CompletedCount = all.Count(a => a.Status == AppointmentStatus.Completed),
            Recent = all.Take(5).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var patient = await GetPatientAsync();
        if (patient == null) return Forbid();
        return View(new PatientProfileViewModel
        {
            FullName = patient.User.FullName,
            Email = patient.User.Email,
            Phone = patient.User.PhoneNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            BloodGroup = patient.BloodGroup,
            Address = patient.Address
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(PatientProfileViewModel model)
    {
        var patient = await GetPatientAsync();
        if (patient == null) return Forbid();
        model.Email = patient.User.Email;

        if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date >= DateTime.Today)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth must be in the past.");
        if (!ModelState.IsValid) return View(model);

        patient.User.FullName = model.FullName.Trim();
        patient.User.PhoneNumber = model.Phone;
        patient.DateOfBirth = model.DateOfBirth;
        patient.Gender = model.Gender;
        patient.BloodGroup = model.BloodGroup;
        patient.Address = model.Address;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Doctors(string search, int? specializationId)
    {
        var query = _db.Doctors.Include(d => d.User).Include(d => d.Specialization).Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.User.FullName.Contains(term) || d.Specialization.Name.Contains(term));
        }
        if (specializationId.HasValue)
            query = query.Where(d => d.SpecializationId == specializationId.Value);

        return View(new DoctorListViewModel
        {
            Search = search,
            SpecializationId = specializationId,
            Doctors = await query.OrderBy(d => d.User.FullName).ToListAsync(),
            Specializations = await _db.Specializations.OrderBy(s => s.Name).ToListAsync()
        });
    }

    public async Task<IActionResult> DoctorDetails(int id)
    {
        var doctor = await _db.Doctors.Include(d => d.User).Include(d => d.Specialization)
            .Include(d => d.Schedules).FirstOrDefaultAsync(d => d.Id == id && d.IsActive);
        if (doctor == null) return NotFound();
        return View(doctor);
    }

    private Task<Doctor> LoadBookableDoctorAsync(int id) =>
        _db.Doctors.Include(d => d.User).Include(d => d.Specialization)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

    [HttpGet]
    public async Task<IActionResult> Book(int doctorId, DateTime? date)
    {
        var doctor = await LoadBookableDoctorAsync(doctorId);
        if (doctor == null) return NotFound();

        var day = (date ?? DateTime.Today.AddDays(1)).Date;
        if (day < DateTime.Today) day = DateTime.Today;

        return View(new BookAppointmentViewModel
        {
            DoctorId = doctor.Id,
            Doctor = doctor,
            Date = day,
            Slots = await _appointments.GetAvailableSlotsAsync(doctor.Id, day)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookAppointmentViewModel model)
    {
        var patient = await GetPatientAsync();
        if (patient == null) return Forbid();
        var doctor = await LoadBookableDoctorAsync(model.DoctorId);
        if (doctor == null) return NotFound();

        model.Date = model.Date.Date;
        if (model.Date < DateTime.Today)
            ModelState.AddModelError(nameof(model.Date), "Please choose today or a future date.");
        else if (model.Date > DateTime.Today.AddDays(AppointmentService.MaxDaysAhead))
            ModelState.AddModelError(nameof(model.Date), $"You can book up to {AppointmentService.MaxDaysAhead} days ahead.");

        TimeSpan start = default;
        if (string.IsNullOrEmpty(model.SelectedStart) || !TimeSpan.TryParse(model.SelectedStart, out start))
            ModelState.AddModelError(nameof(model.SelectedStart), "Please select a time slot.");

        if (ModelState.IsValid)
        {
            var result = await _appointments.BookAsync(patient.Id, doctor.Id, model.Date, start, model.ReasonForVisit);
            if (result.Success)
            {
                TempData["Success"] = "Appointment requested. The doctor will confirm it shortly.";
                return RedirectToAction(nameof(AppointmentDetails), new { id = result.Appointment.Id });
            }
            ModelState.AddModelError("", result.Error);
        }

        model.Doctor = doctor;
        model.Slots = await _appointments.GetAvailableSlotsAsync(doctor.Id, model.Date);
        return View(model);
    }

    public async Task<IActionResult> Appointments()
    {
        var patient = await GetPatientAsync();
        if (patient == null) return Forbid();

        var all = await _db.Appointments.Where(a => a.PatientId == patient.Id).WithDetails().ToListAsync();
        var today = DateTime.Today;

        return View(new PatientAppointmentsViewModel
        {
            Upcoming = all.Where(a => IsActive(a) && a.AppointmentDate >= today)
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToList(),
            Past = all.Where(a => !(IsActive(a) && a.AppointmentDate >= today))
                .OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ToList()
        });
    }

    public async Task<IActionResult> AppointmentDetails(int id)
    {
        var patient = await GetPatientAsync();
        if (patient == null) return Forbid();

        var appointment = await _db.Appointments.WithDetails()
            .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);
        if (appointment == null) return NotFound();
        return View(appointment);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var patient = await GetPatientAsync();
        if (patient == null) return Forbid();

        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);
        if (appointment == null) return NotFound();

        if (IsActive(appointment))
        {
            appointment.Status = AppointmentStatus.Cancelled;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Your appointment has been cancelled.";
        }
        else
        {
            TempData["Error"] = "Only pending or confirmed appointments can be cancelled.";
        }
        return RedirectToAction(nameof(Appointments));
    }

    public async Task<IActionResult> Consultations()
    {
        var patient = await GetPatientAsync();
        if (patient == null) return Forbid();

        var items = await _db.Appointments.WithDetails()
            .Where(a => a.PatientId == patient.Id && a.Status == AppointmentStatus.Completed && a.ConsultationNote != null)
            .OrderByDescending(a => a.AppointmentDate).ToListAsync();
        return View(items);
    }
}
