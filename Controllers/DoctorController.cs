using HealthcareAppointmentSystem.Data;
using HealthcareAppointmentSystem.Models;
using HealthcareAppointmentSystem.Services;
using HealthcareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAppointmentSystem.Controllers;

[Authorize(Roles = AppRoles.Doctor)]
public class DoctorController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Every action starts from the logged-in doctor, so a doctor only ever sees their own schedule and appointments.
    private async Task<Doctor> GetDoctorAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _db.Doctors.Include(d => d.User).Include(d => d.Specialization)
            .FirstOrDefaultAsync(d => d.UserId == userId);
    }

    private static bool IsActive(Appointment a) =>
        a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed;

    public async Task<IActionResult> Dashboard()
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var mine = _db.Appointments.Where(a => a.DoctorId == doctor.Id);
        var today = DateTime.Today;

        var active = await mine.WithDetails()
            .Where(a => (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) && a.AppointmentDate >= today)
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToListAsync();

        return View(new DoctorDashboardViewModel
        {
            Doctor = doctor,
            Today = active.Where(a => a.AppointmentDate == today).ToList(),
            Upcoming = active.Where(a => a.AppointmentDate > today).Take(6).ToList(),
            TotalPatients = await mine.Select(a => a.PatientId).Distinct().CountAsync(),
            PendingCount = await mine.CountAsync(a => a.Status == AppointmentStatus.Pending),
            CompletedCount = await mine.CountAsync(a => a.Status == AppointmentStatus.Completed)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        return View(await BuildProfileAsync(doctor));
    }

    private async Task<DoctorProfileViewModel> BuildProfileAsync(Doctor doctor) => new()
    {
        FullName = doctor.User.FullName,
        Email = doctor.User.Email,
        Phone = doctor.User.PhoneNumber,
        SpecializationId = doctor.SpecializationId,
        Qualification = doctor.Qualification,
        ExperienceYears = doctor.ExperienceYears,
        ConsultationFee = doctor.ConsultationFee,
        Bio = doctor.Bio,
        Specializations = await _db.Specializations.OrderBy(s => s.Name).ToListAsync()
    };

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(DoctorProfileViewModel model)
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();
        model.Email = doctor.User.Email;

        if (!await _db.Specializations.AnyAsync(s => s.Id == model.SpecializationId))
            ModelState.AddModelError(nameof(model.SpecializationId), "Please choose a valid specialization.");

        if (!ModelState.IsValid)
        {
            model.Specializations = await _db.Specializations.OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        doctor.User.FullName = model.FullName.Trim();
        doctor.User.PhoneNumber = model.Phone;
        doctor.SpecializationId = model.SpecializationId;
        doctor.Qualification = model.Qualification;
        doctor.ExperienceYears = model.ExperienceYears;
        doctor.ConsultationFee = model.ConsultationFee;
        doctor.Bio = model.Bio;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var existing = await _db.DoctorSchedules.Where(s => s.DoctorId == doctor.Id).ToListAsync();
        var model = new ScheduleViewModel();
        foreach (var day in Fmt.Week)
        {
            var row = existing.FirstOrDefault(s => s.DayOfWeek == day);
            model.Days.Add(new ScheduleDayViewModel
            {
                Day = day,
                IsAvailable = row != null,
                StartTime = row?.StartTime ?? new TimeSpan(9, 0, 0),
                EndTime = row?.EndTime ?? new TimeSpan(13, 0, 0),
                SlotMinutes = row?.SlotMinutes ?? 30
            });
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(ScheduleViewModel model)
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        for (var i = 0; i < model.Days.Count; i++)
        {
            var day = model.Days[i];
            if (!day.IsAvailable) continue;

            if (day.EndTime <= day.StartTime)
                ModelState.AddModelError($"Days[{i}].EndTime", "End time must be after the start time.");
            else if (day.SlotMinutes < 10 || day.SlotMinutes > 120)
                ModelState.AddModelError($"Days[{i}].SlotMinutes", "Slot length must be 10-120 minutes.");
            else if (TimeSpan.FromMinutes(day.SlotMinutes) > day.EndTime - day.StartTime)
                ModelState.AddModelError($"Days[{i}].SlotMinutes", "Slot is longer than the working window.");
        }
        if (!ModelState.IsValid) return View(model);

        // Always scope to this doctor's own rows.
        var old = await _db.DoctorSchedules.Where(s => s.DoctorId == doctor.Id).ToListAsync();
        _db.DoctorSchedules.RemoveRange(old);

        foreach (var day in model.Days.Where(d => d.IsAvailable))
        {
            _db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctor.Id,
                DayOfWeek = day.Day,
                StartTime = day.StartTime,
                EndTime = day.EndTime,
                SlotMinutes = day.SlotMinutes
            });
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = "Your weekly schedule has been saved.";
        return RedirectToAction(nameof(Schedule));
    }

    public async Task<IActionResult> Appointments(string view = "upcoming")
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var all = await _db.Appointments.Where(a => a.DoctorId == doctor.Id).WithDetails().ToListAsync();
        var today = DateTime.Today;

        var model = new DoctorAppointmentsViewModel { View = view == "history" ? "history" : "upcoming" };
        model.Items = model.View == "upcoming"
            ? all.Where(a => IsActive(a) && a.AppointmentDate >= today)
                  .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToList()
            : all.Where(a => !(IsActive(a) && a.AppointmentDate >= today))
                  .OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ToList();
        return View(model);
    }

    private Task<Appointment> FindOwnAppointmentAsync(int id, int doctorId) =>
        _db.Appointments.WithDetails().FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctorId);

    public async Task<IActionResult> AppointmentDetails(int id)
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var appointment = await FindOwnAppointmentAsync(id, doctor.Id);
        if (appointment == null) return NotFound();
        return View(appointment);
    }

    // Basic patient information, only for patients who have an appointment with this doctor.
    public async Task<IActionResult> PatientDetails(int id)
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var history = await _db.Appointments.WithDetails()
            .Where(a => a.DoctorId == doctor.Id && a.PatientId == id)
            .OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ToListAsync();
        if (history.Count == 0) return NotFound();

        return View(new PatientHistoryViewModel { Patient = history[0].Patient, Appointments = history });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, string returnTo = null)
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);
        if (appointment == null) return NotFound();

        if (appointment.Status == AppointmentStatus.Pending)
        {
            appointment.Status = AppointmentStatus.Confirmed;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Appointment confirmed.";
        }
        else TempData["Error"] = "Only pending appointments can be confirmed.";

        return RedirectAfterAction(returnTo, id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string returnTo = null)
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);
        if (appointment == null) return NotFound();

        if (IsActive(appointment))
        {
            appointment.Status = AppointmentStatus.Cancelled;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Appointment cancelled.";
        }
        else TempData["Error"] = "Only pending or confirmed appointments can be cancelled.";

        return RedirectAfterAction(returnTo, id);
    }

    private IActionResult RedirectAfterAction(string returnTo, int id) =>
        returnTo == "details"
            ? RedirectToAction(nameof(AppointmentDetails), new { id })
            : RedirectToAction(nameof(Appointments));

    // Completing an appointment and writing the consultation note happen on the same form.
    [HttpGet]
    public async Task<IActionResult> Complete(int id)
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var appointment = await FindOwnAppointmentAsync(id, doctor.Id);
        if (appointment == null) return NotFound();

        var error = CompletionError(appointment);
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(AppointmentDetails), new { id });
        }

        return View(new ConsultationNoteViewModel
        {
            AppointmentId = id,
            Appointment = appointment,
            Diagnosis = appointment.ConsultationNote?.Diagnosis,
            Notes = appointment.ConsultationNote?.Notes,
            Prescription = appointment.ConsultationNote?.Prescription
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(ConsultationNoteViewModel model)
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var appointment = await FindOwnAppointmentAsync(model.AppointmentId, doctor.Id);
        if (appointment == null) return NotFound();

        var error = CompletionError(appointment);
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(AppointmentDetails), new { id = appointment.Id });
        }

        if (!ModelState.IsValid)
        {
            model.Appointment = appointment;
            return View(model);
        }

        var note = appointment.ConsultationNote;
        if (note == null)
        {
            note = new ConsultationNote { AppointmentId = appointment.Id };
            _db.ConsultationNotes.Add(note);
        }
        note.Diagnosis = model.Diagnosis.Trim();
        note.Notes = model.Notes;
        note.Prescription = model.Prescription;
        appointment.Status = AppointmentStatus.Completed;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Consultation saved and appointment marked as completed.";
        return RedirectToAction(nameof(AppointmentDetails), new { id = appointment.Id });
    }

    private static string CompletionError(Appointment a)
    {
        if (a.Status != AppointmentStatus.Confirmed && a.Status != AppointmentStatus.Completed)
            return "Confirm the appointment before completing it.";
        if (a.AppointmentDate > DateTime.Today)
            return "You can only complete an appointment on or after its date.";
        return null;
    }

    public async Task<IActionResult> Notes()
    {
        var doctor = await GetDoctorAsync();
        if (doctor == null) return Forbid();

        var items = await _db.Appointments.WithDetails()
            .Where(a => a.DoctorId == doctor.Id && a.ConsultationNote != null)
            .OrderByDescending(a => a.AppointmentDate).ToListAsync();
        return View(items);
    }
}
