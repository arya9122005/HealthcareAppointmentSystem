namespace HealthcareAppointmentSystem.Models;

// One row = one working window on one weekday. No row for a day means "not available".
public class DoctorSchedule
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Length of each bookable slot in minutes.
    public int SlotMinutes { get; set; } = 30;
}
