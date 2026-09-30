using System.ComponentModel.DataAnnotations;

namespace HealthcareAppointmentSystem.Models;

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; }

    [DataType(DataType.Date)]
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    [Required, StringLength(300)]
    public string ReasonForVisit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ConsultationNote ConsultationNote { get; set; }
}
