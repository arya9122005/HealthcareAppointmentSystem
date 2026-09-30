using HealthcareAppointmentSystem.Data;
using HealthcareAppointmentSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAppointmentSystem.Services;

public record TimeSlot(TimeSpan Start, TimeSpan End)
{
    public string Value => Start.ToString(@"hh\:mm");
    public string Label => Fmt.Time(Start);
}

public record BookingResult(bool Success, string Error, Appointment Appointment)
{
    public static BookingResult Ok(Appointment a) => new(true, null, a);
    public static BookingResult Fail(string error) => new(false, error, null);
}

public interface IAppointmentService
{
    Task<List<TimeSlot>> GetAvailableSlotsAsync(int doctorId, DateTime date);
    Task<BookingResult> BookAsync(int patientId, int doctorId, DateTime date, TimeSpan start, string reason);
}

public class AppointmentService : IAppointmentService
{
    public const int MaxDaysAhead = 60;
    private readonly ApplicationDbContext _db;

    public AppointmentService(ApplicationDbContext db) => _db = db;

    // Slots = the doctor's working window for that weekday, cut into SlotMinutes pieces,
    // minus slots already booked (non-cancelled) and minus times that have already passed today.
    public async Task<List<TimeSlot>> GetAvailableSlotsAsync(int doctorId, DateTime date)
    {
        date = date.Date;
        var slots = new List<TimeSlot>();
        if (date < DateTime.Today || date > DateTime.Today.AddDays(MaxDaysAhead)) return slots;

        var dayOfWeek = date.DayOfWeek;
        var schedules = await _db.DoctorSchedules.AsNoTracking()
            .Where(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek)
            .ToListAsync();
        if (schedules.Count == 0) return slots;

        var booked = await _db.Appointments.AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate == date && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.StartTime)
            .ToListAsync();

        var now = DateTime.Now.TimeOfDay;
        foreach (var schedule in schedules.OrderBy(s => s.StartTime))
        {
            var length = TimeSpan.FromMinutes(schedule.SlotMinutes);
            for (var t = schedule.StartTime; t + length <= schedule.EndTime; t += length)
            {
                if (date == DateTime.Today && t <= now) continue;
                if (booked.Contains(t)) continue;
                slots.Add(new TimeSlot(t, t + length));
            }
        }
        return slots;
    }

    public async Task<BookingResult> BookAsync(int patientId, int doctorId, DateTime date, TimeSpan start, string reason)
    {
        date = date.Date;

        var doctorActive = await _db.Doctors.AnyAsync(d => d.Id == doctorId && d.IsActive);
        if (!doctorActive) return BookingResult.Fail("This doctor is not available for booking.");

        var slots = await GetAvailableSlotsAsync(doctorId, date);
        var slot = slots.FirstOrDefault(s => s.Start == start);
        if (slot == null)
            return BookingResult.Fail("That time slot is no longer available. Please choose another one.");

        var patientClash = await _db.Appointments.AnyAsync(a =>
            a.PatientId == patientId && a.AppointmentDate == date && a.StartTime == start &&
            a.Status != AppointmentStatus.Cancelled);
        if (patientClash)
            return BookingResult.Fail("You already have an appointment at this time.");

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = date,
            StartTime = slot.Start,
            EndTime = slot.End,
            ReasonForVisit = reason.Trim(),
            Status = AppointmentStatus.Pending
        };
        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The unique index caught a simultaneous booking of the same slot.
            return BookingResult.Fail("Someone just booked that slot. Please pick another time.");
        }
        return BookingResult.Ok(appointment);
    }
}
